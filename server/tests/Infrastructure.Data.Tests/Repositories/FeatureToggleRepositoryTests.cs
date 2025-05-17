using Core.Entities;
using Core.Extensions;
using Infrastructure.Data.Repositories;
using Infrastructure.Data.Tests.Bases;
using Infrastructure.Data.Tests.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using SharedTestDependencies.Constants;
using SharedTestDependencies.Extensions;
using SharedTestDependencies.Fakes;
using SharedTestDependencies.Fixtures;
using Shouldly;

namespace Infrastructure.Data.Tests.Repositories;

[Collection(TestCollections.Database)]
public class FeatureToggleRepositoryTests : RepositoryTestBase
{
    private readonly FakeTimeProvider _timeProvider;
    private readonly PgFeatureToggleRepository _repository;
    private readonly IDistributedCache _cache;

    private FeatureToggleRepositoryTests(PostgresFixture fixture) : base(fixture)
    {
        _timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();
        var serviceProvider = services.BuildServiceProvider();
        _cache = serviceProvider.GetRequiredService<IDistributedCache>();
        _repository = new PgFeatureToggleRepository(DbContext, _cache, _timeProvider);
    }

    public class IsEnabledAsync(PostgresFixture fixture) : FeatureToggleRepositoryTests(fixture)
    {
        [Fact]
        public async Task IsEnabledAsync_WithNonExistentFeature_ReturnsTrue()
        {
            // Arrange
            var featureName = "non_existent_feature";

            // Act
            var result = await _repository.IsEnabledAsync(featureName);

            // Assert
            result.ShouldBeTrue();
        }

        [Fact]
        public async Task IsEnabledAsync_WithEnabledFeature_ReturnsTrue()
        {
            // Arrange
            var featureName = "enabled_feature";
            await DbContext.SeedFeatureAsync(_timeProvider, featureName, true);

            // Act
            var result = await _repository.IsEnabledAsync(featureName);

            // Assert
            result.ShouldBeTrue();
        }

        [Fact]
        public async Task IsEnabledAsync_WithDisabledFeature_ReturnsFalse()
        {
            // Arrange
            var featureName = "disabled_feature";
            await DbContext.SeedFeatureAsync(_timeProvider, featureName, false);

            // Act
            var result = await _repository.IsEnabledAsync(featureName);

            // Assert
            result.ShouldBeFalse();
        }

        [Fact]
        public async Task IsEnabledAsync_WithCachedValue_ReturnsCachedResult()
        {
            // Arrange
            var featureName = "cached_feature";
            await _cache.SetStringAsync($"feature:{featureName}", "false");

            // Act
            var result = await _repository.IsEnabledAsync(featureName);

            // Assert
            result.ShouldBeFalse();

            // Verify that no database query was made
            var dbFeature = await DbContext.Features.FirstOrDefaultAsync(f => f.Name == featureName);
            dbFeature.ShouldBeNull();
        }

        [Fact]
        public async Task IsEnabledAsync_WithoutCachedValue_SetsCache()
        {
            // Arrange
            var featureName = "feature_to_cache";
            await DbContext.SeedFeatureAsync(_timeProvider, featureName, true);

            // Act
            var result = await _repository.IsEnabledAsync(featureName);

            // Assert
            result.ShouldBeTrue();
            var cachedValue = await _cache.GetStringAsync($"feature:{featureName}");
            cachedValue.ShouldBe("True");
        }
    }

    public class SetEnabledAsync(PostgresFixture fixture) : FeatureToggleRepositoryTests(fixture)
    {
        [Fact]
        public async Task SetEnabledAsync_WithNewFeature_CreatesFeature()
        {
            // Arrange
            var featureName = "new_feature";
            var now = _timeProvider.Now();

            // Act
            var result = await _repository.SetEnabledAsync(featureName, true);

            // Assert
            result.ShouldBeTrue();
            var feature = await DbContext.Features.FirstOrDefaultAsync(f => f.Name == featureName);
            feature.ShouldNotBeNull();
            feature.IsEnabled.ShouldBeTrue();
            feature.CreatedAt.ShouldBeWithinTolerance(now);
        }

