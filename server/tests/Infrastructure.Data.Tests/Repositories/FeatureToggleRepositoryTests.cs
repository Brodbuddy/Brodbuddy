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
        public async Task SetEnabledAsync_EnablingFeature_InvalidatesCache()
        {
            // Arrange
            var featureName = "feature_to_enable";
            await _cache.SetStringAsync($"feature:{featureName}", "False");

            // Act
            await _repository.SetEnabledAsync(featureName, true);

            // Assert
            var cachedValue = await _cache.GetStringAsync($"feature:{featureName}");
            cachedValue.ShouldBeNull();
        }

        [Fact]
        public async Task SetEnabledAsync_DisablingFeature_InvalidatesCache()
        {
            // Arrange
            var featureName = "feature_to_disable";
            await _cache.SetStringAsync($"feature:{featureName}", "True");

            // Act
            await _repository.SetEnabledAsync(featureName, false);

            // Assert
            var cachedValue = await _cache.GetStringAsync($"feature:{featureName}");
            cachedValue.ShouldBeNull();
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

        [Fact]
        public async Task IsEnabledForUserAsync_WithCachedValue_ReturnsCachedResult()
        {
            // Arrange
            var featureName = "cached_user_feature";
            var userId = Guid.NewGuid();
            var userCacheKey = $"feature:{featureName}:user:{userId}";
            await _cache.SetStringAsync(userCacheKey, "false");

            // Act
            var result = await _repository.IsEnabledForUserAsync(featureName, userId);

            // Assert
            result.ShouldBeFalse();

            var dbFeature = await DbContext.Features.FirstOrDefaultAsync(f => f.Name == featureName);
            dbFeature.ShouldBeNull();
        }

        [Fact]
        public async Task IsEnabledForUserAsync_WithoutCachedValue_SetsUserCache()
        {
            // Arrange
            var featureName = "feature_to_cache_user";
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
            var userCacheKey = $"feature:{featureName}:user:{user.Id}";
            var cachedValue = await _cache.GetStringAsync(userCacheKey);
            cachedValue.ShouldBe("True");
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

    public class AddUserToFeatureAsync(PostgresFixture fixture) : FeatureToggleRepositoryTests(fixture)
    {
        [Fact]
        public async Task AddUserToFeatureAsync_WithNonExistentFeature_ReturnsFalse()
        {
            // Arrange
            var featureName = "non_existent_feature";
            var userId = Guid.NewGuid();

            // Act
            var result = await _repository.AddUserToFeatureAsync(featureName, userId);

            // Assert
            result.ShouldBeFalse();
        }

        [Fact]
        public async Task AddUserToFeatureAsync_WithNewUser_AddsUserAndReturnsTrue()
        {
            // Arrange
            var featureName = "test_feature";
            var user = await DbContext.SeedUserAsync(_timeProvider);
            var feature = await DbContext.SeedFeatureAsync(_timeProvider, featureName, true);

            // Act
            var result = await _repository.AddUserToFeatureAsync(featureName, user.Id);

            // Assert
            result.ShouldBeTrue();
            var featureUser = await DbContext.FeatureUsers
                .FirstOrDefaultAsync(fu => fu.FeatureId == feature.Id && fu.UserId == user.Id);
            featureUser.ShouldNotBeNull();
        }

        [Fact]
        public async Task AddUserToFeatureAsync_WithExistingUser_ReturnsTrue()
        {
            // Arrange
            var featureName = "test_feature";
            var user = await DbContext.SeedUserAsync(_timeProvider);
            var feature = await DbContext.SeedFeatureAsync(_timeProvider, featureName, true);

            var existingFeatureUser = new FeatureUser
            {
                FeatureId = feature.Id,
                UserId = user.Id
            };
            await DbContext.FeatureUsers.AddAsync(existingFeatureUser);
            await DbContext.SaveChangesAsync();

            // Act
            var result = await _repository.AddUserToFeatureAsync(featureName, user.Id);

            // Assert
            result.ShouldBeTrue();
        }

        [Fact]
        public async Task AddUserToFeatureAsync_InvalidatesCacheForUser()
        {
            // Arrange
            var featureName = "test_feature";
            var user = await DbContext.SeedUserAsync(_timeProvider);
            await DbContext.SeedFeatureAsync(_timeProvider, featureName, true);

            // Pre-populate cache
            var userCacheKey = $"feature:{featureName}:user:{user.Id}";
            await _cache.SetStringAsync(userCacheKey, "False");

            // Act
            await _repository.AddUserToFeatureAsync(featureName, user.Id);

            // Assert
            var cachedValue = await _cache.GetStringAsync(userCacheKey);
            cachedValue.ShouldBeNull();
        }
    }

    public class RemoveUserFromFeatureAsync(PostgresFixture fixture) : FeatureToggleRepositoryTests(fixture)
    {
        [Fact]
        public async Task RemoveUserFromFeatureAsync_WithNonExistentFeature_ReturnsFalse()
        {
            // Arrange
            var featureName = "non_existent_feature";
            var userId = Guid.NewGuid();

            // Act
            var result = await _repository.RemoveUserFromFeatureAsync(featureName, userId);

            // Assert
            result.ShouldBeFalse();
        }

        [Fact]
        public async Task RemoveUserFromFeatureAsync_WithNonExistentUser_ReturnsFalse()
        {
            // Arrange
            var featureName = "test_feature";
            var userId = Guid.NewGuid();
            await DbContext.SeedFeatureAsync(_timeProvider, featureName, true);

            // Act
            var result = await _repository.RemoveUserFromFeatureAsync(featureName, userId);

            // Assert
            result.ShouldBeFalse();
        }

        [Fact]
        public async Task RemoveUserFromFeatureAsync_WithExistingUser_RemovesAndReturnsTrue()
        {
            // Arrange
            var featureName = "test_feature";
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
            var result = await _repository.RemoveUserFromFeatureAsync(featureName, user.Id);

            // Assert
            result.ShouldBeTrue();
            var deletedFeatureUser = await DbContext.FeatureUsers
                .FirstOrDefaultAsync(fu => fu.FeatureId == feature.Id && fu.UserId == user.Id);
            deletedFeatureUser.ShouldBeNull();
        }

        [Fact]
        public async Task RemoveUserFromFeatureAsync_InvalidatesCacheForUser()
        {
            // Arrange
            var featureName = "test_feature";
            var user = await DbContext.SeedUserAsync(_timeProvider);
            var feature = await DbContext.SeedFeatureAsync(_timeProvider, featureName, true);

            var featureUser = new FeatureUser
            {
                FeatureId = feature.Id,
                UserId = user.Id
            };
            await DbContext.FeatureUsers.AddAsync(featureUser);
            await DbContext.SaveChangesAsync();

            // Pre-populate cache
            var userCacheKey = $"feature:{featureName}:user:{user.Id}";
            await _cache.SetStringAsync(userCacheKey, "True");

            // Act
            await _repository.RemoveUserFromFeatureAsync(featureName, user.Id);

            // Assert
            var cachedValue = await _cache.GetStringAsync(userCacheKey);
            cachedValue.ShouldBeNull();
        }
    }
}