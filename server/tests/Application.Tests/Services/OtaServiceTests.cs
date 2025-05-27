using Application.Interfaces.Communication.Notifiers;
using Application.Interfaces.Communication.Publishers;
using Application.Interfaces.Data.Repositories;
using Application.Interfaces.Data.Repositories.Sourdough;
using Application.Models.DTOs;
using Application.Services;
using Core.Entities;
using Core.Exceptions;
using Core.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Application.Tests.Services;

public class OtaServiceTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly Mock<IFirmwareRepository> _firmwareRepositoryMock;
    private readonly Mock<ISourdoughAnalyzerRepository> _analyzerRepositoryMock;
    private readonly Mock<IFirmwareStorageService> _firmwareStorageMock;
    private readonly Mock<IFirmwareUpdateBackgroundService> _backgroundServiceMock;
    private readonly Mock<IOtaPublisher> _otaPublisherMock;
    private readonly Mock<IUserNotifier> _userNotifierMock;
    private readonly Mock<ILogger<OtaService>> _loggerMock;
    private readonly OtaService _service;

    private OtaServiceTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _firmwareRepositoryMock = new Mock<IFirmwareRepository>();
        _analyzerRepositoryMock = new Mock<ISourdoughAnalyzerRepository>();
        _firmwareStorageMock = new Mock<IFirmwareStorageService>();
        _backgroundServiceMock = new Mock<IFirmwareUpdateBackgroundService>();
        _otaPublisherMock = new Mock<IOtaPublisher>();
        _userNotifierMock = new Mock<IUserNotifier>();
        _loggerMock = new Mock<ILogger<OtaService>>();
        
        _service = new OtaService(
            _firmwareRepositoryMock.Object,
            _analyzerRepositoryMock.Object,
            _firmwareStorageMock.Object,
            _backgroundServiceMock.Object,
            _otaPublisherMock.Object,
            _userNotifierMock.Object,
            _loggerMock.Object);
    }

    public class StartOtaUpdateAsync(ITestOutputHelper outputHelper) : OtaServiceTests(outputHelper)
    {
        [Fact]
        public async Task StartOtaUpdateAsync_WithValidInputs_StartsUpdateSuccessfully()
        {
            // Arrange
            var analyzerId = Guid.NewGuid();
            var firmwareVersionId = Guid.NewGuid();
            var updateId = Guid.NewGuid();
            var firmwareData = new byte[] { 0x01, 0x02, 0x03, 0x04 };
            var crc32 = 0x12345678u;
            var version = "1.2.3";
            
            var firmware = new FirmwareVersion
            {
                Id = firmwareVersionId,
                Version = version
            };
            
            var capturedUpdate = null as FirmwareUpdate;
            
            _firmwareStorageMock.Setup(x => x.FirmwareExists(firmwareVersionId)).Returns(true);
            _firmwareStorageMock.Setup(x => x.ReadFirmwareAsync(firmwareVersionId)).ReturnsAsync(firmwareData);
            _firmwareStorageMock.Setup(x => x.CalculateCrc32(firmwareData)).Returns(crc32);
            _firmwareRepositoryMock.Setup(x => x.GetByIdAsync(firmwareVersionId)).ReturnsAsync(firmware);
            _firmwareRepositoryMock.Setup(x => x.CreateUpdateAsync(It.IsAny<FirmwareUpdate>()))
                .Callback<FirmwareUpdate>(u => 
                {
                    capturedUpdate = u;
                    u.Id = updateId;
                })
                .ReturnsAsync((FirmwareUpdate u) => u);
            
            // Act
            var result = await _service.StartOtaUpdateAsync(analyzerId, firmwareVersionId);
            
            // Assert
            result.ShouldBe(updateId);
            capturedUpdate.ShouldNotBeNull();
            capturedUpdate.AnalyzerId.ShouldBe(analyzerId);
            capturedUpdate.FirmwareVersionId.ShouldBe(firmwareVersionId);
            capturedUpdate.Status.ShouldBe(FirmwareUpdate.OtaStatus.Started);
            capturedUpdate.Progress.ShouldBe(0);
            
            _otaPublisherMock.Verify(x => x.PublishStartOtaAsync(It.Is<OtaStartCommand>(cmd =>
                cmd.AnalyzerId == analyzerId.ToString() &&
                cmd.Version == version &&
                cmd.Size == firmwareData.Length &&
                cmd.Crc32 == crc32
            )), Times.Once);
            
            _userNotifierMock.Verify(x => x.NotifyOtaProgressAsync(analyzerId, It.Is<OtaProgressUpdate>(u =>
                u.AnalyzerId == analyzerId &&
                u.UpdateId == updateId &&
                u.Status == FirmwareUpdate.OtaStatus.Started &&
                u.Progress == 0
            )), Times.Once);
            
            _backgroundServiceMock.Verify(x => x.StartFirmwareTransferAsync(analyzerId, updateId, firmwareData), Times.Once);
        }

        [Fact]
        public async Task StartOtaUpdateAsync_FirmwareNotInStorage_ThrowsEntityNotFoundException()
        {
            // Arrange
            var analyzerId = Guid.NewGuid();
            var firmwareVersionId = Guid.NewGuid();
            
            _firmwareStorageMock.Setup(x => x.FirmwareExists(firmwareVersionId)).Returns(false);
            
            // Act & Assert
            await Should.ThrowAsync<EntityNotFoundException>(() => 
                _service.StartOtaUpdateAsync(analyzerId, firmwareVersionId));
        }

        [Fact]
        public async Task StartOtaUpdateAsync_FirmwareNotInRepository_ThrowsEntityNotFoundException()
        {
            // Arrange
            var analyzerId = Guid.NewGuid();
            var firmwareVersionId = Guid.NewGuid();
            var firmwareData = new byte[] { 0x01, 0x02 };
            
            _firmwareStorageMock.Setup(x => x.FirmwareExists(firmwareVersionId)).Returns(true);
            _firmwareStorageMock.Setup(x => x.ReadFirmwareAsync(firmwareVersionId)).ReturnsAsync(firmwareData);
            _firmwareStorageMock.Setup(x => x.CalculateCrc32(firmwareData)).Returns(0x12345678u);
            _firmwareRepositoryMock.Setup(x => x.GetByIdAsync(firmwareVersionId)).ReturnsAsync((FirmwareVersion)null!);
            
            // Act & Assert
            await Should.ThrowAsync<EntityNotFoundException>(() => 
                _service.StartOtaUpdateAsync(analyzerId, firmwareVersionId));
        }

        [Fact]
        public async Task StartOtaUpdateAsync_WithLargeFirmware_HandlesCorrectly()
        {
            // Arrange
            var analyzerId = Guid.NewGuid();
            var firmwareVersionId = Guid.NewGuid();
            var updateId = Guid.NewGuid();
            var firmwareData = new byte[1024 * 1024]; // 1MB
            var crc32 = 0xABCDEF01u;
            var version = "2.0.0";
            
            var firmware = new FirmwareVersion
            {
                Id = firmwareVersionId,
                Version = version
            };
            
            _firmwareStorageMock.Setup(x => x.FirmwareExists(firmwareVersionId)).Returns(true);
            _firmwareStorageMock.Setup(x => x.ReadFirmwareAsync(firmwareVersionId)).ReturnsAsync(firmwareData);
            _firmwareStorageMock.Setup(x => x.CalculateCrc32(firmwareData)).Returns(crc32);
            _firmwareRepositoryMock.Setup(x => x.GetByIdAsync(firmwareVersionId)).ReturnsAsync(firmware);
            _firmwareRepositoryMock.Setup(x => x.CreateUpdateAsync(It.IsAny<FirmwareUpdate>()))
                .Callback<FirmwareUpdate>(u => u.Id = updateId)
                .ReturnsAsync((FirmwareUpdate u) => u);
            
            // Act
            var result = await _service.StartOtaUpdateAsync(analyzerId, firmwareVersionId);
            
            // Assert
            result.ShouldBe(updateId);
            
            _otaPublisherMock.Verify(x => x.PublishStartOtaAsync(It.Is<OtaStartCommand>(cmd =>
                cmd.Size == firmwareData.Length
            )), Times.Once);
        }
    }

    public class ProcessOtaStatusAsync(ITestOutputHelper outputHelper) : OtaServiceTests(outputHelper)
    {
        [Fact]
        public async Task ProcessOtaStatusAsync_WithActiveUpdate_UpdatesStatus()
        {
            // Arrange
            var analyzerId = Guid.NewGuid();
            var updateId = Guid.NewGuid();
            var firmwareVersionId = Guid.NewGuid();
            var status = new OtaStatusMessage(
                Status: FirmwareUpdate.OtaStatus.Downloading,
                Progress: 50,
                Message: "Halfway there"
            );
            
            var activeUpdate = new FirmwareUpdate
            {
                Id = updateId,
                AnalyzerId = analyzerId,
                FirmwareVersionId = firmwareVersionId,
                Status = FirmwareUpdate.OtaStatus.Started,
                Progress = 0
            };
            
            _firmwareRepositoryMock.Setup(x => x.GetActiveUpdateForAnalyzerAsync(analyzerId))
                .ReturnsAsync(activeUpdate);
            
            // Act
            await _service.ProcessOtaStatusAsync(analyzerId, status);
            
            // Assert
            _firmwareRepositoryMock.Verify(x => x.UpdateUpdateStatusAsync(updateId, status.Status, status.Progress), Times.Once);
            
            _userNotifierMock.Verify(x => x.NotifyOtaProgressAsync(analyzerId, It.Is<OtaProgressUpdate>(u =>
                u.AnalyzerId == analyzerId &&
                u.UpdateId == updateId &&
                u.Status == status.Status &&
                u.Progress == status.Progress &&
                u.Message == status.Message
            )), Times.Once);
        }

        [Fact]
        public async Task ProcessOtaStatusAsync_NoActiveUpdate_LogsWarning()
        {
            // Arrange
            var analyzerId = Guid.NewGuid();
            var status = new OtaStatusMessage(
                Status: FirmwareUpdate.OtaStatus.Downloading,
                Progress: 50,
                Message: null
            );
            
            _firmwareRepositoryMock.Setup(x => x.GetActiveUpdateForAnalyzerAsync(analyzerId))
                .ReturnsAsync((FirmwareUpdate)null!);
            
            // Act
            await _service.ProcessOtaStatusAsync(analyzerId, status);
            
            // Assert
            _firmwareRepositoryMock.Verify(x => x.UpdateUpdateStatusAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>()), Times.Never);
            _userNotifierMock.Verify(x => x.NotifyOtaProgressAsync(It.IsAny<Guid>(), It.IsAny<OtaProgressUpdate>()), Times.Never);
        }

        [Fact]
        public async Task ProcessOtaStatusAsync_StatusComplete_UpdatesAnalyzerFirmwareVersion()
        {
            // Arrange
            var analyzerId = Guid.NewGuid();
            var updateId = Guid.NewGuid();
            var firmwareVersionId = Guid.NewGuid();
            var newVersion = "2.0.0";
            
            var status = new OtaStatusMessage(
                Status: FirmwareUpdate.OtaStatus.Complete,
                Progress: 100,
                Message: "Update completed successfully"
            );
            
            var activeUpdate = new FirmwareUpdate
            {
                Id = updateId,
                AnalyzerId = analyzerId,
                FirmwareVersionId = firmwareVersionId,
                Status = FirmwareUpdate.OtaStatus.Downloading,
                Progress = 95
            };
            
            var firmware = new FirmwareVersion
            {
                Id = firmwareVersionId,
                Version = newVersion
            };
            
            var analyzer = new SourdoughAnalyzer
            {
                Id = analyzerId,
                FirmwareVersion = "1.0.0"
            };
            
            _firmwareRepositoryMock.Setup(x => x.GetActiveUpdateForAnalyzerAsync(analyzerId))
                .ReturnsAsync(activeUpdate);
            _firmwareRepositoryMock.Setup(x => x.GetByIdAsync(firmwareVersionId))
                .ReturnsAsync(firmware);
            _analyzerRepositoryMock.Setup(x => x.GetByIdAsync(analyzerId))
                .ReturnsAsync(analyzer);
            
            // Act
            await _service.ProcessOtaStatusAsync(analyzerId, status);
            
            // Assert
            analyzer.FirmwareVersion.ShouldBe(newVersion);
            _analyzerRepositoryMock.Verify(x => x.UpdateAsync(analyzer), Times.Once);
            _firmwareRepositoryMock.Verify(x => x.UpdateUpdateStatusAsync(updateId, status.Status, status.Progress), Times.Once);
        }

        [Fact]
        public async Task ProcessOtaStatusAsync_SameStatusAndProgress_DoesNotUpdate()
        {
            // Arrange
            var analyzerId = Guid.NewGuid();
            var updateId = Guid.NewGuid();
            var status = new OtaStatusMessage(
                Status: FirmwareUpdate.OtaStatus.Downloading,
                Progress: 50,
                Message: null
            );
            
            var activeUpdate = new FirmwareUpdate
            {
                Id = updateId,
                AnalyzerId = analyzerId,
                Status = FirmwareUpdate.OtaStatus.Downloading,
                Progress = 50 // Samme som indgående status 
            };
            
            _firmwareRepositoryMock.Setup(x => x.GetActiveUpdateForAnalyzerAsync(analyzerId))
                .ReturnsAsync(activeUpdate);
            
            // Act
            await _service.ProcessOtaStatusAsync(analyzerId, status);
            
            // Assert
            _firmwareRepositoryMock.Verify(x => x.UpdateUpdateStatusAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>()), Times.Never);
            
            // Men skal stadig notify 
            _userNotifierMock.Verify(x => x.NotifyOtaProgressAsync(analyzerId, It.IsAny<OtaProgressUpdate>()), Times.Once);
        }

        [Fact]
        public async Task ProcessOtaStatusAsync_WithErrorStatus_HandlesCorrectly()
        {
            // Arrange
            var analyzerId = Guid.NewGuid();
            var updateId = Guid.NewGuid();
            var status = new OtaStatusMessage(
                Status: FirmwareUpdate.OtaStatus.Failed,
                Progress: 25,
                Message: "CRC check failed"
            );
            
            var activeUpdate = new FirmwareUpdate
            {
                Id = updateId,
                AnalyzerId = analyzerId,
                Status = FirmwareUpdate.OtaStatus.Downloading,
                Progress = 25
            };
            
            _firmwareRepositoryMock.Setup(x => x.GetActiveUpdateForAnalyzerAsync(analyzerId))
                .ReturnsAsync(activeUpdate);
            
            // Act
            await _service.ProcessOtaStatusAsync(analyzerId, status);
            
            // Assert
            _firmwareRepositoryMock.Verify(x => x.UpdateUpdateStatusAsync(updateId, FirmwareUpdate.OtaStatus.Failed, 25), Times.Once);
            
            _userNotifierMock.Verify(x => x.NotifyOtaProgressAsync(analyzerId, It.Is<OtaProgressUpdate>(u =>
                u.Status == FirmwareUpdate.OtaStatus.Failed &&
                u.Message == "CRC check failed"
            )), Times.Once);
        }

        [Fact]
        public async Task ProcessOtaStatusAsync_NullMessage_UsesDefaultMessage()
        {
            // Arrange
            var analyzerId = Guid.NewGuid();
            var updateId = Guid.NewGuid();
            var status = new OtaStatusMessage(
                Status: FirmwareUpdate.OtaStatus.Downloading,
                Progress: 75,
                Message: null
            );
            
            var activeUpdate = new FirmwareUpdate
            {
                Id = updateId,
                AnalyzerId = analyzerId,
                Status = FirmwareUpdate.OtaStatus.Downloading,
                Progress = 50
            };
            
            _firmwareRepositoryMock.Setup(x => x.GetActiveUpdateForAnalyzerAsync(analyzerId))
                .ReturnsAsync(activeUpdate);
            
            // Act
            await _service.ProcessOtaStatusAsync(analyzerId, status);
            
            // Assert
            _userNotifierMock.Verify(x => x.NotifyOtaProgressAsync(analyzerId, It.Is<OtaProgressUpdate>(u =>
                u.Message == "OTA downloading - 75%"
            )), Times.Once);
        }
    }
}