        [Fact]
        public async Task SetEnabledAsync_WithExistingFeature_UpdatesFeature()
        {
            // Arrange
            var featureName = "existing_feature";
            await DbContext.SeedFeatureAsync(_timeProvider, featureName, false);
            var now = _timeProvider.Now();

            // Act
            var result = await _repository.SetEnabledAsync(featureName, true);

            // Assert
            result.ShouldBeTrue();
            var updatedFeature = await DbContext.Features.FirstOrDefaultAsync(f => f.Name == featureName);
            updatedFeature.ShouldNotBeNull();
            updatedFeature.IsEnabled.ShouldBeTrue();
            updatedFeature.LastModifiedAt.ShouldNotBeNull();
            updatedFeature.LastModifiedAt.Value.ShouldBeWithinTolerance(now);
        }

        [Fact]
        public async Task SetEnabledAsync_EnablingFeature_UpdatesCache()
        {
            // Arrange
            var featureName = "feature_to_enable";

            // Act
            await _repository.SetEnabledAsync(featureName, true);

            // Assert
            var cachedValue = await _cache.GetStringAsync($"feature:{featureName}");
            cachedValue.ShouldBe("True");
        }

        [Fact]
        public async Task SetEnabledAsync_DisablingFeature_UpdatesCache()
        {
            // Arrange
            var featureName = "feature_to_disable";

            // Act
            await _repository.SetEnabledAsync(featureName, false);

            // Assert
            var cachedValue = await _cache.GetStringAsync($"feature:{featureName}");
            cachedValue.ShouldBe("False");
        }
    }

    public class IsEnabledForUserAsync(PostgresFixture fixture) : FeatureToggleRepositoryTests(fixture)
    {
        [Fact]
        public async Task IsEnabledForUserAsync_WithNonExistentFeature_ReturnsTrue()
        {
            // Arrange
            var featureName = "non_existent_feature";
            var userId = Guid.NewGuid();

            // Act
            var result = await _repository.IsEnabledForUserAsync(featureName, userId);

            // Assert
            result.ShouldBeTrue();
        }

        [Fact]
        public async Task IsEnabledForUserAsync_WithDisabledFeature_ReturnsFalse()
        {
            // Arrange
            var featureName = "disabled_for_user_feature";
            var userId = Guid.NewGuid();
            await DbContext.SeedFeatureAsync(_timeProvider, featureName, false);

            // Act
            var result = await _repository.IsEnabledForUserAsync(featureName, userId);

            // Assert
            result.ShouldBeFalse();
        }

        [Fact]
        public async Task IsEnabledForUserAsync_WithEnabledFeatureButUserNotInList_ReturnsFalse()
        {
            // Arrange
            var featureName = "enabled_feature_without_user";
            var userId = Guid.NewGuid();
            await DbContext.SeedFeatureAsync(_timeProvider, featureName, true);

            // Act
            var result = await _repository.IsEnabledForUserAsync(featureName, userId);

            // Assert
            result.ShouldBeFalse();
        }

        [Fact]
        public async Task IsEnabledForUserAsync_WithEnabledFeatureAndUserInList_ReturnsTrue()
        {
            // Arrange
            var featureName = "enabled_feature_with_user";
            var user = await DbContext.SeedUserAsync(_timeProvider);
            var feature = await DbContext.SeedFeatureAsync(_timeProvider, featureName, true);

            var featureUser = new FeatureUser
            {
                FeatureId = feature.Id,
                UserId = user.Id
            };
            await DbContext.FeatureUsers.AddAsync(featureUser);
            await DbContext.SaveChangesAsync();

            // Act
            var result = await _repository.IsEnabledForUserAsync(featureName, user.Id);

            // Assert
            result.ShouldBeTrue();
        }
    }

    public class GetAllFeaturesAsync(PostgresFixture fixture) : FeatureToggleRepositoryTests(fixture)
    {
        [Fact]
        public async Task GetAllFeaturesAsync_WithNoFeatures_ReturnsEmptyList()
        {
            // Act
            var result = await _repository.GetAllFeaturesAsync();

            // Assert
            result.ShouldBeEmpty();
        }

        [Fact]
        public async Task GetAllFeaturesAsync_WithMultipleFeatures_ReturnsAllFeatures()
        {
            // Arrange
            await DbContext.SeedFeatureAsync(_timeProvider, "feature1", true);
            await DbContext.SeedFeatureAsync(_timeProvider, "feature2", false);
            await DbContext.SeedFeatureAsync(_timeProvider, "feature3", true);

            // Act
            var result = await _repository.GetAllFeaturesAsync();

            // Assert
            var enumerable = result.ToList();
            enumerable.Count.ShouldBe(3);
            enumerable.Select(f => f.Name).ShouldContain("feature1");
            enumerable.Select(f => f.Name).ShouldContain("feature2");
            enumerable.Select(f => f.Name).ShouldContain("feature3");
        }
    }
}