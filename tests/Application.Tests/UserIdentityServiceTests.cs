using Application.Interfaces;
using Application.Services;
using Moq;
using SharedTestDependencies;
using Shouldly;

namespace Application.Tests;

public class UserIdentityServiceTests
{
    private readonly Mock<IUserIdentityRepository> _repositoryMock;
    private readonly UserIdentityService _service;
    private readonly FakeTimeProvider _timeProvider;

    public UserIdentityServiceTests()
    {
        _repositoryMock = new Mock<IUserIdentityRepository>();
        _service = new UserIdentityService(_repositoryMock.Object, _timeProvider);
    }

    [Fact]
    public async Task CreateAsync_WithValidEmail_ReturnsExpectedGuid()
    {
        // Arrange
        var email = "test@email.com";
        var expectedId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.SaveAsync(email))
            .ReturnsAsync(expectedId);

        // Act
        var result = await _service.CreateAsync(email);

        // Assert
        result.ShouldBe(expectedId);
        _repositoryMock.Verify(r => r.SaveAsync(email), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("      ")]
    public async Task CreateAsync_WithNullOrEmptyEmail_ThrowsArgumentException(string email)
    {
        // Act & Assert
        var expection = await Should.ThrowAsync<ArgumentException>(() =>
            _service.CreateAsync(email));
        expection.ParamName.ShouldBe("email");
        expection.Message.ShouldContain("Email cannot be null or empty");

    }

    [Theory]
    [InlineData("test")]
    [InlineData("test@")]
    [InlineData("@test.com")]
    [InlineData(".test@test.com")]
    [InlineData("test@test")]
    public async Task CreateAsync_WithInvalidEmailFormat_ThrowsArugmentException(string email)
    {
        // Act & Assert
        var exception = await Should.ThrowAsync<ArgumentException>(() => 
            _service.CreateAsync(email));
        
        exception.ParamName.ShouldBe("email");
        exception.Message.ShouldContain("Invalid email format");
    }
    
   
}