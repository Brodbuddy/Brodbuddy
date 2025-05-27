using Application.Interfaces.Data.Repositories;
using Application.Models.DTOs;
using Application.Services;
using Core.Entities;
using Core.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Application.Tests.Services;

public class FirmwareManagementServiceTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly Mock<IFirmwareRepository> _firmwareRepositoryMock;
    private readonly Mock<IFirmwareStorageService> _firmwareStorageMock;
    private readonly Mock<ILogger<FirmwareManagementService>> _loggerMock;
    private readonly FirmwareManagementService _service;

    private FirmwareManagementServiceTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _firmwareRepositoryMock = new Mock<IFirmwareRepository>();
        _firmwareStorageMock = new Mock<IFirmwareStorageService>();
        _loggerMock = new Mock<ILogger<FirmwareManagementService>>();
        
        _service = new FirmwareManagementService(
            _firmwareRepositoryMock.Object,
            _firmwareStorageMock.Object,
            _loggerMock.Object);
    }

    public class UploadFirmwareAsync(ITestOutputHelper outputHelper) : FirmwareManagementServiceTests(outputHelper)
    {
        [Fact]
        public async Task UploadFirmwareAsync_WithValidData_CreatesAndStoresFirmware()
        {
            // Arrange
            var firmwareData = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
            var crc32 = 0x12345678u;
            var userId = Guid.NewGuid();
            var firmwareId = Guid.NewGuid();
            
            var upload = new FirmwareUpload(
                Data: firmwareData,
                Version: "1.2.3",
                Description: "Test firmware",
                ReleaseNotes: "Fixed bugs",
                IsStable: true,
                CreatedBy: userId
            );
            
            FirmwareVersion capturedFirmware = null!;
            
            _firmwareStorageMock.Setup(x => x.CalculateCrc32(firmwareData)).Returns(crc32);
            _firmwareRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<FirmwareVersion>()))
                .Callback<FirmwareVersion>(f => 
                {
                    capturedFirmware = f;
                    f.Id = firmwareId;
                })
                .ReturnsAsync((FirmwareVersion f) => f);
            
            // Act
            var result = await _service.UploadFirmwareAsync(upload);
            
            // Assert
            result.ShouldNotBeNull();
            result.FirmwareId.ShouldBe(firmwareId);
            result.Version.ShouldBe("1.2.3");
            result.Size.ShouldBe(firmwareData.Length);
            result.Crc32.ShouldBe(crc32);
            
            capturedFirmware.ShouldNotBeNull();
            capturedFirmware.Version.ShouldBe("1.2.3");
            capturedFirmware.Description.ShouldBe("Test firmware");
            capturedFirmware.FileSize.ShouldBe(firmwareData.Length);
            capturedFirmware.Crc32.ShouldBe((int)crc32);
            capturedFirmware.ReleaseNotes.ShouldBe("Fixed bugs");
            capturedFirmware.IsStable.ShouldBeTrue();
            capturedFirmware.CreatedBy.ShouldBe(userId);
            
            _firmwareStorageMock.Verify(x => x.SaveFirmwareAsync(firmwareId, firmwareData), Times.Once);
        }

        [Fact]
        public async Task UploadFirmwareAsync_WithEmptyData_ThrowsBusinessRuleViolationException()
        {
            // Arrange
            var upload = new FirmwareUpload(
                Data: [],
                Version: "1.0.0",
                Description: "Empty firmware",
                ReleaseNotes: null,
                IsStable: false,
                CreatedBy: null
            );
            
            // Act & Assert
            await Should.ThrowAsync<BusinessRuleViolationException>(() => _service.UploadFirmwareAsync(upload));
        }

        [Fact]
        public async Task UploadFirmwareAsync_WithLargeFirmware_HandlesCorrectly()
        {
            // Arrange
            var firmwareData = new byte[10 * 1024 * 1024]; // 10MB
            Random.Shared.NextBytes(firmwareData);
            var crc32 = 0x12345678u;
            
            var upload = new FirmwareUpload(
                Data: firmwareData,
                Version: "2.0.0-beta",
                Description: "Large firmware update",
                ReleaseNotes: "Major update with new features",
                IsStable: false,
                CreatedBy: null
            );
            
            _firmwareStorageMock.Setup(x => x.CalculateCrc32(firmwareData)).Returns(crc32);
            _firmwareRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<FirmwareVersion>()))
                .Callback<FirmwareVersion>(f => f.Id = Guid.NewGuid())
                .ReturnsAsync((FirmwareVersion f) => f);
            
            // Act
            var result = await _service.UploadFirmwareAsync(upload);
            
            // Assert
            result.Size.ShouldBe(firmwareData.Length);
            result.Crc32.ShouldBe(crc32);
            
            _firmwareRepositoryMock.Verify(x => x.CreateAsync(It.Is<FirmwareVersion>(f =>
                f.FileSize == firmwareData.Length &&
                !f.IsStable
            )), Times.Once);
        }

        [Fact]
        public async Task UploadFirmwareAsync_WithNullOptionalFields_HandlesCorrectly()
        {
            // Arrange
            var firmwareData = new byte[] { 0xFF, 0xFE };
            var crc32 = 0xABCDEF01u;
            
            var upload = new FirmwareUpload(
                Data: firmwareData,
                Version: "0.1.0",
                Description: "Minimal firmware",
                ReleaseNotes: null,
                IsStable: true,
                CreatedBy: null
            );
            
            FirmwareVersion capturedFirmware = null!;
            
            _firmwareStorageMock.Setup(x => x.CalculateCrc32(firmwareData)).Returns(crc32);
            _firmwareRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<FirmwareVersion>()))
                .Callback<FirmwareVersion>(f => 
                {
                    capturedFirmware = f;
                    f.Id = Guid.NewGuid();
                })
                .ReturnsAsync((FirmwareVersion f) => f);
            
            // Act
            await _service.UploadFirmwareAsync(upload);
            
            // Assert
            capturedFirmware.ReleaseNotes.ShouldBeNull();
            capturedFirmware.CreatedBy.ShouldBeNull();
        }

        [Fact]
        public async Task UploadFirmwareAsync_RepositoryFailure_PropagatesException()
        {
            // Arrange
            var firmwareData = new byte[] { 0x01 };
            var upload = new FirmwareUpload(
                Data: firmwareData,
                Version: "1.0.0",
                Description: "Test",
                ReleaseNotes: null,
                IsStable: false,
                CreatedBy: null
            );
            
            _firmwareStorageMock.Setup(x => x.CalculateCrc32(firmwareData)).Returns(0x12345678u);
            _firmwareRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<FirmwareVersion>()))
                .ThrowsAsync(new Exception("Database error"));
            
            // Act & Assert
            await Should.ThrowAsync<Exception>(() => _service.UploadFirmwareAsync(upload));
            
            // SKal ikke gemme til stoarge hvis repository fejler 
            _firmwareStorageMock.Verify(x => x.SaveFirmwareAsync(It.IsAny<Guid>(), It.IsAny<byte[]>()), Times.Never);
        }
    }

    public class GetAllFirmwareVersionsAsync(ITestOutputHelper outputHelper) : FirmwareManagementServiceTests(outputHelper)
    {
        [Fact]
        public async Task GetAllFirmwareVersionsAsync_ReturnsAllVersions()
        {
            // Arrange
            var versions = new List<FirmwareVersion>
            {
                new() { Id = Guid.NewGuid(), Version = "1.0.0", IsStable = true },
                new() { Id = Guid.NewGuid(), Version = "1.1.0", IsStable = true },
                new() { Id = Guid.NewGuid(), Version = "2.0.0-beta", IsStable = false }
            };
            
            _firmwareRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(versions);
            
            // Act
            var result = await _service.GetAllFirmwareVersionsAsync();
            
            // Assert
            var firmwareVersions = result.ToList();
            firmwareVersions.ShouldNotBeNull();
            firmwareVersions.Count.ShouldBe(3);
            firmwareVersions.ShouldContain(v => v.Version == "1.0.0");
            firmwareVersions.ShouldContain(v => v.Version == "1.1.0");
            firmwareVersions.ShouldContain(v => v.Version == "2.0.0-beta");
        }

        [Fact]
        public async Task GetAllFirmwareVersionsAsync_NoVersions_ReturnsEmptyList()
        {
            // Arrange
            _firmwareRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<FirmwareVersion>());
            
            // Act
            var result = await _service.GetAllFirmwareVersionsAsync();
            
            // Assert
            var firmwareVersions = result.ToList();
            firmwareVersions.ShouldNotBeNull();
            firmwareVersions.ShouldBeEmpty();
        }
    }
}