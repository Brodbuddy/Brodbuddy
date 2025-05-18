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

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task IsEnabledAsync_WithFeatureEnabledStatus_ReturnsExpectedStatus(bool isEnabled)
        {
            // Arrange
            var featureName = $"feature_{isEnabled}";
            await DbContext.SeedFeatureAsync(_timeProvider, featureName, isEnabled);

            // Act
            var result = await _repository.IsEnabledAsync(featureName);

            // Assert
            result.ShouldBe(isEnabled);
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

        [Theory]
        [InlineData(true, "False")]
        [InlineData(false, "True")]
        public async Task SetEnabledAsync_ChangingFeatureStatus_InvalidatesCache(bool newStatus, string cachedValue)
        {
            // Arrange
            var featureName = $"feature_to_{(newStatus ? "enable" : "disable")}";
            await _cache.SetStringAsync($"feature:{featureName}", cachedValue);

            // Act
            await _repository.SetEnabledAsync(featureName, newStatus);

            // Assert
            var cachedResult = await _cache.GetStringAsync($"feature:{featureName}");
            cachedResult.ShouldBeNull();
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
        public async Task IsEnabledForUserAsync_With100PercentRollout_ReturnsTrueForAllUsers()
        {
            // Arrange
            var featureName = "rollout_feature_100";
            await DbContext.SeedFeatureAsync(_timeProvider, featureName, true);
            await _repository.SetRolloutPercentageAsync(featureName, 100);
            
            var userId = Guid.NewGuid();

            // Act
            var result = await _repository.IsEnabledForUserAsync(featureName, userId);

            // Assert
            result.ShouldBeTrue();
        }
        
        [Fact]
        public async Task IsEnabledForUserAsync_WithRolloutPercentage_ReturnsTrueForIncludedUsers()
        {
            // Arrange
            var featureName = "rollout_feature_90";
            await DbContext.SeedFeatureAsync(_timeProvider, featureName, true);
            await _repository.SetRolloutPercentageAsync(featureName, 90);
            
            bool foundIncluded = false;
            Guid includedUserId = Guid.Empty;
            
            for (int i = 0; i < 100; i++)
            {
                var testId = Guid.NewGuid();
                var testResult = await _repository.IsEnabledForUserAsync(featureName, testId);
                
                if (!testResult) continue;
                includedUserId = testId;
                foundIncluded = true;
                break;
            }
            
            foundIncluded.ShouldBeTrue();

            // Act
            var result = await _repository.IsEnabledForUserAsync(featureName, includedUserId);

            // Assert
            result.ShouldBeTrue();
        }
        
        [Fact]
        public async Task IsEnabledForUserAsync_WithRolloutPercentage_ReturnsFalseForExcludedUsers()
        {
            // Arrange
            var featureName = "rollout_feature_1";
            await DbContext.SeedFeatureAsync(_timeProvider, featureName, true);
            await _repository.SetRolloutPercentageAsync(featureName, 1); 
            
            var excludedUserId = Guid.NewGuid();

            // Act
            var result = await _repository.IsEnabledForUserAsync(featureName, excludedUserId);

            // Assert
            result.ShouldBeFalse();
        }
        
        [Fact]
        public async Task IsEnabledForUserAsync_WithRolloutAndExplicitUser_ReturnsTrueForExplicitUser()
        {
            // Arrange
            var featureName = "rollout_with_explicit_user";
            var user = await DbContext.SeedUserAsync(_timeProvider);
            var feature = await DbContext.SeedFeatureAsync(_timeProvider, featureName, true);
            feature.RolloutPercentage = 0;
            await DbContext.SaveChangesAsync();
            
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
        public async Task IsEnabledForUserAsync_WithNoRolloutPercentage_UsesUserListOnly()
        {
            // Arrange
            var featureName = "no_rollout_feature";
            var user = await DbContext.SeedUserAsync(_timeProvider);
            var feature = await DbContext.SeedFeatureAsync(_timeProvider, featureName, true);
            
            var featureUser = new FeatureUser
            {
                FeatureId = feature.Id,
                UserId = user.Id
            };
            await DbContext.FeatureUsers.AddAsync(featureUser);
            await DbContext.SaveChangesAsync();
            
            var userNotInList = Guid.NewGuid();

            // Act
            var resultInList = await _repository.IsEnabledForUserAsync(featureName, user.Id);
            var resultNotInList = await _repository.IsEnabledForUserAsync(featureName, userNotInList);

            // Assert
            resultInList.ShouldBeTrue();
            resultNotInList.ShouldBeFalse();
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
        
        [Theory]
        [InlineData(10)]
        [InlineData(30)]
        [InlineData(50)]
        [InlineData(70)]
        [InlineData(90)]
        public async Task IsEnabledForUserAsync_WithSpecificRolloutPercentage_DistributesUsersCorrectly(int percentage)
        {
            // Arrange
            var testId = Guid.NewGuid().ToString("N");
            var featureName = $"rollout_feature_{percentage}_{testId}";
    
            await DbContext.SeedFeatureAsync(_timeProvider, featureName, true);
            await _repository.SetRolloutPercentageAsync(featureName, percentage);
    
            const int sampleSize = 1000;
            var enabledCount = 0;
    
            // Act
            for (int i = 0; i < sampleSize; i++)
            {
                var userId = Guid.NewGuid();
                var result = await _repository.IsEnabledForUserAsync(featureName, userId);
                if (result) enabledCount++;
            }
    
            // Assert
            var actualPercentage = enabledCount / (double) sampleSize * 100;
            var marginOfError = 6.0; 
    
            actualPercentage.ShouldBeInRange(percentage - marginOfError, percentage + marginOfError);
        }
        
        [Fact]
        public async Task IsEnabledForUserAsync_WithRolloutPercentage_DistributesUsersCorrectly_AcrossMultiplePercentages()
        {
            var random = new Random();
            
            // Test 10 tilfælde procenter mellem 10 og 90
            for (int test = 0; test < 10; test++)
            {
                var percentage = random.Next(10, 91);
                
                // Arrange
                var testId = Guid.NewGuid().ToString("N");
                var featureName = $"rollout_feature_{percentage}_{testId}";
                
                await DbContext.SeedFeatureAsync(_timeProvider, featureName, true);
                await _repository.SetRolloutPercentageAsync(featureName, percentage);
                
                const int sampleSize = 1000;
                var enabledCount = 0;
                
                // Act
                for (int i = 0; i < sampleSize; i++)
                {
                    var userId = Guid.NewGuid();
                    var result = await _repository.IsEnabledForUserAsync(featureName, userId);
                    if (result) enabledCount++;
                }
                
                // Assert
                var actualPercentage = enabledCount / (double)sampleSize * 100;
                var marginOfError = 6.0;
                
                actualPercentage.ShouldBeInRange(percentage - marginOfError, percentage + marginOfError);
            }
        }
        
        [Fact]
        public async Task IsEnabledForUserAsync_WithRolloutPercentage_IsDeterministic()
        {
            var random = new Random();
            
            // Test 10 random combinations
            for (int test = 0; test < 10; test++)
            {
                // Arrange
                var userId = Guid.NewGuid();
                var percentage = random.Next(0, 101);
                var testId = Guid.NewGuid().ToString("N");
                var featureName = $"deterministic_feature_{percentage}_{testId}";
                
                await DbContext.SeedFeatureAsync(_timeProvider, featureName, true);
                await _repository.SetRolloutPercentageAsync(featureName, percentage);
                
                // Act
                var result1 = await _repository.IsEnabledForUserAsync(featureName, userId);
                var result2 = await _repository.IsEnabledForUserAsync(featureName, userId);
                var result3 = await _repository.IsEnabledForUserAsync(featureName, userId);
                
                // Assert
                result1.ShouldBe(result2);
                result2.ShouldBe(result3);
            }
        }
        
        [Fact]
        public async Task IsEnabledForUserAsync_WithExplicitUser_ReturnsTrueRegardlessOfPercentage()
        {
            var random = new Random();
            
            // Test 10 random percentages
            for (int test = 0; test < 10; test++)
            {
                // Arrange
                var percentage = random.Next(0, 101);
                var testId = Guid.NewGuid().ToString("N");
                var featureName = $"explicit_user_feature_{percentage}_{testId}";
                var user = await DbContext.SeedUserAsync(_timeProvider);
                
                await DbContext.SeedFeatureAsync(_timeProvider, featureName, true);
                await _repository.SetRolloutPercentageAsync(featureName, percentage);
                await _repository.AddUserToFeatureAsync(featureName, user.Id);
                
                // Act
                var result = await _repository.IsEnabledForUserAsync(featureName, user.Id);
                
                // Assert
                result.ShouldBeTrue();
            }
        }
        
        [Theory]
        [InlineData(0, false)]
        [InlineData(100, true)]
        public async Task IsEnabledForUserAsync_WithBoundaryPercentages_ReturnsExpectedResult(int percentage, bool expectedResult)
        {
            // Arrange 
            var userId = Guid.NewGuid();
            var testId = Guid.NewGuid().ToString("N");
            var featureName = $"feature_{percentage}_{testId}";
    
            await DbContext.SeedFeatureAsync(_timeProvider, featureName, true);
            await _repository.SetRolloutPercentageAsync(featureName, percentage);
    
            // Act
            var result = await _repository.IsEnabledForUserAsync(featureName, userId);
    
            // Assert
            result.ShouldBe(expectedResult);
        }
        
        [Fact]
        public async Task IsEnabledForUserAsync_WithSameUserIdAndFeature_ReturnsSameResultConsistently()
        {
            // Arrange
            var featureName = "determinism_test";
            var userId = Guid.NewGuid();
            
            await DbContext.SeedFeatureAsync(_timeProvider, featureName, true);
            await _repository.SetRolloutPercentageAsync(featureName, 50);
            
            var userCacheKey = $"feature:{featureName}:user:{userId}";
            
            // Act
            var results = new List<bool>();
            for (int i = 0; i < 5; i++)
            {
                await _cache.RemoveAsync(userCacheKey); // Tving genberegning
                var result = await _repository.IsEnabledForUserAsync(featureName, userId);
                results.Add(result);
            }
            
            // Assert
            results.All(r => r == results[0]).ShouldBeTrue();
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
            var deletedFeatureUser = await DbContext.FeatureUsers.FirstOrDefaultAsync(fu => fu.FeatureId == feature.Id && fu.UserId == user.Id);
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
    
    public class SetRolloutPercentageAsync(PostgresFixture fixture) : FeatureToggleRepositoryTests(fixture)
    {
        [Fact]
        public async Task SetRolloutPercentageAsync_WithNonExistentFeature_ReturnsFalse()
        {
            // Arrange
            var featureName = "non_existent_feature";
            var percentage = 50;

            // Act
            var result = await _repository.SetRolloutPercentageAsync(featureName, percentage);

            // Assert
            result.ShouldBeFalse();
        }

        [Fact]
        public async Task SetRolloutPercentageAsync_WithExistingFeature_UpdatesAndReturnsTrue()
        {
            // Arrange
            var featureName = "test_feature";
            var percentage = 75;
            var feature = await DbContext.SeedFeatureAsync(_timeProvider, featureName, true);

            // Act
            var result = await _repository.SetRolloutPercentageAsync(featureName, percentage);

            // Assert
            result.ShouldBeTrue();
            var updatedFeature = await DbContext.Features.FirstAsync(f => f.Id == feature.Id);
            updatedFeature.RolloutPercentage.ShouldBe(percentage);
            updatedFeature.LastModifiedAt.ShouldNotBeNull();
        }

        [Fact]
        public async Task SetRolloutPercentageAsync_InvalidatesFeatureCache()
        {
            // Arrange
            var featureName = "test_feature";
            var percentage = 25;
            await DbContext.SeedFeatureAsync(_timeProvider, featureName, true);
            
            // Pre-populate cache
            await _cache.SetStringAsync($"feature:{featureName}", "true");

            // Act
            await _repository.SetRolloutPercentageAsync(featureName, percentage);

            // Assert
            var cachedValue = await _cache.GetStringAsync($"feature:{featureName}");
            cachedValue.ShouldBeNull();
        }
    }

    public class GetDjb2HashCode(PostgresFixture fixture) : FeatureToggleRepositoryTests(fixture)
    {
        [Fact]
        public async Task GetDjb2HashCode_WithMultipleRandomInputs_DistributesEvenly()
        {
            // Arrange
            var buckets = new int[10];
            const int sampleSize = 10000;
            var featureName = "hash_distribution_test";
            
            await DbContext.SeedFeatureAsync(_timeProvider, featureName, true);
            await _repository.SetRolloutPercentageAsync(featureName, 100);
            
            // Act
            for (int i = 0; i < sampleSize; i++)
            {
                var userId = Guid.NewGuid();
                var combinedString = $"{userId}:{featureName}";
                var combinedHash = PgFeatureToggleRepository.GetDjb2HashCode(combinedString);
                var normalizedHash = Math.Abs(combinedHash) % 100;
                var bucket = normalizedHash / 10;
                buckets[bucket]++;
            }
            
            // Assert
            // Hver bucket skal have ca. 1000 elementer (10000/10)
            foreach (var count in buckets)
            {
                count.ShouldBeInRange(800, 1200);
            }
        } 
    }
}