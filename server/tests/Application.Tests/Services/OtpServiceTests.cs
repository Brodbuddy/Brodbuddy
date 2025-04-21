using Application.Interfaces.Data.Repositories;
using Application.Services;
using Moq;
using Shouldly;
using Xunit;

namespace Application.Tests.Services;

public class OtpServiceTests
{
    private readonly Mock<IOtpRepository> _repositoryMock;
    private readonly OtpService _service;

    protected OtpServiceTests()
    {
        _repositoryMock = new Mock<IOtpRepository>();
        _service = new OtpService(_repositoryMock.Object);
    }

    public class GenerateAsync : OtpServiceTests
    {
        [Fact]
        public async Task GenerateAsync_ReturnsSixDigitCode()
        {
            // Arrange
            _repositoryMock.Setup(r => r.SaveAsync(It.IsAny<int>())).ReturnsAsync(Guid.NewGuid());

            // Act
            var (_, code) = await _service.GenerateAsync();

            // Assert
            code.ShouldBeInRange(100000, 999999);
            _repositoryMock.Verify(
                repo => repo.SaveAsync(It.Is<int>(expectedCode => expectedCode >= 100000 && expectedCode <= 999999)),
                Times.Once);
        }
    }

    public class IsValidAsync : OtpServiceTests
    {
        [Fact]
        public async Task IsValidAsync_WithValidCode_ReturnsTrue()
        {
            // Arrange
            Guid id = Guid.NewGuid();
            int validcode = 111111;
            _repositoryMock.Setup(r => r.IsValidAsync(id, validcode)).ReturnsAsync(true);

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
        
        [Theory]
        [InlineData(99999, false)]   // Under min
        [InlineData(100000, true)]   // Præcis min
        [InlineData(500000, true)]   // Middel
        [InlineData(999999, true)]   // Præcis max
        [InlineData(1000000, false)] // Over max
        public async Task IsValidAsync_WithBoundaryValues_ReturnsExpectedResult(int testCode, bool expectedResult)
        {
            // Arrange
            Guid id = Guid.NewGuid();
            
            if (expectedResult)
            {
                _repositoryMock.Setup(r => r.IsValidAsync(id, testCode)).ReturnsAsync(true);
            }

            // Act
            bool result = await _service.IsValidAsync(id, testCode);

            // Assert
            result.ShouldBe(expectedResult);
            
            _repositoryMock.Verify(r => r.IsValidAsync(id, testCode), expectedResult ? Times.Once() : Times.Never());
        }
    }

    public class MarkAsUsedAsync : OtpServiceTests
    {
        [Fact]
        public async Task MarkAsUsedAsync_WithValidCode_ReturnsTrue()
        {
            // Arrange
            Guid id = Guid.NewGuid();
            _repositoryMock.Setup(r => r.MarkAsUsedAsync(id)).ReturnsAsync(true);

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
            _repositoryMock.Setup(r => r.MarkAsUsedAsync(id)).ReturnsAsync(true);

            // Act
            bool? result = await _service.MarkAsUsedAsync(id);

            // Assert
            result.ShouldNotBeNull();
        }
    }
}