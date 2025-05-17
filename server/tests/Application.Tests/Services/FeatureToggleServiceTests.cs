using Application.Interfaces;
using Application.Services;
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

    public class IsEnabled(ITestOutputHelper outputHelper) : FeatureToggleServiceTests(outputHelper)
    {
        [Fact]
        public void IsEnabled_WithEnabledFeature_ReturnsTrue()
        {
            // Arrange
            var featureName = "enabled_feature";
            _repositoryMock.Setup(r => r.IsEnabledAsync(featureName))
                .ReturnsAsync(true);

            // Act
            var result = _service.IsEnabled(featureName);

            // Assert
            result.ShouldBeTrue();
            _repositoryMock.Verify(r => r.IsEnabledAsync(featureName), Times.Once);
        }

        [Fact]
        public void IsEnabled_WithDisabledFeature_ReturnsFalse()
        {
            // Arrange
            var featureName = "disabled_feature";
            _repositoryMock.Setup(r => r.IsEnabledAsync(featureName))
                .ReturnsAsync(false);

            // Act
            var result = _service.IsEnabled(featureName);

            // Assert
            result.ShouldBeFalse();
            _repositoryMock.Verify(r => r.IsEnabledAsync(featureName), Times.Once);
        }

        [Fact]
        public void IsEnabled_WithNullFeatureName_ReturnsTrue()
        {
            // Arrange
            string? featureName = null;
            _repositoryMock.Setup(r => r.IsEnabledAsync(It.IsAny<string>()))
                .ReturnsAsync(true);

            // Act
            var result = _service.IsEnabled(featureName!);

            // Assert
            result.ShouldBeTrue();
            _repositoryMock.Verify(r => r.IsEnabledAsync(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void IsEnabled_WithEmptyFeatureName_ReturnsFalse()
        {
            // Arrange
            var featureName = string.Empty;
            _repositoryMock.Setup(r => r.IsEnabledAsync(featureName))
                .ReturnsAsync(false);

            // Act
            var result = _service.IsEnabled(featureName);

            // Assert
            result.ShouldBeFalse();
            _repositoryMock.Verify(r => r.IsEnabledAsync(featureName), Times.Once);
        }
    }

    public class IsEnabledForUser(ITestOutputHelper outputHelper) : FeatureToggleServiceTests(outputHelper)
    {
        [Fact]
        public void IsEnabledForUser_WithEnabledFeatureForUser_ReturnsTrue()
        {
            // Arrange
            var featureName = "user_feature";
            var userId = Guid.NewGuid();
            _repositoryMock.Setup(r => r.IsEnabledForUserAsync(featureName, userId))
                .ReturnsAsync(true);

            // Act
            var result = _service.IsEnabledForUser(featureName, userId);

            // Assert
            result.ShouldBeTrue();
            _repositoryMock.Verify(r => r.IsEnabledForUserAsync(featureName, userId), Times.Once);
        }

        [Fact]
        public void IsEnabledForUser_WithDisabledFeatureForUser_ReturnsFalse()
        {
            // Arrange
            var featureName = "user_feature";
            var userId = Guid.NewGuid();
            _repositoryMock.Setup(r => r.IsEnabledForUserAsync(featureName, userId))
                .ReturnsAsync(false);

            // Act
            var result = _service.IsEnabledForUser(featureName, userId);

            // Assert
            result.ShouldBeFalse();
            _repositoryMock.Verify(r => r.IsEnabledForUserAsync(featureName, userId), Times.Once);
        }

        [Fact]
        public void IsEnabledForUser_WithNullFeatureName_ReturnsTrue()
        {
            // Arrange
            string? featureName = null;
            var userId = Guid.NewGuid();
            _repositoryMock.Setup(r => r.IsEnabledForUserAsync(It.IsAny<string>(), userId))
                .ReturnsAsync(true);

            // Act
            var result = _service.IsEnabledForUser(featureName!, userId);

            // Assert
            result.ShouldBeTrue();
            _repositoryMock.Verify(r => r.IsEnabledForUserAsync(It.IsAny<string>(), userId), Times.Once);
        }

        [Fact]
        public void IsEnabledForUser_WithEmptyGuid_ReturnsFalse()
        {
            // Arrange
            var featureName = "user_feature";
            var userId = Guid.Empty;
            _repositoryMock.Setup(r => r.IsEnabledForUserAsync(featureName, userId))
                .ReturnsAsync(false);

            // Act
            var result = _service.IsEnabledForUser(featureName, userId);

            // Assert
            result.ShouldBeFalse();
            _repositoryMock.Verify(r => r.IsEnabledForUserAsync(featureName, userId), Times.Once);
        }
    }
}