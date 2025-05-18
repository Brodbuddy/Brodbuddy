using Application.Interfaces.Data.Repositories;
using Application.Services;
using Core.Entities;
using Moq;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Application.Tests.Services;

public class FeatureToggleServiceTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly Mock<IFeatureToggleRepository> _repositoryMock;
    private readonly FeatureToggleService _service;

    private FeatureToggleServiceTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _repositoryMock = new Mock<IFeatureToggleRepository>();
        _service = new FeatureToggleService(_repositoryMock.Object);
    }

    public class IsEnabledAsync(ITestOutputHelper outputHelper) : FeatureToggleServiceTests(outputHelper)
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task IsEnabledAsync_WithFeatureStatus_ReturnsExpectedStatus(bool isEnabled)
        {
            // Arrange
            var featureName = $"feature_{isEnabled}";
            _repositoryMock.Setup(r => r.IsEnabledAsync(featureName)).ReturnsAsync(isEnabled);

            // Act
            var result = await _service.IsEnabledAsync(featureName);

            // Assert
            result.ShouldBe(isEnabled);
            _repositoryMock.Verify(r => r.IsEnabledAsync(featureName), Times.Once);
        }

        [Fact]
        public async Task IsEnabledAsync_WithNullFeatureName_ReturnsTrue()
        {
            // Arrange
            string? featureName = null;
            _repositoryMock.Setup(r => r.IsEnabledAsync(It.IsAny<string>())).ReturnsAsync(true);

            // Act
            var result = await _service.IsEnabledAsync(featureName!);

            // Assert
            result.ShouldBeTrue();
            _repositoryMock.Verify(r => r.IsEnabledAsync(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task IsEnabledAsync_WithEmptyFeatureName_ReturnsFalse()
        {
            // Arrange
            var featureName = string.Empty;
            _repositoryMock.Setup(r => r.IsEnabledAsync(featureName)).ReturnsAsync(false);

            // Act
            var result = await _service.IsEnabledAsync(featureName);

            // Assert
            result.ShouldBeFalse();
            _repositoryMock.Verify(r => r.IsEnabledAsync(featureName), Times.Once);
        }
    }

    public class IsEnabledForUserAsync(ITestOutputHelper outputHelper) : FeatureToggleServiceTests(outputHelper)
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task IsEnabledForUserAsync_WithFeatureStatus_ReturnsExpectedStatus(bool isEnabled)
        {
            // Arrange
            var featureName = "user_feature";
            var userId = Guid.NewGuid();
            _repositoryMock.Setup(r => r.IsEnabledForUserAsync(featureName, userId)).ReturnsAsync(isEnabled);

            // Act
            var result = await _service.IsEnabledForUserAsync(featureName, userId);

            // Assert
            result.ShouldBe(isEnabled);
            _repositoryMock.Verify(r => r.IsEnabledForUserAsync(featureName, userId), Times.Once);
        }

        [Fact]
        public async Task IsEnabledForUserAsync_WithNullFeatureName_ReturnsTrue()
        {
            // Arrange
            string? featureName = null;
            var userId = Guid.NewGuid();
            _repositoryMock.Setup(r => r.IsEnabledForUserAsync(It.IsAny<string>(), userId)).ReturnsAsync(true);

            // Act
            var result = await _service.IsEnabledForUserAsync(featureName!, userId);

            // Assert
            result.ShouldBeTrue();
            _repositoryMock.Verify(r => r.IsEnabledForUserAsync(It.IsAny<string>(), userId), Times.Once);
        }

        [Fact]
        public async Task IsEnabledForUserAsync_WithEmptyGuid_ReturnsFalse()
        {
            // Arrange
            var featureName = "user_feature";
            var userId = Guid.Empty;
            _repositoryMock.Setup(r => r.IsEnabledForUserAsync(featureName, userId)).ReturnsAsync(false);

            // Act
            var result = await _service.IsEnabledForUserAsync(featureName, userId);

            // Assert
            result.ShouldBeFalse();
            _repositoryMock.Verify(r => r.IsEnabledForUserAsync(featureName, userId), Times.Once);
        }
    }

    public class SetRolloutPercentageAsync(ITestOutputHelper outputHelper) : FeatureToggleServiceTests(outputHelper)
    {
        [Fact]
        public async Task SetRolloutPercentageAsync_WithValidInputs_DelegatesToRepository()
        {
            // Arrange
            var featureName = "rollout_feature";
            var percentage = 50;
            _repositoryMock.Setup(r => r.SetRolloutPercentageAsync(featureName, percentage)).ReturnsAsync(true);

            // Act
            var result = await _service.SetRolloutPercentageAsync(featureName, percentage);

            // Assert
            result.ShouldBeTrue();
            _repositoryMock.Verify(r => r.SetRolloutPercentageAsync(featureName, percentage), Times.Once);
        }

        [Fact]
        public async Task SetRolloutPercentageAsync_WithInvalidFeature_ReturnsFalse()
        {
            // Arrange
            var featureName = "non_existent_feature";
            var percentage = 30;
            _repositoryMock.Setup(r => r.SetRolloutPercentageAsync(featureName, percentage)).ReturnsAsync(false);

            // Act
            var result = await _service.SetRolloutPercentageAsync(featureName, percentage);

            // Assert
            result.ShouldBeFalse();
            _repositoryMock.Verify(r => r.SetRolloutPercentageAsync(featureName, percentage), Times.Once);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(100)]
        public async Task SetRolloutPercentageAsync_WithBoundaryPercentage_ReturnsTrue(int percentage)
        {
            // Arrange
            var featureName = "boundary_test_feature";
            _repositoryMock.Setup(r => r.SetRolloutPercentageAsync(featureName, percentage)).ReturnsAsync(true);

            // Act
            var result = await _service.SetRolloutPercentageAsync(featureName, percentage);

            // Assert
            result.ShouldBeTrue();
            _repositoryMock.Verify(r => r.SetRolloutPercentageAsync(featureName, percentage), Times.Once);
        }
    }
    
    public class GetAllFeaturesAsync(ITestOutputHelper outputHelper) : FeatureToggleServiceTests(outputHelper)
    {
        [Fact]
        public async Task GetAllFeaturesAsync_ReturnsFeatures()
        {
            // Arrange
            var features = new List<Feature>
            {
                new() { Id = Guid.NewGuid(), Name = "feature1", IsEnabled = true },
                new() { Id = Guid.NewGuid(), Name = "feature2", IsEnabled = false }
            };
            _repositoryMock.Setup(r => r.GetAllFeaturesAsync()).ReturnsAsync(features);
    
            // Act
            var result = await _service.GetAllFeaturesAsync();
    
            // Assert
            var resultList = result.ToList();
            resultList.Count.ShouldBe(2);
            resultList.ShouldContain(f => f.Name == "feature1" && f.IsEnabled);
            resultList.ShouldContain(f => f.Name == "feature2" && !f.IsEnabled);
            _repositoryMock.Verify(r => r.GetAllFeaturesAsync(), Times.Once);
        }
    
        [Fact]
        public async Task GetAllFeaturesAsync_WithNoFeatures_ReturnsEmptyList()
        {
            // Arrange
            _repositoryMock.Setup(r => r.GetAllFeaturesAsync()).ReturnsAsync(new List<Feature>());
    
            // Act
            var result = await _service.GetAllFeaturesAsync();
    
            // Assert
            result.ShouldBeEmpty();
            _repositoryMock.Verify(r => r.GetAllFeaturesAsync(), Times.Once);
        }
    }
    
    public class SetFeatureEnabledAsync(ITestOutputHelper outputHelper) : FeatureToggleServiceTests(outputHelper)
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task SetFeatureEnabledAsync_WithValidFeature_ReturnsTrue(bool enabled)
        {
            // Arrange
            var featureName = "test_feature";
            _repositoryMock.Setup(r => r.SetEnabledAsync(featureName, enabled)).ReturnsAsync(true);
    
            // Act
            var result = await _service.SetFeatureEnabledAsync(featureName, enabled);
    
            // Assert
            result.ShouldBeTrue();
            _repositoryMock.Verify(r => r.SetEnabledAsync(featureName, enabled), Times.Once);
        }
    
        [Fact]
        public async Task SetFeatureEnabledAsync_WithInvalidFeature_ReturnsFalse()
        {
            // Arrange
            var featureName = "non_existent_feature";
            _repositoryMock.Setup(r => r.SetEnabledAsync(featureName, It.IsAny<bool>())).ReturnsAsync(false);
    
            // Act
            var result = await _service.SetFeatureEnabledAsync(featureName, true);
    
            // Assert
            result.ShouldBeFalse();
            _repositoryMock.Verify(r => r.SetEnabledAsync(featureName, true), Times.Once);
        }
    
        [Fact]
        public async Task SetFeatureEnabledAsync_WithEmptyFeatureName_ReturnsFalse()
        {
            // Arrange
            var featureName = string.Empty;
            _repositoryMock.Setup(r => r.SetEnabledAsync(featureName, It.IsAny<bool>())).ReturnsAsync(false);
    
            // Act
            var result = await _service.SetFeatureEnabledAsync(featureName, true);
    
            // Assert
            result.ShouldBeFalse();
            _repositoryMock.Verify(r => r.SetEnabledAsync(featureName, true), Times.Once);
        }
    }
    
    public class AddUserToFeatureAsync(ITestOutputHelper outputHelper) : FeatureToggleServiceTests(outputHelper)
    {
        [Fact]
        public async Task AddUserToFeatureAsync_WithValidInputs_ReturnsTrue()
        {
            // Arrange
            var featureName = "user_feature";
            var userId = Guid.NewGuid();
            _repositoryMock.Setup(r => r.AddUserToFeatureAsync(featureName, userId)).ReturnsAsync(true);
    
            // Act
            var result = await _service.AddUserToFeatureAsync(featureName, userId);
    
            // Assert
            result.ShouldBeTrue();
            _repositoryMock.Verify(r => r.AddUserToFeatureAsync(featureName, userId), Times.Once);
        }
    
        [Fact]
        public async Task AddUserToFeatureAsync_WithInvalidFeature_ReturnsFalse()
        {
            // Arrange
            var featureName = "non_existent_feature";
            var userId = Guid.NewGuid();
            _repositoryMock.Setup(r => r.AddUserToFeatureAsync(featureName, userId)).ReturnsAsync(false);
    
            // Act
            var result = await _service.AddUserToFeatureAsync(featureName, userId);
    
            // Assert
            result.ShouldBeFalse();
            _repositoryMock.Verify(r => r.AddUserToFeatureAsync(featureName, userId), Times.Once);
        }
    
        [Fact]
        public async Task AddUserToFeatureAsync_WithEmptyGuid_ReturnsFalse()
        {
            // Arrange
            var featureName = "user_feature";
            var userId = Guid.Empty;
            _repositoryMock.Setup(r => r.AddUserToFeatureAsync(featureName, userId)).ReturnsAsync(false);
    
            // Act
            var result = await _service.AddUserToFeatureAsync(featureName, userId);
    
            // Assert
            result.ShouldBeFalse();
            _repositoryMock.Verify(r => r.AddUserToFeatureAsync(featureName, userId), Times.Once);
        }
    }
    
    public class RemoveUserFromFeatureAsync(ITestOutputHelper outputHelper) : FeatureToggleServiceTests(outputHelper)
    {
        [Fact]
        public async Task RemoveUserFromFeatureAsync_WithValidInputs_ReturnsTrue()
        {
            // Arrange
            var featureName = "user_feature";
            var userId = Guid.NewGuid();
            _repositoryMock.Setup(r => r.RemoveUserFromFeatureAsync(featureName, userId)).ReturnsAsync(true);
    
            // Act
            var result = await _service.RemoveUserFromFeatureAsync(featureName, userId);
    
            // Assert
            result.ShouldBeTrue();
            _repositoryMock.Verify(r => r.RemoveUserFromFeatureAsync(featureName, userId), Times.Once);
        }
    
        [Fact]
        public async Task RemoveUserFromFeatureAsync_WithInvalidFeature_ReturnsFalse()
        {
            // Arrange
            var featureName = "non_existent_feature";
            var userId = Guid.NewGuid();
            _repositoryMock.Setup(r => r.RemoveUserFromFeatureAsync(featureName, userId)).ReturnsAsync(false);
    
            // Act
            var result = await _service.RemoveUserFromFeatureAsync(featureName, userId);
    
            // Assert
            result.ShouldBeFalse();
            _repositoryMock.Verify(r => r.RemoveUserFromFeatureAsync(featureName, userId), Times.Once);
        }
    
        [Fact]
        public async Task RemoveUserFromFeatureAsync_WithNonExistentUser_ReturnsFalse()
        {
            // Arrange
            var featureName = "user_feature";
            var userId = Guid.NewGuid();
            _repositoryMock.Setup(r => r.RemoveUserFromFeatureAsync(featureName, userId)).ReturnsAsync(false);
    
            // Act
            var result = await _service.RemoveUserFromFeatureAsync(featureName, userId);
    
            // Assert
            result.ShouldBeFalse();
            _repositoryMock.Verify(r => r.RemoveUserFromFeatureAsync(featureName, userId), Times.Once);
        }
    
        [Fact]
        public async Task RemoveUserFromFeatureAsync_WithEmptyGuid_ReturnsFalse()
        {
            // Arrange
            var featureName = "user_feature";
            var userId = Guid.Empty;
            _repositoryMock.Setup(r => r.RemoveUserFromFeatureAsync(featureName, userId)).ReturnsAsync(false);
    
            // Act
            var result = await _service.RemoveUserFromFeatureAsync(featureName, userId);
    
            // Assert
            result.ShouldBeFalse();
            _repositoryMock.Verify(r => r.RemoveUserFromFeatureAsync(featureName, userId), Times.Once);
        }
    }
}