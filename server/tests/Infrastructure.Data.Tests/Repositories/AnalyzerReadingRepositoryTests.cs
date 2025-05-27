using Core.Entities;
using Core.ValueObjects;
using Infrastructure.Data.Repositories.Sourdough;
using Infrastructure.Data.Tests.Bases;
using Infrastructure.Data.Tests.Database;
using Microsoft.EntityFrameworkCore;
using SharedTestDependencies.Constants;
using SharedTestDependencies.Fakes;
using SharedTestDependencies.Fixtures;
using Shouldly;
using Xunit;

namespace Infrastructure.Data.Tests.Repositories;

[Collection(TestCollections.Database)]
public class AnalyzerReadingRepositoryTests : RepositoryTestBase, IClassFixture<PostgresFixture>
{
    private readonly PgAnalyzerReadingRepository _repository;
    private readonly FakeTimeProvider _timeProvider;

    public AnalyzerReadingRepositoryTests(PostgresFixture fixture) : base(fixture)
    {
        _timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        _repository = new PgAnalyzerReadingRepository(DbContext);
    }

    public class SaveReadingAsync(PostgresFixture fixture) : AnalyzerReadingRepositoryTests(fixture)
    {
        [Fact]
        public async Task SaveReadingAsync_NewReading_SavesSuccessfully()
        {
            // Arrange
            var user = await DbContext.SeedUserAsync(_timeProvider);
            var analyzer = await DbContext.SeedSourdoughAnalyzerAsync(_timeProvider, "AA:BB:CC:DD:EE:FF", "Test Analyzer", isActivated: true);
            
            var reading = new SourdoughReading(
                Rise: 15.5,
                Temperature: 22.3,
                Humidity: 65.7,
                EpochTime: 1640995200,
                Timestamp: new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                LocalTime: new DateTime(2022, 1, 1, 1, 0, 0, DateTimeKind.Unspecified)
            );

            // Act
            await _repository.SaveReadingAsync(reading, user.Id, analyzer.Id);

            // Assert
            var saved = await DbContext.AnalyzerReadings.FirstOrDefaultAsync();
            saved.ShouldNotBeNull();
            saved.AnalyzerId.ShouldBe(analyzer.Id);
            saved.UserId.ShouldBe(user.Id);
            saved.Temperature.ShouldBe(22.3m);
            saved.Humidity.ShouldBe(65.7m);
            saved.Rise.ShouldBe(15.5m);
            saved.EpochTime.ShouldBe(1640995200);
        }

        [Fact]
        public async Task SaveReadingAsync_DuplicateEpochTimeForSameAnalyzer_DoesNotSave()
        {
            // Arrange
            var user = await DbContext.SeedUserAsync(_timeProvider);
            var analyzer = await DbContext.SeedSourdoughAnalyzerAsync(_timeProvider, "AA:BB:CC:DD:EE:FF", "Test Analyzer", isActivated: true);
            
            var existingReading = new AnalyzerReading
            {
                Id = Guid.NewGuid(),
                AnalyzerId = analyzer.Id,
                UserId = user.Id,
                Temperature = 20.0m,
                Humidity = 60.0m,
                Rise = 10.0m,
                EpochTime = 1640995200,
                Timestamp = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                LocalTime = new DateTime(2022, 1, 1, 1, 0, 0, DateTimeKind.Unspecified),
                CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
            };
            DbContext.AnalyzerReadings.Add(existingReading);
            await DbContext.SaveChangesAsync();

            var reading = new SourdoughReading(
                Rise: 15.5,
                Temperature: 22.3,
                Humidity: 65.7,
                EpochTime: 1640995200,
                Timestamp: new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                LocalTime: new DateTime(2022, 1, 1, 1, 0, 0, DateTimeKind.Unspecified)
            );

            // Act
            await _repository.SaveReadingAsync(reading, user.Id, analyzer.Id);

            // Assert
            var count = await DbContext.AnalyzerReadings.CountAsync();
            count.ShouldBe(1);
            var saved = await DbContext.AnalyzerReadings.FirstAsync();
            saved.Temperature.ShouldBe(20.0m);
        }

