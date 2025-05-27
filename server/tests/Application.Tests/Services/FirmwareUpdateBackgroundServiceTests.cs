using Application.Interfaces.Communication.Notifiers;
using Application.Interfaces.Data.Repositories;
using Application.Services;
using Core.Entities;
using Core.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Application.Tests.Services;

public class FirmwareUpdateBackgroundServiceTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<ILogger<FirmwareUpdateBackgroundService>> _loggerMock;
    private readonly Mock<IServiceScope> _serviceScopeMock;
    private readonly Mock<IServiceProvider> _scopedServiceProviderMock;
    private readonly Mock<IFirmwareRepository> _firmwareRepositoryMock;
    private readonly Mock<IUserNotifier> _userNotifierMock;
    private readonly Mock<IFirmwareTransferService> _transferServiceMock;
    private readonly Mock<IServiceScopeFactory> _serviceScopeFactoryMock;
    private readonly FirmwareUpdateBackgroundService _service;

    private FirmwareUpdateBackgroundServiceTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _serviceProviderMock = new Mock<IServiceProvider>();
        _loggerMock = new Mock<ILogger<FirmwareUpdateBackgroundService>>();
        _serviceScopeMock = new Mock<IServiceScope>();
        _scopedServiceProviderMock = new Mock<IServiceProvider>();
        _firmwareRepositoryMock = new Mock<IFirmwareRepository>();
        _userNotifierMock = new Mock<IUserNotifier>();
        _transferServiceMock = new Mock<IFirmwareTransferService>();
        _serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
        
        // Setup service scope
        _serviceScopeMock.Setup(x => x.ServiceProvider).Returns(_scopedServiceProviderMock.Object);
        _serviceScopeFactoryMock.Setup(x => x.CreateScope()).Returns(_serviceScopeMock.Object);
        _serviceProviderMock.Setup(x => x.GetService(typeof(IServiceScopeFactory)))
            .Returns(_serviceScopeFactoryMock.Object);
        
        // Setup scoped services
        _scopedServiceProviderMock.Setup(x => x.GetService(typeof(IFirmwareRepository)))
            .Returns(_firmwareRepositoryMock.Object);
        _scopedServiceProviderMock.Setup(x => x.GetService(typeof(IUserNotifier)))
            .Returns(_userNotifierMock.Object);
        _scopedServiceProviderMock.Setup(x => x.GetService(typeof(IFirmwareTransferService)))
            .Returns(_transferServiceMock.Object);
        
        _service = new FirmwareUpdateBackgroundService(
            _serviceProviderMock.Object,
            _loggerMock.Object);
    }

    public class StartFirmwareTransferAsync(ITestOutputHelper outputHelper) : FirmwareUpdateBackgroundServiceTests(outputHelper)
    {
        [Fact]
        public async Task StartFirmwareTransferAsync_ReturnsImmediately()
        {
            // Arrange
            var analyzerId = Guid.NewGuid();
            var updateId = Guid.NewGuid();
            var firmwareData = new byte[] { 0x01, 0x02, 0x03 };
            
            var tcs = new TaskCompletionSource<bool>();
            _transferServiceMock.Setup(x => x.SendFirmwareAsync(It.IsAny<Guid>(), It.IsAny<byte[]>()))
                .Returns(async () =>
                {
                    await Task.Delay(100); // Simuler arbejde 
                    tcs.SetResult(true);
                });
            
            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            await _service.StartFirmwareTransferAsync(analyzerId, updateId, firmwareData);
            stopwatch.Stop();
            
            // Assert - Burde returnere med det samme 
            stopwatch.ElapsedMilliseconds.ShouldBeLessThan(50);
            
            // Vent på at bacgkroundtask er færdig 
            await tcs.Task;
        }

        [Fact]
        public async Task StartFirmwareTransferAsync_ExecutesTransferInBackground()
        {
            // Arrange
            var analyzerId = Guid.NewGuid();
            var updateId = Guid.NewGuid();
            var firmwareData = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
            
            var transferCompleted = new TaskCompletionSource<bool>();
            _transferServiceMock.Setup(x => x.SendFirmwareAsync(analyzerId, firmwareData))
                .Returns(Task.CompletedTask)
                .Callback(() => transferCompleted.SetResult(true));
            
            // Act
            await _service.StartFirmwareTransferAsync(analyzerId, updateId, firmwareData);
            
            // Assert - Vent på backgroundtask handling 
            var completed = await Task.WhenAny(transferCompleted.Task, Task.Delay(1000));
            completed.ShouldBe(transferCompleted.Task);
            
            _transferServiceMock.Verify(x => x.SendFirmwareAsync(analyzerId, firmwareData), Times.Once);
        }

        [Fact]
        public async Task StartFirmwareTransferAsync_UpdatesStatusToDownloading()
        {
            // Arrange
            var analyzerId = Guid.NewGuid();
            var updateId = Guid.NewGuid();
            var firmwareData = new byte[] { 0x01 };
            
            var statusUpdated = new TaskCompletionSource<bool>();
            _firmwareRepositoryMock.Setup(x => x.UpdateUpdateStatusAsync(updateId, FirmwareUpdate.OtaStatus.Downloading, 0))
                .Returns(Task.CompletedTask)
                .Callback(() => statusUpdated.SetResult(true));
            
            // Act
            await _service.StartFirmwareTransferAsync(analyzerId, updateId, firmwareData);
            
            // Assert
            var completed = await Task.WhenAny(statusUpdated.Task, Task.Delay(1000));
            completed.ShouldBe(statusUpdated.Task);
            
            _firmwareRepositoryMock.Verify(x => x.UpdateUpdateStatusAsync(updateId, FirmwareUpdate.OtaStatus.Downloading, 0), Times.Once);
        }

        [Fact]
        public async Task StartFirmwareTransferAsync_NotifiesUserOfProgress()
        {
            // Arrange
            var analyzerId = Guid.NewGuid();
            var updateId = Guid.NewGuid();
            var firmwareData = new byte[] { 0x01 };
            
            var notified = new TaskCompletionSource<bool>();
            _userNotifierMock.Setup(x => x.NotifyOtaProgressAsync(analyzerId, It.Is<OtaProgressUpdate>(u =>
                u.AnalyzerId == analyzerId &&
                u.UpdateId == updateId &&
                u.Status == FirmwareUpdate.OtaStatus.Downloading &&
                u.Progress == 0 &&
                u.Message == "Starting firmware download"
            )))
                .Returns(Task.CompletedTask)
                .Callback(() => notified.SetResult(true));
            
            // Act
            await _service.StartFirmwareTransferAsync(analyzerId, updateId, firmwareData);
            
            // Assert
            var completed = await Task.WhenAny(notified.Task, Task.Delay(1000));
            completed.ShouldBe(notified.Task);
        }

        [Fact]
        public async Task StartFirmwareTransferAsync_OnFailure_UpdatesStatusToFailed()
        {
            // Arrange
            var analyzerId = Guid.NewGuid();
            var updateId = Guid.NewGuid();
            var firmwareData = new byte[] { 0x01 };
            var exception = new Exception("Transfer failed");
            
            var failureHandled = new TaskCompletionSource<bool>();
            
            _transferServiceMock.Setup(x => x.SendFirmwareAsync(analyzerId, firmwareData))
                .ThrowsAsync(exception);
            
            _firmwareRepositoryMock.Setup(x => x.UpdateUpdateStatusAsync(updateId, FirmwareUpdate.OtaStatus.Failed, null))
                .Returns(Task.CompletedTask)
                .Callback(() => failureHandled.SetResult(true));
            
            // Act
            await _service.StartFirmwareTransferAsync(analyzerId, updateId, firmwareData);
            
            // Assert
            var completed = await Task.WhenAny(failureHandled.Task, Task.Delay(5000));
            completed.ShouldBe(failureHandled.Task);
            
            _firmwareRepositoryMock.Verify(x => x.UpdateUpdateStatusAsync(updateId, FirmwareUpdate.OtaStatus.Failed, null), Times.Once);
        }

        [Fact]
        public async Task StartFirmwareTransferAsync_OnFailure_NotifiesUserWithError()
        {
            // Arrange
            var analyzerId = Guid.NewGuid();
            var updateId = Guid.NewGuid();
            var firmwareData = new byte[] { 0x01 };
            var exception = new Exception("Network error");
            
            var errorNotified = new TaskCompletionSource<bool>();
            
            _transferServiceMock.Setup(x => x.SendFirmwareAsync(analyzerId, firmwareData))
                .ThrowsAsync(exception);
            
            _userNotifierMock.Setup(x => x.NotifyOtaProgressAsync(analyzerId, It.Is<OtaProgressUpdate>(u =>
                u.AnalyzerId == analyzerId &&
                u.UpdateId == updateId &&
                u.Status == FirmwareUpdate.OtaStatus.Failed &&
                u.Progress == 0 &&
                u.Message != null && u.Message.Contains("Network error")
            )))
                .Returns(Task.CompletedTask)
                .Callback(() => errorNotified.SetResult(true));
            
            // Act
            await _service.StartFirmwareTransferAsync(analyzerId, updateId, firmwareData);
            
            // Assert
            var completed = await Task.WhenAny(errorNotified.Task, Task.Delay(1000));
            completed.ShouldBe(errorNotified.Task);
        }

        [Fact]
        public async Task StartFirmwareTransferAsync_CreatesNewScope()
        {
            // Arrange
            var analyzerId = Guid.NewGuid();
            var updateId = Guid.NewGuid();
            var firmwareData = new byte[] { 0x01 };
            
            var scopeCreated = new TaskCompletionSource<bool>();
            _serviceScopeFactoryMock.Setup(x => x.CreateScope())
                .Returns(_serviceScopeMock.Object)
                .Callback(() => scopeCreated.SetResult(true));
            
            // Act
            await _service.StartFirmwareTransferAsync(analyzerId, updateId, firmwareData);
            
            // Assert
            var completed = await Task.WhenAny(scopeCreated.Task, Task.Delay(1000));
            completed.ShouldBe(scopeCreated.Task);
            
            _serviceScopeFactoryMock.Verify(x => x.CreateScope(), Times.Once);
        }

        [Fact]
        public async Task StartFirmwareTransferAsync_DisposesScope()
        {
            // Arrange
            var analyzerId = Guid.NewGuid();
            var updateId = Guid.NewGuid();
            var firmwareData = new byte[] { 0x01 };
            
            var scopeDisposed = new TaskCompletionSource<bool>();
            _serviceScopeMock.Setup(x => x.Dispose())
                .Callback(() => scopeDisposed.SetResult(true));
            
            // Act
            await _service.StartFirmwareTransferAsync(analyzerId, updateId, firmwareData);
            
            // Assert
            var completed = await Task.WhenAny(scopeDisposed.Task, Task.Delay(1000));
            completed.ShouldBe(scopeDisposed.Task);
            
            _serviceScopeMock.Verify(x => x.Dispose(), Times.Once);
        }
    }
}