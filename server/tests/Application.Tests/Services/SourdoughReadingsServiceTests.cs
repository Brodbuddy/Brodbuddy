using Application.Interfaces.Data.Repositories.Sourdough;
using Application.Services.Sourdough;
using Core.Entities;
using Moq;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Application.Tests.Services;

public class SourdoughReadingsServiceTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly Mock<IAnalyzerReadingRepository> _repositoryMock;
    private readonly SourdoughReadingsService _service;

    private SourdoughReadingsServiceTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _repositoryMock = new Mock<IAnalyzerReadingRepository>();
        _service = new SourdoughReadingsService(_repositoryMock.Object);
    }

    public class GetLatestReadingAsync(ITestOutputHelper outputHelper) : SourdoughReadingsServiceTests(outputHelper)
    {
        [Fact]
        public async Task GetLatestReadingAsync_WithExistingReading_ReturnsReading()
        {
            // Arrange
            var analyzerId = Guid.NewGuid();
            var expectedReading = new AnalyzerReading
            {
                Id = Guid.NewGuid(),
                AnalyzerId = analyzerId,
                Temperature = 25.5m,
                Humidity = 65.3m,
                Rise = 2.5m,
                Timestamp = DateTime.UtcNow
            };
            
            _repositoryMock.Setup(x => x.GetLatestReadingAsync(analyzerId))
                .ReturnsAsync(expectedReading);
            
            // Act
            var result = await _service.GetLatestReadingAsync(analyzerId);
            
            // Assert
            result.ShouldNotBeNull();
            result.ShouldBe(expectedReading);
        }

        [Fact]
        public async Task GetLatestReadingAsync_NoReadings_ReturnsNull()
        {
            // Arrange
            var analyzerId = Guid.NewGuid();
            
            _repositoryMock.Setup(x => x.GetLatestReadingAsync(analyzerId))
                .ReturnsAsync((AnalyzerReading?)null);
            
            // Act
            var result = await _service.GetLatestReadingAsync(analyzerId);
            
            // Assert
            result.ShouldBeNull();
        }
    }

    public class GetLatestReadingsForCachingAsync(ITestOutputHelper outputHelper) : SourdoughReadingsServiceTests(outputHelper)
    {
        [Fact]
        public async Task GetLatestReadingsForCachingAsync_ReturnsRequestedNumberOfReadings()
        {
            // Arrange
            var analyzerId = Guid.NewGuid();
            var maxResults = 10;
            var readings = new List<AnalyzerReading>();
            
            for (int i = 0; i < 10; i++)
            {
                readings.Add(new AnalyzerReading
                {
                    Id = Guid.NewGuid(),
                    AnalyzerId = analyzerId,
                    Temperature = 20.0m + i,
                    Humidity = 60.0m + i,
                    Rise = 1.0m + (i * 0.1m),
                    Timestamp = DateTime.UtcNow.AddMinutes(-i)
                });
            }
            
            _repositoryMock.Setup(x => x.GetLatestReadingsForCachingAsync(analyzerId, maxResults))
                .ReturnsAsync(readings);
            
            // Act
            var result = await _service.GetLatestReadingsForCachingAsync(analyzerId, maxResults);
            
            // Assert
            var analyzerReadings = result.ToList();
            analyzerReadings.ShouldNotBeNull();
            analyzerReadings.Count.ShouldBe(10);
        }

        [Fact]
        public async Task GetLatestReadingsForCachingAsync_NoReadings_ReturnsEmptyList()
        {
            // Arrange
            var analyzerId = Guid.NewGuid();
            var maxResults = 20;
            
            _repositoryMock.Setup(x => x.GetLatestReadingsForCachingAsync(analyzerId, maxResults))
                .ReturnsAsync(new List<AnalyzerReading>());
            
            // Act
            var result = await _service.GetLatestReadingsForCachingAsync(analyzerId, maxResults);
            
            // Assert
            var analyzerReadings = result.ToList();
            analyzerReadings.ShouldNotBeNull();
            analyzerReadings.ShouldBeEmpty();
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(50)]
        [InlineData(100)]
        public async Task GetLatestReadingsForCachingAsync_PassesMaxResultsCorrectly(int maxResults)
        {
            // Arrange
            var analyzerId = Guid.NewGuid();
            
            _repositoryMock.Setup(x => x.GetLatestReadingsForCachingAsync(analyzerId, maxResults))
                .ReturnsAsync(new List<AnalyzerReading>());
            
            // Act
            await _service.GetLatestReadingsForCachingAsync(analyzerId, maxResults);
            
            // Assert
            _repositoryMock.Verify(x => x.GetLatestReadingsForCachingAsync(analyzerId, maxResults), Times.Once);
        }
    }

    public class GetReadingsByTimeRangeAsync(ITestOutputHelper outputHelper) : SourdoughReadingsServiceTests(outputHelper)
    {
        [Fact]
        public async Task GetReadingsByTimeRangeAsync_WithDateRange_ReturnsReadings()
        {
            // Arrange
            var analyzerId = Guid.NewGuid();
            var fromDate = DateTime.UtcNow.AddDays(-7);
            var toDate = DateTime.UtcNow;
            
            var readings = new List<AnalyzerReading>
            {
                new() { Id = Guid.NewGuid(), AnalyzerId = analyzerId, Timestamp = fromDate.AddDays(1) },
                new() { Id = Guid.NewGuid(), AnalyzerId = analyzerId, Timestamp = fromDate.AddDays(3) },
                new() { Id = Guid.NewGuid(), AnalyzerId = analyzerId, Timestamp = fromDate.AddDays(5) }
            };
            
            _repositoryMock.Setup(x => x.GetReadingsAsync(analyzerId, fromDate, toDate))
                .ReturnsAsync(readings);
            
            // Act
            var result = await _service.GetReadingsByTimeRangeAsync(analyzerId, fromDate, toDate);
            
            // Assert
            var analyzerReadings = result.ToList();
            analyzerReadings.ShouldNotBeNull();
            analyzerReadings.Count.ShouldBe(3);
        }

        [Fact]
        public async Task GetReadingsByTimeRangeAsync_WithNullFromDate_PassesNullToRepository()
        {
            // Arrange
            var analyzerId = Guid.NewGuid();
            DateTime? fromDate = null;
            var toDate = DateTime.UtcNow;
            
            _repositoryMock.Setup(x => x.GetReadingsAsync(analyzerId, fromDate, toDate))
                .ReturnsAsync(new List<AnalyzerReading>());
            
            // Act
            await _service.GetReadingsByTimeRangeAsync(analyzerId, fromDate, toDate);
            
            // Assert
            _repositoryMock.Verify(x => x.GetReadingsAsync(analyzerId, null, toDate), Times.Once);
        }

        [Fact]
        public async Task GetReadingsByTimeRangeAsync_WithNullToDate_PassesNullToRepository()
        {
            // Arrange
            var analyzerId = Guid.NewGuid();
            var fromDate = DateTime.UtcNow.AddDays(-30);
            DateTime? toDate = null;
            
            _repositoryMock.Setup(x => x.GetReadingsAsync(analyzerId, fromDate, toDate))
                .ReturnsAsync(new List<AnalyzerReading>());
            
            // Act
            await _service.GetReadingsByTimeRangeAsync(analyzerId, fromDate, toDate);
            
            // Assert
            _repositoryMock.Verify(x => x.GetReadingsAsync(analyzerId, fromDate, null), Times.Once);
        }

        [Fact]
        public async Task GetReadingsByTimeRangeAsync_WithBothNullDates_PassesBothNullsToRepository()
        {
            // Arrange
            var analyzerId = Guid.NewGuid();
            DateTime? fromDate = null;
            DateTime? toDate = null;
            
            _repositoryMock.Setup(x => x.GetReadingsAsync(analyzerId, fromDate, toDate))
                .ReturnsAsync(new List<AnalyzerReading>());
            
            // Act
            await _service.GetReadingsByTimeRangeAsync(analyzerId, fromDate, toDate);
            
            // Assert
            _repositoryMock.Verify(x => x.GetReadingsAsync(analyzerId, null, null), Times.Once);
        }

        [Fact]
        public async Task GetReadingsByTimeRangeAsync_NoReadingsInRange_ReturnsEmptyList()
        {
            // Arrange
            var analyzerId = Guid.NewGuid();
            var fromDate = DateTime.UtcNow.AddHours(-1);
            var toDate = DateTime.UtcNow;
            
            _repositoryMock.Setup(x => x.GetReadingsAsync(analyzerId, fromDate, toDate))
                .ReturnsAsync(new List<AnalyzerReading>());
            
            // Act
            var result = await _service.GetReadingsByTimeRangeAsync(analyzerId, fromDate, toDate);
            
            // Assert
            var analyzerReadings = result.ToList();
            analyzerReadings.ShouldNotBeNull();
            analyzerReadings.ShouldBeEmpty();
        }
    }
}