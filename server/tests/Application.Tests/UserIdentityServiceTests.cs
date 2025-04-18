using Application.Interfaces;
using Application.Services;
using Core.Entities;
using Moq;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Application.Tests;

public class UserIdentityServiceTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly Mock<IUserIdentityRepository> _repositoryMock;
    private readonly UserIdentityService _service;

    private UserIdentityServiceTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _repositoryMock = new Mock<IUserIdentityRepository>();
        _service = new UserIdentityService(_repositoryMock.Object);
    }

    public static IEnumerable<object?[]> NullOrEmptyOrWhitespaceEmailData()
    {
        yield return [null];     
        yield return [""];    
        yield return ["      "]; 
    }
    
    public class CreateAsync(ITestOutputHelper outputHelper) : UserIdentityServiceTests(outputHelper)
    {
        [Theory]
        [InlineData("      User@mail.dk", "user@mail.dk")]
        [InlineData("user@mail.dk      ", "user@mail.dk")]
        [InlineData("  uSeR@mAiL.dK  ", "user@mail.dk")]
        [InlineData("user@mail.dk", "user@mail.dk")]
        public async Task CreateAsync_WithValidEmail_ReturnsExpectedGuid(string inputEmail, string normalizedEmail)
        {
            // Arrange
            var expectedId = Guid.NewGuid();
            _repositoryMock.Setup(r => r.SaveAsync(normalizedEmail))
                .ReturnsAsync(expectedId);

            // Act
            var result = await _service.CreateAsync(inputEmail);

            // Assert
            result.ShouldBe(expectedId);
            _repositoryMock.Verify(r => r.SaveAsync(normalizedEmail), Times.Once);
        }
        
        [Theory]
        [MemberData(nameof(NullOrEmptyOrWhitespaceEmailData))]
        public async Task CreateAsync_WithNullOrEmptyEmail_ThrowsArgumentException(string email)
        {
            // Act & Assert
            await Should.ThrowAsync<ArgumentException>(() =>
                _service.CreateAsync(email));
        }

        [Theory]
        [InlineData("test")]
        [InlineData("test@")]
        [InlineData("@test.com")]
        [InlineData(".test@test.com")]
        [InlineData("test@test")]
        public async Task CreateAsync_WithInvalidEmailFormat_ThrowsArgumentException(string email)
        {
            // Act & Assert
            await Should.ThrowAsync<ArgumentException>(() =>
                _service.CreateAsync(email));


        }

        [Fact]
        public async Task CreateAsync_WithExistingEmail_ReturnsExistingUserId()
        {
            // Arrange
            var email = "test@email.com";
            var existingId = Guid.NewGuid();
            var existingUser = new User { Id = existingId, Email = email };

            _repositoryMock.Setup(r => r.ExistsAsync(email))
                .ReturnsAsync(true);
            _repositoryMock.Setup(r => r.GetAsync(email))
                .ReturnsAsync(existingUser);

            // Act
            var result = await _service.CreateAsync(email);

            // Assert
            result.ShouldBe(existingId);
            _repositoryMock.Verify(r => r.ExistsAsync(email), Times.Once);
            _repositoryMock.Verify(r => r.GetAsync(email), Times.Once);
            _repositoryMock.Verify(r => r.SaveAsync(It.IsAny<string>()), Times.Never);
        }
    }

    public class ExistsAsync(ITestOutputHelper outputHelper) : UserIdentityServiceTests(outputHelper)
    {
        [Fact]
        public async Task ExistsAsync_WithExistingUserId_ReturnsTrue()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _repositoryMock.Setup(r => r.ExistsAsync(userId))
                .ReturnsAsync(true);

            // Act
            var result = await _service.ExistsAsync(userId);

            // Assert
            result.ShouldBeTrue();
            _repositoryMock.Verify(r => r.ExistsAsync(userId), Times.Once);
        }

        [Fact]
        public async Task ExistsAsync_WithNonExistingUserId_ReturnsFalse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _repositoryMock.Setup(r => r.ExistsAsync(userId))
                .ReturnsAsync(false);

            // Act
            var result = await _service.ExistsAsync(userId);

            // Assert
            result.ShouldBeFalse();
            _repositoryMock.Verify(r => r.ExistsAsync(userId), Times.Once);
        }

        [Fact]
        public async Task ExistsAsync_WithExistingEmail_ReturnsTrue()
        {
            // Arrange
            var email = "test@email.com";
            _repositoryMock.Setup(r => r.ExistsAsync(email))
                .ReturnsAsync(true);

            // Act
            var result = await _service.ExistsAsync(email);

            // Assert
            result.ShouldBeTrue();
            _repositoryMock.Verify(r => r.ExistsAsync(email), Times.Once);
        }

        [Fact]
        public async Task ExistsAsync_WithNonExistingEmail_ReturnsFalse()
        {
            // Arrange
            var email = "test@email.com";
            _repositoryMock.Setup(r => r.ExistsAsync(email))
                .ReturnsAsync(false);

            // Act
            var result = await _service.ExistsAsync(email);

            // Assert
            result.ShouldBeFalse();
            _repositoryMock.Verify(r => r.ExistsAsync(email), Times.Once);
        }
    }

    public class GetAsync(ITestOutputHelper outputHelper) : UserIdentityServiceTests(outputHelper)
    {
        [Fact]
        public async Task GetAsync_WithValidUserId_ReturnsUser()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var expectedUser = new User { Id = userId, Email = "test@email.com" };
            _repositoryMock.Setup(r => r.GetAsync(userId))
                .ReturnsAsync(expectedUser);

            // Act
            var result = await _service.GetAsync(userId);

            // Assert
            result.ShouldBe(expectedUser);
            _repositoryMock.Verify(r => r.GetAsync(userId), Times.Once);
        }

        [Fact]
        public async Task GetAsync_WithEmptyUserId_ThrowsArgumentException()
        {
            // Arrange
            var emptyId = Guid.Empty;

            // Act & Assert
            await Should.ThrowAsync<ArgumentException>(() =>
                _service.GetAsync(emptyId));

            _repositoryMock.Verify(r => r.GetAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task GetAsync_WithValidEmail_ReturnsUser()
        {
            // Arrange
            var email = "test@email.com";
            var expectedUser = new User { Id = Guid.NewGuid(), Email = email };
            _repositoryMock.Setup(r => r.GetAsync(email))
                .ReturnsAsync(expectedUser);

            // Act
            var result = await _service.GetAsync(email);

            // Assert
            result.ShouldBe(expectedUser);
            _repositoryMock.Verify(r => r.GetAsync(email), Times.Once);
        }

        [Theory]
        [MemberData(nameof(NullOrEmptyOrWhitespaceEmailData))]
        public async Task GetAsync_WithNullOrEmptyEmail_ThrowsArgumentException(string email)
        {
            // Act & Assert
            await Should.ThrowAsync<ArgumentException>(() =>
                _service.GetAsync(email));

            _repositoryMock.Verify(r => r.GetAsync(It.IsAny<string>()), Times.Never);
        }
    }
}