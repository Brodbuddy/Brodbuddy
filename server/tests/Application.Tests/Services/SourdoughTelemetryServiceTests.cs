using Application.Interfaces.Communication.Notifiers;
using Application.Interfaces.Communication.Publishers;
using Application.Interfaces.Data.Repositories.Sourdough;
using Application.Services.Sourdough;
using Core.ValueObjects;
using Moq;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Application.Tests.Services;

public class SourdoughTelemetryServiceTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly Mock<IUserNotifier> _userNotifierMock;
    private readonly Mock<IAdminNotifier> _adminNotifierMock;
    private readonly Mock<ISourdoughAnalyzerRepository> _analyzerRepositoryMock;
    private readonly Mock<IAnalyzerPublisher> _analyzerPublisherMock;
    private readonly Mock<IAnalyzerReadingRepository> _readingRepositoryMock;
    private readonly SourdoughTelemetryService _service;

    private SourdoughTelemetryServiceTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _userNotifierMock = new Mock<IUserNotifier>();
        _adminNotifierMock = new Mock<IAdminNotifier>();
        _analyzerRepositoryMock = new Mock<ISourdoughAnalyzerRepository>();
        _analyzerPublisherMock = new Mock<IAnalyzerPublisher>();
        _readingRepositoryMock = new Mock<IAnalyzerReadingRepository>();
        
        _service = new SourdoughTelemetryService(
            _userNotifierMock.Object, 
            _adminNotifierMock.Object,
            _analyzerRepositoryMock.Object,
            _analyzerPublisherMock.Object,
            _readingRepositoryMock.Object);
    }

    public class ProcessSourdoughReadingAsync(ITestOutputHelper outputHelper) : SourdoughTelemetryServiceTests(outputHelper)
    {
        [Fact]
        public async Task ProcessSourdoughReadingAsync_WithOwner_NotifiesOwner()
        {
            // Arrange
            var analyzerId = Guid.NewGuid();
            var ownerId = Guid.NewGuid();
            var reading = new SourdoughReading(
                Rise: 2.5,
                Temperature: 25.5,
                Humidity: 65.3,
                EpochTime: 1234567890,
                Timestamp: DateTime.UtcNow,
                LocalTime: DateTime.UtcNow
            );
            
            _analyzerRepositoryMock.Setup(x => x.GetOwnersUserIdAsync(analyzerId)).ReturnsAsync(ownerId);
            
            // Act
            await _service.ProcessSourdoughReadingAsync(analyzerId, reading);
            
            // Assert
            _userNotifierMock.Verify(x => x.NotifySourdoughReadingAsync(ownerId, reading), Times.Once);
        }

        [Fact]
        public async Task ProcessSourdoughReadingAsync_WithNoOwner_DoesNotNotify()
        {
            // Arrange
            var analyzerId = Guid.NewGuid();
            var reading = new SourdoughReading(
                Rise: 2.5,
                Temperature: 25.5,
                Humidity: 65.3,
                EpochTime: 1234567890,
                Timestamp: DateTime.UtcNow,
                LocalTime: DateTime.UtcNow
            );
            
            _analyzerRepositoryMock.Setup(x => x.GetOwnersUserIdAsync(analyzerId)).ReturnsAsync((Guid?)null);
            
            // Act
            await _service.ProcessSourdoughReadingAsync(analyzerId, reading);
            
            // Assert
            _userNotifierMock.Verify(x => x.NotifySourdoughReadingAsync(It.IsAny<Guid>(), It.IsAny<SourdoughReading>()), Times.Never);
        }

        [Fact]
        public async Task ProcessSourdoughReadingAsync_MultipleCalls_HandlesCorrectly()
        {
            // Arrange
            var analyzerId1 = Guid.NewGuid();
            var analyzerId2 = Guid.NewGuid();
            var ownerId1 = Guid.NewGuid();
            var ownerId2 = Guid.NewGuid();
            
            var reading1 = new SourdoughReading(
                Rise: 1.5,
                Temperature: 20.0,
                Humidity: 60.0,
                EpochTime: 1234567890,
                Timestamp: DateTime.UtcNow,
                LocalTime: DateTime.UtcNow
            );
            
            var reading2 = new SourdoughReading(
                Rise: 2.0,
                Temperature: 22.0,
                Humidity: 65.0,
                EpochTime: 1234567900,
                Timestamp: DateTime.UtcNow.AddMinutes(1),
                LocalTime: DateTime.UtcNow.AddMinutes(1)
            );
            
            _analyzerRepositoryMock.Setup(x => x.GetOwnersUserIdAsync(analyzerId1)).ReturnsAsync(ownerId1);
            _analyzerRepositoryMock.Setup(x => x.GetOwnersUserIdAsync(analyzerId2)).ReturnsAsync(ownerId2);
            
            // Act
            await _service.ProcessSourdoughReadingAsync(analyzerId1, reading1);
            await _service.ProcessSourdoughReadingAsync(analyzerId2, reading2);
            
            // Assert
            _userNotifierMock.Verify(x => x.NotifySourdoughReadingAsync(ownerId1, reading1), Times.Once);
            _userNotifierMock.Verify(x => x.NotifySourdoughReadingAsync(ownerId2, reading2), Times.Once);
        }

        [Fact]
        public async Task ProcessSourdoughReadingAsync_WithDifferentReadings_NotifiesCorrectly()
        {
            // Arrange
            var analyzerId = Guid.NewGuid();
            var ownerId = Guid.NewGuid();
            
            var reading1 = new SourdoughReading(
                Rise: 1.0,
                Temperature: 20.0,
                Humidity: 60.0,
                EpochTime: 1234567890,
                Timestamp: DateTime.UtcNow,
                LocalTime: DateTime.UtcNow
            );
            
            var reading2 = new SourdoughReading(
                Rise: 3.0,
                Temperature: 25.0,
                Humidity: 70.0,
                EpochTime: 1234567950,
                Timestamp: DateTime.UtcNow.AddMinutes(5),
                LocalTime: DateTime.UtcNow.AddMinutes(5)
            );
            
            _analyzerRepositoryMock.Setup(x => x.GetOwnersUserIdAsync(analyzerId)).ReturnsAsync(ownerId);
            
            // Act
            await _service.ProcessSourdoughReadingAsync(analyzerId, reading1);
            await _service.ProcessSourdoughReadingAsync(analyzerId, reading2);
            
            // Assert
            _userNotifierMock.Verify(x => x.NotifySourdoughReadingAsync(ownerId, reading1), Times.Once);
            _userNotifierMock.Verify(x => x.NotifySourdoughReadingAsync(ownerId, reading2), Times.Once);
        }

        [Theory]
        [InlineData(0.0, 0.0, 0.0)]
        [InlineData(5.0, 50.0, 100.0)]
        [InlineData(-1.0, -10.0, 25.0)]
        public async Task ProcessSourdoughReadingAsync_WithVariousReadingValues_NotifiesCorrectly(
            double rise, double temperature, double humidity)
        {
            // Arrange
            var analyzerId = Guid.NewGuid();
            var ownerId = Guid.NewGuid();
            var timestamp = DateTime.UtcNow;
            var reading = new SourdoughReading(
                Rise: rise,
                Temperature: temperature,
                Humidity: humidity,
                EpochTime: 1234567890,
                Timestamp: timestamp,
                LocalTime: timestamp
            );
            
            _analyzerRepositoryMock.Setup(x => x.GetOwnersUserIdAsync(analyzerId)).ReturnsAsync(ownerId);
            
            // Act
            await _service.ProcessSourdoughReadingAsync(analyzerId, reading);
            
            // Assert
            _userNotifierMock.Verify(x => x.NotifySourdoughReadingAsync(ownerId, It.Is<SourdoughReading>(r => 
                Math.Abs(r.Rise - rise) < 0.0001 &&
                Math.Abs(r.Temperature - temperature) < 0.0001 &&
                Math.Abs(r.Humidity - humidity) < 0.0001
            )), Times.Once);
        }

        [Fact]
        public async Task ProcessSourdoughReadingAsync_CalledConcurrently_HandlesCorrectly()
        {
            // Arrange
            var analyzerId = Guid.NewGuid();
            var ownerId = Guid.NewGuid();
            var readings = new List<SourdoughReading>();
            
            for (int i = 0; i < 10; i++)
            {
                readings.Add(new SourdoughReading(
                    Rise: i * 0.5,
                    Temperature: 20.0 + i,
                    Humidity: 60.0 + i,
                    EpochTime: 1234567890 + i,
                    Timestamp: DateTime.UtcNow.AddSeconds(i),
                    LocalTime: DateTime.UtcNow.AddSeconds(i)
                ));
            }
            
            _analyzerRepositoryMock.Setup(x => x.GetOwnersUserIdAsync(analyzerId)).ReturnsAsync(ownerId);
            
            // Act
            var tasks = readings.Select(reading => _service.ProcessSourdoughReadingAsync(analyzerId, reading));
            await Task.WhenAll(tasks);
            
            // Assert
            foreach (var reading in readings)
            {
                _userNotifierMock.Verify(x => x.NotifySourdoughReadingAsync(ownerId, reading), Times.Once);
            }
        }

        [Fact]
        public async Task ProcessSourdoughReadingAsync_RepositoryReturnsNullOwner_DoesNotThrow()
        {
            // Arrange
            var analyzerId = Guid.NewGuid();
            var reading = new SourdoughReading(
                Rise: 2.5,
                Temperature: 25.5,
                Humidity: 65.3,
                EpochTime: 1234567890,
                Timestamp: DateTime.UtcNow,
                LocalTime: DateTime.UtcNow
            );
            
            _analyzerRepositoryMock.Setup(x => x.GetOwnersUserIdAsync(analyzerId)).ReturnsAsync((Guid?)null);
            
            // Act & Assert 
            await Should.NotThrowAsync(() => _service.ProcessSourdoughReadingAsync(analyzerId, reading));
        }
    }

    public class ProcessDiagnosticsResponseAsync(ITestOutputHelper outputHelper) : SourdoughTelemetryServiceTests(outputHelper)
    {
        [Fact]
        public async Task ProcessDiagnosticsResponseAsync_WithOwner_NotifiesAdmin()
        {
            // Arrange
            var analyzerId = Guid.NewGuid();
            var ownerId = Guid.NewGuid();
            var timestamp = DateTime.UtcNow;
            var diagnostics = new DiagnosticsResponse(
                AnalyzerId: analyzerId.ToString(),
                EpochTime: 1234567890,
                Timestamp: timestamp,
                LocalTime: timestamp,
                Uptime: 3600,
                FreeHeap: 45000,
                State: "monitoring",
                Wifi: new WifiInfo(Connected: true, Rssi: -65),
                Sensors: new SensorInfo(Temperature: 25.5, Humidity: 65.3, Rise: 2.5),
                Humidity: 65.3
            );
            
            _analyzerRepositoryMock.Setup(x => x.GetOwnersUserIdAsync(analyzerId)).ReturnsAsync(ownerId);
            
            // Act
            await _service.ProcessDiagnosticsResponseAsync(analyzerId, diagnostics);
            
            // Assert
            _adminNotifierMock.Verify(x => x.NotifyDiagnosticsResponseAsync(ownerId, diagnostics), Times.Once);
        }

        [Fact]
        public async Task ProcessDiagnosticsResponseAsync_WithNoOwner_DoesNotNotify()
        {
            // Arrange
            var analyzerId = Guid.NewGuid();
            var timestamp = DateTime.UtcNow;
            var diagnostics = new DiagnosticsResponse(
                AnalyzerId: analyzerId.ToString(),
                EpochTime: 1234567890,
                Timestamp: timestamp,
                LocalTime: timestamp,
                Uptime: 3600,
                FreeHeap: 45000,
                State: "monitoring",
                Wifi: new WifiInfo(Connected: true, Rssi: -65),
                Sensors: new SensorInfo(Temperature: 25.5, Humidity: 65.3, Rise: 2.5),
                Humidity: 65.3
            );
            
            _analyzerRepositoryMock.Setup(x => x.GetOwnersUserIdAsync(analyzerId)).ReturnsAsync((Guid?)null);
            
            // Act
            await _service.ProcessDiagnosticsResponseAsync(analyzerId, diagnostics);
            
            // Assert
            _adminNotifierMock.Verify(x => x.NotifyDiagnosticsResponseAsync(It.IsAny<Guid>(), It.IsAny<DiagnosticsResponse>()), Times.Never);
        }
    }

    public class ProcessDiagnosticsRequestAsync(ITestOutputHelper outputHelper) : SourdoughTelemetryServiceTests(outputHelper)
    {
        [Fact]
        public async Task ProcessDiagnosticsRequestAsync_PublishesRequest()
        {
            // Arrange
            var analyzerId = Guid.NewGuid();
            
            // Act
            await _service.ProcessDiagnosticsRequestAsync(analyzerId);
            
            // Assert
            _analyzerPublisherMock.Verify(x => x.RequestDiagnosticsAsync(analyzerId), Times.Once);
        }
    }

    public class SaveSourdoughReadingAsync(ITestOutputHelper outputHelper) : SourdoughTelemetryServiceTests(outputHelper)
    {
        [Fact]
        public async Task SaveSourdoughReadingAsync_WithOwner_SavesReading()
        {
            // Arrange
            var analyzerId = Guid.NewGuid();
            var ownerId = Guid.NewGuid();
            var reading = new SourdoughReading(
                Rise: 2.5,
                Temperature: 25.5,
                Humidity: 65.3,
                EpochTime: 1234567890,
                Timestamp: DateTime.UtcNow,
                LocalTime: DateTime.UtcNow
            );
            
            _analyzerRepositoryMock.Setup(x => x.GetOwnersUserIdAsync(analyzerId)).ReturnsAsync(ownerId);
            
            // Act
            await _service.SaveSourdoughReadingAsync(analyzerId, reading);
            
            // Assert
            _readingRepositoryMock.Verify(x => x.SaveReadingAsync(reading, ownerId, analyzerId), Times.Once);
        }

        [Fact]
        public async Task SaveSourdoughReadingAsync_WithNoOwner_DoesNotSave()
        {
            // Arrange
            var analyzerId = Guid.NewGuid();
            var reading = new SourdoughReading(
                Rise: 2.5,
                Temperature: 25.5,
                Humidity: 65.3,
                EpochTime: 1234567890,
                Timestamp: DateTime.UtcNow,
                LocalTime: DateTime.UtcNow
            );
            
            _analyzerRepositoryMock.Setup(x => x.GetOwnersUserIdAsync(analyzerId)).ReturnsAsync((Guid?)null);
            
            // Act
            await _service.SaveSourdoughReadingAsync(analyzerId, reading);
            
            // Assert
            _readingRepositoryMock.Verify(x => x.SaveReadingAsync(It.IsAny<SourdoughReading>(), It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
        }
    }
}