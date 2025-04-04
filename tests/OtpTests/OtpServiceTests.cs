using Xunit;
using Shouldly;
using Application;
using Application.Interfaces;
using Application.Services;
using Moq;

namespace OtpTests;

public class OtpServiceTests
{
    private readonly Mock<IOtpRepository> _repositoryMock;
    private readonly OtpService _service;

    public OtpServiceTests()
    {
        _repositoryMock = new Mock<IOtpRepository>();
        _service = new OtpService(_repositoryMock.Object);
    }
    
    [Fact]
    public async Task GenerateAsync_ReturnsSixDigitCode()
    {
        // Arrange
        _repositoryMock.Setup(r => r.SaveAsync(It.IsAny<int>()))
            .ReturnsAsync(Guid.NewGuid());

        // Act
        int code = await _service.GenerateAsync();
        
        // Assert
        code.ShouldBeInRange(100000, 999999);
        _repositoryMock.Verify(repo => repo.SaveAsync(It.Is<int>(code => code >= 100000 && code <= 999999)), Times.Once);
    }

    [Fact]
    public async Task IsValidAsync_CallsRepository()
    {
        // Arrange
        Guid id = Guid.NewGuid();
        int validcode = 111111;
        _repositoryMock.Setup(r => r.IsValidAsync(id, validcode))
            .ReturnsAsync(true);
        
        // Act
        bool result = await _service.IsValidAsync(id, validcode);
        
        // Assert
        result.ShouldBeTrue();
        _repositoryMock.Verify(r => r.IsValidAsync(id, validcode), Times.Once);
    }

    [Fact]
    public async Task IsValidAsync_InvalidCodeFormat_ReturnsFalse()
    {
        // Arrange
        int invalidCode = 11111; // ikke 6 cifre lang kode.
        
        // Act
        bool result = await _service.IsValidAsync(Guid.NewGuid(), invalidCode);
        
        // Assert
        result.ShouldBeFalse();
        _repositoryMock.Verify(r => r.IsValidAsync(It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task IsValidAsync_ReturnsNonNullValue()
    {
        // Arrange
        Guid id = Guid.NewGuid();
        int validcode = 222222;
        _repositoryMock.Setup(r => r.IsValidAsync(id, validcode))
            .ReturnsAsync(true);
        
        // Act
        bool? result = await _service.IsValidAsync(Guid.NewGuid(), validcode);
        
        // Assert
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task MarkAsUsedAsync_CallsRepository()
    {
        // Arrange
        Guid id = Guid.NewGuid();
        _repositoryMock.Setup(r => r.MarkAsUsedAsync(id))
            .ReturnsAsync(true);
        
        // Act
        bool result = await _service.MarkAsUsedAsync(id);

        //Assert
        result.ShouldBeTrue();
        _repositoryMock.Verify(r => r.MarkAsUsedAsync(id), Times.Once);
    }

    [Fact]
    public async Task MarkAsUsedAsync_ReturnsNonNullValue()
    {
        // Arrange
        Guid id = Guid.NewGuid();
        _repositoryMock.Setup(r => r.MarkAsUsedAsync(id))
            .ReturnsAsync(true);
        
        // Act
        bool? result = await _service.MarkAsUsedAsync(id);
        
        // Assert
        result.ShouldNotBeNull();
    }
    
    
   
}