        [Fact]
        public async Task SaveReadingAsync_SameEpochTimeDifferentAnalyzer_SavesBoth()
        {
            // Arrange
            var user = await DbContext.SeedUserAsync(_timeProvider);
            var analyzer1 = await DbContext.SeedSourdoughAnalyzerAsync(_timeProvider, "AA:BB:CC:DD:EE:FF", "Analyzer 1", isActivated: true);
            var analyzer2 = await DbContext.SeedSourdoughAnalyzerAsync(_timeProvider, "11:22:33:44:55:66", "Analyzer 2", isActivated: true);
            
            var reading = new SourdoughReading(
                Rise: 15.5,
                Temperature: 22.3,
                Humidity: 65.7,
                EpochTime: 1640995200,
                Timestamp: new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                LocalTime: new DateTime(2022, 1, 1, 1, 0, 0, DateTimeKind.Unspecified)
            );

            // Act
            await _repository.SaveReadingAsync(reading, user.Id, analyzer1.Id);
            await _repository.SaveReadingAsync(reading, user.Id, analyzer2.Id);

            // Assert
            var count = await DbContext.AnalyzerReadings.CountAsync();
            count.ShouldBe(2);
        }

        [Fact]
        public async Task SaveReadingAsync_NullableValues_SavesCorrectly()
        {
            // Arrange
            var user = await DbContext.SeedUserAsync(_timeProvider);
            var analyzer = await DbContext.SeedSourdoughAnalyzerAsync(_timeProvider, "AA:BB:CC:DD:EE:FF", "Test Analyzer", isActivated: true);
            
            var reading = new SourdoughReading(
                Rise: 0.0,
                Temperature: 0.0,
                Humidity: 0.0,
                EpochTime: 1640995200,
                Timestamp: new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                LocalTime: new DateTime(2022, 1, 1, 1, 0, 0, DateTimeKind.Unspecified)
            );

            // Act
            await _repository.SaveReadingAsync(reading, user.Id, analyzer.Id);

            // Assert
            var saved = await DbContext.AnalyzerReadings.FirstOrDefaultAsync();
            saved.ShouldNotBeNull();
            saved.Temperature.ShouldBe(0.0m);
            saved.Humidity.ShouldBe(0.0m);
            saved.Rise.ShouldBe(0.0m);
        }
    }

    public class GetReadingsAsync(PostgresFixture fixture) : AnalyzerReadingRepositoryTests(fixture)
    {
        [Fact]
        public async Task GetReadingsAsync_NoReadings_ReturnsEmptyList()
        {
            // Arrange
            var analyzer = await DbContext.SeedSourdoughAnalyzerAsync(_timeProvider, "AA:BB:CC:DD:EE:FF", "Test Analyzer", isActivated: true);

            // Act
            var result = await _repository.GetReadingsAsync(analyzer.Id);

            // Assert
            var analyzerReadings = result.ToList();
            analyzerReadings.ShouldNotBeNull();
            analyzerReadings.ShouldBeEmpty();
        }

        [Fact]
        public async Task GetReadingsAsync_MultipleReadings_ReturnsAll()
        {
            // Arrange
            var user = await DbContext.SeedUserAsync(_timeProvider);
            var analyzer = await DbContext.SeedSourdoughAnalyzerAsync(_timeProvider, "AA:BB:CC:DD:EE:FF", "Test Analyzer", isActivated: true);
            
            var readings = new[]
            {
                new AnalyzerReading
                {
                    Id = Guid.NewGuid(),
                    AnalyzerId = analyzer.Id,
                    UserId = user.Id,
                    Temperature = 20.0m,
                    Humidity = 60.0m,
                    Rise = 10.0m,
                    EpochTime = 1640995200,
                    Timestamp = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    LocalTime = new DateTime(2022, 1, 1, 1, 0, 0, DateTimeKind.Unspecified),
                    CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
                },
                new AnalyzerReading
                {
                    Id = Guid.NewGuid(),
                    AnalyzerId = analyzer.Id,
                    UserId = user.Id,
                    Temperature = 21.0m,
                    Humidity = 61.0m,
                    Rise = 11.0m,
                    EpochTime = 1640998800,
                    Timestamp = new DateTime(2022, 1, 1, 1, 0, 0, DateTimeKind.Utc),
                    LocalTime = new DateTime(2022, 1, 1, 2, 0, 0, DateTimeKind.Unspecified),
                    CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
                }
            };
            DbContext.AnalyzerReadings.AddRange(readings);
            await DbContext.SaveChangesAsync();

            // Act
            var result = (await _repository.GetReadingsAsync(analyzer.Id)).ToList();

            // Assert
            result.Count.ShouldBe(2);
            result.Any(r => r.Temperature == 20.0m).ShouldBeTrue();
            result.Any(r => r.Temperature == 21.0m).ShouldBeTrue();
        }

        [Fact]
        public async Task GetReadingsAsync_WithFromDate_FiltersCorrectly()
        {
            // Arrange
            var user = await DbContext.SeedUserAsync(_timeProvider);
            var analyzer = await DbContext.SeedSourdoughAnalyzerAsync(_timeProvider, "AA:BB:CC:DD:EE:FF", "Test Analyzer", isActivated: true);
            
            var readings = new[]
            {
                new AnalyzerReading
                {
                    Id = Guid.NewGuid(),
                    AnalyzerId = analyzer.Id,
                    UserId = user.Id,
                    Temperature = 20.0m,
                    EpochTime = 1640995200,
                    Timestamp = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    LocalTime = new DateTime(2022, 1, 1, 1, 0, 0, DateTimeKind.Unspecified),
                    CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
                },
                new AnalyzerReading
                {
                    Id = Guid.NewGuid(),
                    AnalyzerId = analyzer.Id,
                    UserId = user.Id,
                    Temperature = 21.0m,
                    EpochTime = 1641081600,
                    Timestamp = new DateTime(2022, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                    LocalTime = new DateTime(2022, 1, 2, 1, 0, 0, DateTimeKind.Unspecified),
                    CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
                },
                new AnalyzerReading
                {
                    Id = Guid.NewGuid(),
                    AnalyzerId = analyzer.Id,
                    UserId = user.Id,
                    Temperature = 22.0m,
                    EpochTime = 1641168000,
                    Timestamp = new DateTime(2022, 1, 3, 0, 0, 0, DateTimeKind.Utc),
                    LocalTime = new DateTime(2022, 1, 3, 1, 0, 0, DateTimeKind.Unspecified),
                    CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
                }
            };
            DbContext.AnalyzerReadings.AddRange(readings);
            await DbContext.SaveChangesAsync();

            // Act
            var fromDate = new DateTime(2022, 1, 2, 0, 0, 0, DateTimeKind.Unspecified);
            var result = (await _repository.GetReadingsAsync(analyzer.Id, from: fromDate)).ToList();

            // Assert
            result.Count.ShouldBe(2);
            result.All(r => r.LocalTime >= fromDate).ShouldBeTrue();
        }

        [Fact]
        public async Task GetReadingsAsync_WithToDate_FiltersCorrectly()
        {
            // Arrange
            var user = await DbContext.SeedUserAsync(_timeProvider);
            var analyzer = await DbContext.SeedSourdoughAnalyzerAsync(_timeProvider, "AA:BB:CC:DD:EE:FF", "Test Analyzer", isActivated: true);
            
            var readings = new[]
            {
                new AnalyzerReading
                {
                    Id = Guid.NewGuid(),
                    AnalyzerId = analyzer.Id,
                    UserId = user.Id,
                    Temperature = 20.0m,
                    EpochTime = 1640995200,
                    Timestamp = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    LocalTime = new DateTime(2022, 1, 1, 1, 0, 0, DateTimeKind.Unspecified),
                    CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
                },
                new AnalyzerReading
                {
                    Id = Guid.NewGuid(),
                    AnalyzerId = analyzer.Id,
                    UserId = user.Id,
                    Temperature = 21.0m,
                    EpochTime = 1641081600,
                    Timestamp = new DateTime(2022, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                    LocalTime = new DateTime(2022, 1, 2, 1, 0, 0, DateTimeKind.Unspecified),
                    CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
                },
                new AnalyzerReading
                {
                    Id = Guid.NewGuid(),
                    AnalyzerId = analyzer.Id,
                    UserId = user.Id,
                    Temperature = 22.0m,
                    EpochTime = 1641168000,
                    Timestamp = new DateTime(2022, 1, 3, 0, 0, 0, DateTimeKind.Utc),
                    LocalTime = new DateTime(2022, 1, 3, 1, 0, 0, DateTimeKind.Unspecified),
                    CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
                }
            };
            DbContext.AnalyzerReadings.AddRange(readings);
            await DbContext.SaveChangesAsync();

            // Act
            var toDate = new DateTime(2022, 1, 2, 23, 59, 59, DateTimeKind.Unspecified);
            var result = (await _repository.GetReadingsAsync(analyzer.Id, toDate: toDate)).ToList();

            // Assert
            result.Count.ShouldBe(2);
            result.All(r => r.LocalTime <= toDate).ShouldBeTrue();
        }

        [Fact]
        public async Task GetReadingsAsync_WithDateRange_FiltersCorrectly()
        {
            // Arrange
            var user = await DbContext.SeedUserAsync(_timeProvider);
            var analyzer = await DbContext.SeedSourdoughAnalyzerAsync(_timeProvider, "AA:BB:CC:DD:EE:FF", "Test Analyzer", isActivated: true);
            
            var readings = new[]
            {
                new AnalyzerReading
                {
                    Id = Guid.NewGuid(),
                    AnalyzerId = analyzer.Id,
                    UserId = user.Id,
                    Temperature = 19.0m,
                    EpochTime = 1640908800,
                    Timestamp = new DateTime(2021, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                    LocalTime = new DateTime(2021, 12, 31, 1, 0, 0, DateTimeKind.Unspecified),
                    CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
                },
                new AnalyzerReading
                {
                    Id = Guid.NewGuid(),
                    AnalyzerId = analyzer.Id,
                    UserId = user.Id,
                    Temperature = 20.0m,
                    EpochTime = 1640995200,
                    Timestamp = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    LocalTime = new DateTime(2022, 1, 1, 1, 0, 0, DateTimeKind.Unspecified),
                    CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
                },
                new AnalyzerReading
                {
                    Id = Guid.NewGuid(),
                    AnalyzerId = analyzer.Id,
                    UserId = user.Id,
                    Temperature = 21.0m,
                    EpochTime = 1641081600,
                    Timestamp = new DateTime(2022, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                    LocalTime = new DateTime(2022, 1, 2, 1, 0, 0, DateTimeKind.Unspecified),
                    CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
                },
                new AnalyzerReading
                {
                    Id = Guid.NewGuid(),
                    AnalyzerId = analyzer.Id,
                    UserId = user.Id,
                    Temperature = 22.0m,
                    EpochTime = 1641168000,
                    Timestamp = new DateTime(2022, 1, 3, 0, 0, 0, DateTimeKind.Utc),
                    LocalTime = new DateTime(2022, 1, 3, 1, 0, 0, DateTimeKind.Unspecified),
                    CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
                }
            };
            DbContext.AnalyzerReadings.AddRange(readings);
            await DbContext.SaveChangesAsync();

            // Act
            var fromDate = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
            var toDate = new DateTime(2022, 1, 2, 23, 59, 59, DateTimeKind.Unspecified);
            var result = (await _repository.GetReadingsAsync(analyzer.Id, from: fromDate, toDate: toDate)).ToList();

            // Assert
            result.Count.ShouldBe(2);
            result.All(r => r.LocalTime >= fromDate && r.LocalTime <= toDate).ShouldBeTrue();
        }

        [Fact]
        public async Task GetReadingsAsync_DifferentAnalyzer_ReturnsOnlyForSpecifiedAnalyzer()
        {
            // Arrange
            var user = await DbContext.SeedUserAsync(_timeProvider);
            var analyzer1 = await DbContext.SeedSourdoughAnalyzerAsync(_timeProvider, "AA:BB:CC:DD:EE:FF", "Analyzer 1", isActivated: true);
            var analyzer2 = await DbContext.SeedSourdoughAnalyzerAsync(_timeProvider, "11:22:33:44:55:66", "Analyzer 2", isActivated: true);
            
            var readings = new[]
            {
                new AnalyzerReading
                {
                    Id = Guid.NewGuid(),
                    AnalyzerId = analyzer1.Id,
                    UserId = user.Id,
                    Temperature = 20.0m,
                    EpochTime = 1640995200,
                    Timestamp = _timeProvider.GetUtcNow().UtcDateTime,
                    LocalTime = new DateTime(_timeProvider.GetUtcNow().UtcDateTime.Ticks, DateTimeKind.Unspecified),
                    CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
                },
                new AnalyzerReading
                {
                    Id = Guid.NewGuid(),
                    AnalyzerId = analyzer2.Id,
                    UserId = user.Id,
                    Temperature = 21.0m,
                    EpochTime = 1640995201,
                    Timestamp = _timeProvider.GetUtcNow().UtcDateTime,
                    LocalTime = new DateTime(_timeProvider.GetUtcNow().UtcDateTime.Ticks, DateTimeKind.Unspecified),
                    CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
                }
            };
            DbContext.AnalyzerReadings.AddRange(readings);
            await DbContext.SaveChangesAsync();

            // Act
            var result = (await _repository.GetReadingsAsync(analyzer1.Id)).ToList();

            // Assert
            result.Count.ShouldBe(1);
            result[0].AnalyzerId.ShouldBe(analyzer1.Id);
        }
    }

    public class GetLatestReadingAsync(PostgresFixture fixture) : AnalyzerReadingRepositoryTests(fixture)
    {
        [Fact]
        public async Task GetLatestReadingAsync_NoReadings_ReturnsNull()
        {
            // Arrange
            var analyzer = await DbContext.SeedSourdoughAnalyzerAsync(_timeProvider, "AA:BB:CC:DD:EE:FF", "Test Analyzer", isActivated: true);

            // Act
            var result = await _repository.GetLatestReadingAsync(analyzer.Id);

            // Assert
            result.ShouldBeNull();
        }

        [Fact]
        public async Task GetLatestReadingAsync_MultipleReadings_ReturnsLatestByTimestamp()
        {
            // Arrange
            var user = await DbContext.SeedUserAsync(_timeProvider);
            var analyzer = await DbContext.SeedSourdoughAnalyzerAsync(_timeProvider, "AA:BB:CC:DD:EE:FF", "Test Analyzer", isActivated: true);
            
            var readings = new[]
            {
                new AnalyzerReading
                {
                    Id = Guid.NewGuid(),
                    AnalyzerId = analyzer.Id,
                    UserId = user.Id,
                    Temperature = 20.0m,
                    EpochTime = 1640995200,
                    Timestamp = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    LocalTime = new DateTime(2022, 1, 1, 1, 0, 0, DateTimeKind.Unspecified),
                    CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
                },
                new AnalyzerReading
                {
                    Id = Guid.NewGuid(),
                    AnalyzerId = analyzer.Id,
                    UserId = user.Id,
                    Temperature = 22.0m,
                    EpochTime = 1641168000,
                    Timestamp = new DateTime(2022, 1, 3, 0, 0, 0, DateTimeKind.Utc),
                    LocalTime = new DateTime(2022, 1, 3, 1, 0, 0, DateTimeKind.Unspecified),
                    CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
                },
                new AnalyzerReading
                {
                    Id = Guid.NewGuid(),
                    AnalyzerId = analyzer.Id,
                    UserId = user.Id,
                    Temperature = 21.0m,
                    EpochTime = 1641081600,
                    Timestamp = new DateTime(2022, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                    LocalTime = new DateTime(2022, 1, 2, 1, 0, 0, DateTimeKind.Unspecified),
                    CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
                }
            };
            DbContext.AnalyzerReadings.AddRange(readings);
            await DbContext.SaveChangesAsync();

            // Act
            var result = await _repository.GetLatestReadingAsync(analyzer.Id);

            // Assert
            result.ShouldNotBeNull();
            result.Temperature.ShouldBe(22.0m);
            result.Timestamp.ShouldBe(new DateTime(2022, 1, 3, 0, 0, 0, DateTimeKind.Utc));
        }

        [Fact]
        public async Task GetLatestReadingAsync_DifferentAnalyzer_ReturnsNullForOtherAnalyzer()
        {
            // Arrange
            var user = await DbContext.SeedUserAsync(_timeProvider);
            var analyzer1 = await DbContext.SeedSourdoughAnalyzerAsync(_timeProvider, "AA:BB:CC:DD:EE:FF", "Analyzer 1", isActivated: true);
            var analyzer2 = await DbContext.SeedSourdoughAnalyzerAsync(_timeProvider, "11:22:33:44:55:66", "Analyzer 2", isActivated: true);
            
            var reading = new AnalyzerReading
            {
                Id = Guid.NewGuid(),
                AnalyzerId = analyzer1.Id,
                UserId = user.Id,
                Temperature = 20.0m,
                EpochTime = 1640995200,
                Timestamp = _timeProvider.GetUtcNow().UtcDateTime,
                LocalTime = new DateTime(_timeProvider.GetUtcNow().UtcDateTime.Ticks, DateTimeKind.Unspecified),
                CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
            };
            DbContext.AnalyzerReadings.Add(reading);
            await DbContext.SaveChangesAsync();

            // Act
            var result = await _repository.GetLatestReadingAsync(analyzer2.Id);

            // Assert
            result.ShouldBeNull();
        }
    }

    public class GetLatestReadingsForCachingAsync(PostgresFixture fixture) : AnalyzerReadingRepositoryTests(fixture)
    {
        [Fact]
        public async Task GetLatestReadingsForCachingAsync_NoReadings_ReturnsEmptyList()
        {
            // Arrange
            var analyzer = await DbContext.SeedSourdoughAnalyzerAsync(_timeProvider, "AA:BB:CC:DD:EE:FF", "Test Analyzer", isActivated: true);

            // Act
            var result = await _repository.GetLatestReadingsForCachingAsync(analyzer.Id);

            // Assert
            var analyzerReadings = result.ToList();
            analyzerReadings.ShouldNotBeNull();
            analyzerReadings.ShouldBeEmpty();
        }

        [Fact]
        public async Task GetLatestReadingsForCachingAsync_LessThanMaxResults_ReturnsAll()
        {
            // Arrange
            var user = await DbContext.SeedUserAsync(_timeProvider);
            var analyzer = await DbContext.SeedSourdoughAnalyzerAsync(_timeProvider, "AA:BB:CC:DD:EE:FF", "Test Analyzer", isActivated: true);
            
            var readings = Enumerable.Range(1, 10).Select(i => new AnalyzerReading
            {
                Id = Guid.NewGuid(),
                AnalyzerId = analyzer.Id,
                UserId = user.Id,
                Temperature = 20.0m + i,
                EpochTime = 1640995200 + i * 3600,
                Timestamp = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddHours(i),
                LocalTime = new DateTime(2022, 1, 1, 1, 0, 0, DateTimeKind.Unspecified).AddHours(i),
                CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
            }).ToList();
            
            DbContext.AnalyzerReadings.AddRange(readings);
            await DbContext.SaveChangesAsync();

            // Act
            var result = (await _repository.GetLatestReadingsForCachingAsync(analyzer.Id, maxResults: 20)).ToList();

            // Assert
            result.Count.ShouldBe(10);
        }

        [Fact]
        public async Task GetLatestReadingsForCachingAsync_MoreThanMaxResults_ReturnsMaxResults()
        {
            // Arrange
            var user = await DbContext.SeedUserAsync(_timeProvider);
            var analyzer = await DbContext.SeedSourdoughAnalyzerAsync(_timeProvider, "AA:BB:CC:DD:EE:FF", "Test Analyzer", isActivated: true);
            
            var readings = Enumerable.Range(1, 20).Select(i => new AnalyzerReading
            {
                Id = Guid.NewGuid(),
                AnalyzerId = analyzer.Id,
                UserId = user.Id,
                Temperature = 20.0m + i,
                EpochTime = 1640995200 + i * 3600,
                Timestamp = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddHours(i),
                LocalTime = new DateTime(2022, 1, 1, 1, 0, 0, DateTimeKind.Unspecified).AddHours(i),
                CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
            }).ToList();
            
            DbContext.AnalyzerReadings.AddRange(readings);
            await DbContext.SaveChangesAsync();

            // Act
            var result = (await _repository.GetLatestReadingsForCachingAsync(analyzer.Id, maxResults: 10)).ToList();

            // Assert
            result.Count.ShouldBe(10);
        }

        [Fact]
        public async Task GetLatestReadingsForCachingAsync_OrdersByTimestampDescending()
        {
            // Arrange
            var user = await DbContext.SeedUserAsync(_timeProvider);
            var analyzer = await DbContext.SeedSourdoughAnalyzerAsync(_timeProvider, "AA:BB:CC:DD:EE:FF", "Test Analyzer", isActivated: true);
            
            var readings = new[]
            {
                new AnalyzerReading
                {
                    Id = Guid.NewGuid(),
                    AnalyzerId = analyzer.Id,
                    UserId = user.Id,
                    Temperature = 20.0m,
                    EpochTime = 1640995200,
                    Timestamp = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    LocalTime = new DateTime(2022, 1, 1, 1, 0, 0, DateTimeKind.Unspecified),
                    CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
                },
                new AnalyzerReading
                {
                    Id = Guid.NewGuid(),
                    AnalyzerId = analyzer.Id,
                    UserId = user.Id,
                    Temperature = 22.0m,
                    EpochTime = 1641168000,
                    Timestamp = new DateTime(2022, 1, 3, 0, 0, 0, DateTimeKind.Utc),
                    LocalTime = new DateTime(2022, 1, 3, 1, 0, 0, DateTimeKind.Unspecified),
                    CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
                },
                new AnalyzerReading
                {
                    Id = Guid.NewGuid(),
                    AnalyzerId = analyzer.Id,
                    UserId = user.Id,
                    Temperature = 21.0m,
                    EpochTime = 1641081600,
                    Timestamp = new DateTime(2022, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                    LocalTime = new DateTime(2022, 1, 2, 1, 0, 0, DateTimeKind.Unspecified),
                    CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
                }
            };
            DbContext.AnalyzerReadings.AddRange(readings);
            await DbContext.SaveChangesAsync();

            // Act
            var result = (await _repository.GetLatestReadingsForCachingAsync(analyzer.Id)).ToList();

            // Assert
            result.Count.ShouldBe(3);
            result[0].Temperature.ShouldBe(22.0m);
            result[1].Temperature.ShouldBe(21.0m);
            result[2].Temperature.ShouldBe(20.0m);
        }

        [Fact]
        public async Task GetLatestReadingsForCachingAsync_DefaultMaxResultsIs500()
        {
            // Arrange
            var user = await DbContext.SeedUserAsync(_timeProvider);
            var analyzer = await DbContext.SeedSourdoughAnalyzerAsync(_timeProvider, "AA:BB:CC:DD:EE:FF", "Test Analyzer", isActivated: true);
            
            var readings = Enumerable.Range(1, 600).Select(i => new AnalyzerReading
            {
                Id = Guid.NewGuid(),
                AnalyzerId = analyzer.Id,
                UserId = user.Id,
                Temperature = 20.0m,
                EpochTime = 1640995200 + i,
                Timestamp = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(i),
                LocalTime = new DateTime(2022, 1, 1, 1, 0, 0, DateTimeKind.Unspecified).AddSeconds(i),
                CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
            }).ToList();
            
            DbContext.AnalyzerReadings.AddRange(readings);
            await DbContext.SaveChangesAsync();

            // Act
            var result = (await _repository.GetLatestReadingsForCachingAsync(analyzer.Id)).ToList();

            // Assert
            result.Count.ShouldBe(500);
        }
    }
}