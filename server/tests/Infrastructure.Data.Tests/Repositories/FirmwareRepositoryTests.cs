using Core.Entities;
using Infrastructure.Data.Repositories;
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
public class FirmwareRepositoryTests : RepositoryTestBase
{
    private readonly PgFirmwareRepository _repository;
    private readonly FakeTimeProvider _timeProvider;

    public FirmwareRepositoryTests(PostgresFixture fixture) : base(fixture)
    {
        _timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        _repository = new PgFirmwareRepository(DbContext, _timeProvider);
    }

    public class GetByIdAsync(PostgresFixture fixture) : FirmwareRepositoryTests(fixture)
    {
        [Fact]
        public async Task GetByIdAsync_ExistingFirmware_ReturnsFirmware()
        {
            // Arrange
            var firmware = new FirmwareVersion
            {
                Id = Guid.NewGuid(),
                Version = "1.0.0",
                Description = "Initial release",
                FileSize = 1024,
                Crc32 = 123456,
                IsStable = true,
                CreatedAt = DateTime.UtcNow
            };
            DbContext.FirmwareVersions.Add(firmware);
            await DbContext.SaveChangesAsync();

            // Act
            var result = await _repository.GetByIdAsync(firmware.Id);

            // Assert
            result.ShouldNotBeNull();
            result.Id.ShouldBe(firmware.Id);
            result.Version.ShouldBe("1.0.0");
        }

        [Fact]
        public async Task GetByIdAsync_NonExistentId_ReturnsNull()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var result = await _repository.GetByIdAsync(nonExistentId);

            // Assert
            result.ShouldBeNull();
        }
    }

    public class GetAllAsync(PostgresFixture fixture) : FirmwareRepositoryTests(fixture)
    {
        [Fact]
        public async Task GetAllAsync_NoFirmware_ReturnsEmptyList()
        {
            // Arrange

            // Act
            var result = await _repository.GetAllAsync();

            // Assert
            var firmwareVersions = result.ToList();
            firmwareVersions.ShouldNotBeNull();
            firmwareVersions.ShouldBeEmpty();
        }

        [Fact]
        public async Task GetAllAsync_MultipleFirmware_ReturnsAllOrderedByCreatedAtDescending()
        {
            // Arrange
            var firmware1 = new FirmwareVersion
            {
                Id = Guid.NewGuid(),
                Version = "1.0.0",
                Description = "First",
                FileSize = 1024,
                Crc32 = 123,
                IsStable = true,
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            };
            var firmware2 = new FirmwareVersion
            {
                Id = Guid.NewGuid(),
                Version = "2.0.0",
                Description = "Second",
                FileSize = 2048,
                Crc32 = 456,
                IsStable = true,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            };
            var firmware3 = new FirmwareVersion
            {
                Id = Guid.NewGuid(),
                Version = "3.0.0",
                Description = "Third",
                FileSize = 3072,
                Crc32 = 789,
                IsStable = false,
                CreatedAt = DateTime.UtcNow
            };
            DbContext.FirmwareVersions.AddRange(firmware1, firmware2, firmware3);
            await DbContext.SaveChangesAsync();

            // Act
            var result = (await _repository.GetAllAsync()).ToList();

            // Assert
            result.Count.ShouldBe(3);
            result[0].Version.ShouldBe("3.0.0");
            result[1].Version.ShouldBe("2.0.0");
            result[2].Version.ShouldBe("1.0.0");
        }
    }

    public class CreateAsync(PostgresFixture fixture) : FirmwareRepositoryTests(fixture)
    {
        [Fact]
        public async Task CreateAsync_ValidFirmware_SavesAndReturnsWithId()
        {
            // Arrange
            var firmware = new FirmwareVersion
            {
                Version = "1.2.3",
                Description = "New firmware",
                FileSize = 1024 * 1024,
                Crc32 = 0xABCDEF,
                FileUrl = "https://example.com/firmware.bin",
                ReleaseNotes = "Bug fixes",
                IsStable = true
            };
            var testTime = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);
            _timeProvider.SetUtcNow(testTime);

            // Act
            var result = await _repository.CreateAsync(firmware);

            // Assert
            result.Id.ShouldNotBe(Guid.Empty);
            result.CreatedAt.ShouldBe(testTime);
            
            var dbEntity = await DbContext.FirmwareVersions.FindAsync(result.Id);
            dbEntity.ShouldNotBeNull();
            dbEntity.Version.ShouldBe("1.2.3");
            dbEntity.FileSize.ShouldBe(1024 * 1024);
        }

        [Fact]
        public async Task CreateAsync_WithCreatedBy_SavesWithUserReference()
        {
            // Arrange
            var user = await DbContext.SeedUserAsync(_timeProvider);

            var firmware = new FirmwareVersion
            {
                Version = "1.0.0",
                Description = "Test",
                FileSize = 1024,
                Crc32 = 123,
                IsStable = true,
                CreatedBy = user.Id
            };

            // Act
            var result = await _repository.CreateAsync(firmware);

            // Assert
            result.CreatedBy.ShouldBe(user.Id);
            
            var dbEntity = await DbContext.FirmwareVersions
                .Include(f => f.CreatedByNavigation)
                .FirstAsync(f => f.Id == result.Id);
            dbEntity.CreatedByNavigation.ShouldNotBeNull();
            dbEntity.CreatedByNavigation.Email.ShouldBe("peter@test.dk");
        }
    }

    public class GetLatestStableAsync(PostgresFixture fixture) : FirmwareRepositoryTests(fixture)
    {
        [Fact]
        public async Task GetLatestStableAsync_NoStableFirmware_ReturnsNull()
        {
            // Arrange
            var unstableFirmware = new FirmwareVersion
            {
                Id = Guid.NewGuid(),
                Version = "1.0.0-beta",
                Description = "Beta",
                FileSize = 1024,
                Crc32 = 123,
                IsStable = false,
                CreatedAt = DateTime.UtcNow
            };
            DbContext.FirmwareVersions.Add(unstableFirmware);
            await DbContext.SaveChangesAsync();

            // Act
            var result = await _repository.GetLatestStableAsync();

            // Assert
            result.ShouldBeNull();
        }

        [Fact]
        public async Task GetLatestStableAsync_MultipleStableFirmware_ReturnsLatest()
        {
            // Arrange
            var older = new FirmwareVersion
            {
                Id = Guid.NewGuid(),
                Version = "1.0.0",
                Description = "Old stable",
                FileSize = 1024,
                Crc32 = 123,
                IsStable = true,
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            };
            var newer = new FirmwareVersion
            {
                Id = Guid.NewGuid(),
                Version = "2.0.0",
                Description = "New stable",
                FileSize = 2048,
                Crc32 = 456,
                IsStable = true,
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            };
            var unstable = new FirmwareVersion
            {
                Id = Guid.NewGuid(),
                Version = "3.0.0-beta",
                Description = "Beta",
                FileSize = 3072,
                Crc32 = 789,
                IsStable = false,
                CreatedAt = DateTime.UtcNow
            };
            DbContext.FirmwareVersions.AddRange(older, newer, unstable);
            await DbContext.SaveChangesAsync();

            // Act
            var result = await _repository.GetLatestStableAsync();

            // Assert
            result.ShouldNotBeNull();
            result.Version.ShouldBe("2.0.0");
            result.Id.ShouldBe(newer.Id);
        }
    }

    public class CreateUpdateAsync(PostgresFixture fixture) : FirmwareRepositoryTests(fixture)
    {
        [Fact]
        public async Task CreateUpdateAsync_ValidUpdate_SavesAndReturnsWithId()
        {
            // Arrange
            var analyzer = await DbContext.SeedSourdoughAnalyzerAsync(_timeProvider, "AA:BB:CC:DD:EE:FF", "Test Analyzer", isActivated: true);
            var firmware = new FirmwareVersion
            {
                Id = Guid.NewGuid(),
                Version = "1.0.0",
                Description = "Test",
                FileSize = 1024,
                Crc32 = 123,
                IsStable = true,
                CreatedAt = DateTime.UtcNow
            };
            DbContext.FirmwareVersions.Add(firmware);
            await DbContext.SaveChangesAsync();

            var update = new FirmwareUpdate
            {
                AnalyzerId = analyzer.Id,
                FirmwareVersionId = firmware.Id,
                Status = FirmwareUpdate.OtaStatus.Started,
                Progress = 0
            };
            var testTime = new DateTime(2024, 1, 15, 14, 30, 0, DateTimeKind.Utc);
            _timeProvider.SetUtcNow(testTime);

            // Act
            var result = await _repository.CreateUpdateAsync(update);

            // Assert
            result.Id.ShouldNotBe(Guid.Empty);
            result.StartedAt.ShouldBe(testTime);
            result.CompletedAt.ShouldBeNull();
            
            var dbEntity = await DbContext.FirmwareUpdates.FindAsync(result.Id);
            dbEntity.ShouldNotBeNull();
            dbEntity.Status.ShouldBe(FirmwareUpdate.OtaStatus.Started);
        }

        [Fact]
        public async Task CreateUpdateAsync_WithErrorMessage_SavesWithError()
        {
            // Arrange
            var analyzer = await DbContext.SeedSourdoughAnalyzerAsync(_timeProvider, "AA:BB:CC:DD:EE:FF", "Test Analyzer", isActivated: true);
            var firmware = new FirmwareVersion
            {
                Id = Guid.NewGuid(),
                Version = "1.0.0",
                Description = "Test",
                FileSize = 1024,
                Crc32 = 123,
                IsStable = true,
                CreatedAt = DateTime.UtcNow
            };
            DbContext.FirmwareVersions.Add(firmware);
            await DbContext.SaveChangesAsync();

            var update = new FirmwareUpdate
            {
                AnalyzerId = analyzer.Id,
                FirmwareVersionId = firmware.Id,
                Status = FirmwareUpdate.OtaStatus.Failed,
                Progress = 0,
                ErrorMessage = "Connection timeout"
            };

            // Act
            var result = await _repository.CreateUpdateAsync(update);

            // Assert
            result.ErrorMessage.ShouldBe("Connection timeout");
        }
    }

    public class UpdateUpdateStatusAsync(PostgresFixture fixture) : FirmwareRepositoryTests(fixture)
    {
        [Fact]
        public async Task UpdateUpdateStatusAsync_ExistingUpdate_UpdatesStatus()
        {
            // Arrange
            var analyzer = await DbContext.SeedSourdoughAnalyzerAsync(_timeProvider, "AA:BB:CC:DD:EE:FF", "Test Analyzer", isActivated: true);
            var firmware = new FirmwareVersion
            {
                Id = Guid.NewGuid(),
                Version = "1.0.0",
                Description = "Test",
                FileSize = 1024,
                Crc32 = 123,
                IsStable = true,
                CreatedAt = DateTime.UtcNow
            };
            var update = new FirmwareUpdate
            {
                Id = Guid.NewGuid(),
                AnalyzerId = analyzer.Id,
                FirmwareVersionId = firmware.Id,
                Status = FirmwareUpdate.OtaStatus.Started,
                Progress = 0,
                StartedAt = DateTime.UtcNow.AddMinutes(-5)
            };
            DbContext.FirmwareVersions.Add(firmware);
            DbContext.FirmwareUpdates.Add(update);
            await DbContext.SaveChangesAsync();

            // Act
            await _repository.UpdateUpdateStatusAsync(update.Id, FirmwareUpdate.OtaStatus.Downloading, 25);

            // Assert
            var dbEntity = await DbContext.FirmwareUpdates.FindAsync(update.Id);
            dbEntity.ShouldNotBeNull();
            dbEntity.Status.ShouldBe(FirmwareUpdate.OtaStatus.Downloading);
            dbEntity.Progress.ShouldBe(25);
            dbEntity.CompletedAt.ShouldBeNull();
        }

        [Fact]
        public async Task UpdateUpdateStatusAsync_StatusComplete_SetsCompletedAt()
        {
            // Arrange
            var analyzer = await DbContext.SeedSourdoughAnalyzerAsync(_timeProvider, "AA:BB:CC:DD:EE:FF", "Test Analyzer", isActivated: true);
            var firmware = new FirmwareVersion
            {
                Id = Guid.NewGuid(),
                Version = "1.0.0",
                Description = "Test",
                FileSize = 1024,
                Crc32 = 123,
                IsStable = true,
                CreatedAt = DateTime.UtcNow
            };
            var update = new FirmwareUpdate
            {
                Id = Guid.NewGuid(),
                AnalyzerId = analyzer.Id,
                FirmwareVersionId = firmware.Id,
                Status = FirmwareUpdate.OtaStatus.Applying,
                Progress = 90,
                StartedAt = DateTime.UtcNow.AddMinutes(-10)
            };
            DbContext.FirmwareVersions.Add(firmware);
            DbContext.FirmwareUpdates.Add(update);
            await DbContext.SaveChangesAsync();
            
            var completionTime = new DateTime(2024, 1, 15, 16, 0, 0, DateTimeKind.Utc);
            _timeProvider.SetUtcNow(completionTime);

            // Act
            await _repository.UpdateUpdateStatusAsync(update.Id, FirmwareUpdate.OtaStatus.Complete, 100);

            // Assert
            var dbEntity = await DbContext.FirmwareUpdates.FindAsync(update.Id);
            dbEntity.ShouldNotBeNull();
            dbEntity.Status.ShouldBe(FirmwareUpdate.OtaStatus.Complete);
            dbEntity.Progress.ShouldBe(100);
            dbEntity.CompletedAt.ShouldBe(completionTime);
        }

        [Fact]
        public async Task UpdateUpdateStatusAsync_StatusFailed_SetsCompletedAt()
        {
            // Arrange
            var analyzer = await DbContext.SeedSourdoughAnalyzerAsync(_timeProvider, "AA:BB:CC:DD:EE:FF", "Test Analyzer", isActivated: true);
            var firmware = new FirmwareVersion
            {
                Id = Guid.NewGuid(),
                Version = "1.0.0",
                Description = "Test",
                FileSize = 1024,
                Crc32 = 123,
                IsStable = true,
                CreatedAt = DateTime.UtcNow
            };
            var update = new FirmwareUpdate
            {
                Id = Guid.NewGuid(),
                AnalyzerId = analyzer.Id,
                FirmwareVersionId = firmware.Id,
                Status = FirmwareUpdate.OtaStatus.Downloading,
                Progress = 50,
                StartedAt = DateTime.UtcNow.AddMinutes(-5)
            };
            DbContext.FirmwareVersions.Add(firmware);
            DbContext.FirmwareUpdates.Add(update);
            await DbContext.SaveChangesAsync();
            
            var failureTime = new DateTime(2024, 1, 15, 17, 30, 0, DateTimeKind.Utc);
            _timeProvider.SetUtcNow(failureTime);

            // Act
            await _repository.UpdateUpdateStatusAsync(update.Id, FirmwareUpdate.OtaStatus.Failed);

            // Assert
            var dbEntity = await DbContext.FirmwareUpdates.FindAsync(update.Id);
            dbEntity.ShouldNotBeNull();
            dbEntity.Status.ShouldBe(FirmwareUpdate.OtaStatus.Failed);
            dbEntity.Progress.ShouldBe(50);
            dbEntity.CompletedAt.ShouldBe(failureTime);
        }

        [Fact]
        public async Task UpdateUpdateStatusAsync_NonExistentId_DoesNotThrow()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            await _repository.UpdateUpdateStatusAsync(nonExistentId, FirmwareUpdate.OtaStatus.Complete, 100);

            // Assert
            var updateCount = await DbContext.FirmwareUpdates.CountAsync();
            updateCount.ShouldBe(0);
        }
    }

    public class GetActiveUpdateForAnalyzerAsync(PostgresFixture fixture) : FirmwareRepositoryTests(fixture)
    {
        [Fact]
        public async Task GetActiveUpdateForAnalyzerAsync_NoActiveUpdate_ReturnsNull()
        {
            // Arrange
            var analyzer = await DbContext.SeedSourdoughAnalyzerAsync(_timeProvider, "AA:BB:CC:DD:EE:FF", "Test Analyzer", isActivated: true);
            var firmware = new FirmwareVersion
            {
                Id = Guid.NewGuid(),
                Version = "1.0.0",
                Description = "Test",
                FileSize = 1024,
                Crc32 = 123,
                IsStable = true,
                CreatedAt = DateTime.UtcNow
            };
            var completedUpdate = new FirmwareUpdate
            {
                Id = Guid.NewGuid(),
                AnalyzerId = analyzer.Id,
                FirmwareVersionId = firmware.Id,
                Status = FirmwareUpdate.OtaStatus.Complete,
                Progress = 100,
                StartedAt = DateTime.UtcNow.AddHours(-1),
                CompletedAt = DateTime.UtcNow.AddMinutes(-30)
            };
            DbContext.FirmwareVersions.Add(firmware);
            DbContext.FirmwareUpdates.Add(completedUpdate);
            await DbContext.SaveChangesAsync();

            // Act
            var result = await _repository.GetActiveUpdateForAnalyzerAsync(analyzer.Id);

            // Assert
            result.ShouldBeNull();
        }

        [Fact]
        public async Task GetActiveUpdateForAnalyzerAsync_ActiveStartedUpdate_ReturnsUpdate()
        {
            // Arrange
            var analyzer = await DbContext.SeedSourdoughAnalyzerAsync(_timeProvider, "AA:BB:CC:DD:EE:FF", "Test Analyzer", isActivated: true);
            var firmware = new FirmwareVersion
            {
                Id = Guid.NewGuid(),
                Version = "1.0.0",
                Description = "Test",
                FileSize = 1024,
                Crc32 = 123,
                IsStable = true,
                CreatedAt = DateTime.UtcNow
            };
            var activeUpdate = new FirmwareUpdate
            {
                Id = Guid.NewGuid(),
                AnalyzerId = analyzer.Id,
                FirmwareVersionId = firmware.Id,
                Status = FirmwareUpdate.OtaStatus.Started,
                Progress = 0,
                StartedAt = DateTime.UtcNow.AddMinutes(-5)
            };
            DbContext.FirmwareVersions.Add(firmware);
            DbContext.FirmwareUpdates.Add(activeUpdate);
            await DbContext.SaveChangesAsync();

            // Act
            var result = await _repository.GetActiveUpdateForAnalyzerAsync(analyzer.Id);

            // Assert
            result.ShouldNotBeNull();
            result.Id.ShouldBe(activeUpdate.Id);
            result.Status.ShouldBe(FirmwareUpdate.OtaStatus.Started);
        }

        [Fact]
        public async Task GetActiveUpdateForAnalyzerAsync_ActiveDownloadingUpdate_ReturnsUpdate()
        {
            // Arrange
            var analyzer = await DbContext.SeedSourdoughAnalyzerAsync(_timeProvider, "AA:BB:CC:DD:EE:FF", "Test Analyzer", isActivated: true);
            var firmware = new FirmwareVersion
            {
                Id = Guid.NewGuid(),
                Version = "1.0.0",
                Description = "Test",
                FileSize = 1024,
                Crc32 = 123,
                IsStable = true,
                CreatedAt = DateTime.UtcNow
            };
            var activeUpdate = new FirmwareUpdate
            {
                Id = Guid.NewGuid(),
                AnalyzerId = analyzer.Id,
                FirmwareVersionId = firmware.Id,
                Status = FirmwareUpdate.OtaStatus.Downloading,
                Progress = 50,
                StartedAt = DateTime.UtcNow.AddMinutes(-10)
            };
            DbContext.FirmwareVersions.Add(firmware);
            DbContext.FirmwareUpdates.Add(activeUpdate);
            await DbContext.SaveChangesAsync();

            // Act
            var result = await _repository.GetActiveUpdateForAnalyzerAsync(analyzer.Id);

            // Assert
            result.ShouldNotBeNull();
            result.Id.ShouldBe(activeUpdate.Id);
            result.Status.ShouldBe(FirmwareUpdate.OtaStatus.Downloading);
        }

        [Fact]
        public async Task GetActiveUpdateForAnalyzerAsync_ActiveApplyingUpdate_ReturnsUpdate()
        {
            // Arrange
            var analyzer = await DbContext.SeedSourdoughAnalyzerAsync(_timeProvider, "AA:BB:CC:DD:EE:FF", "Test Analyzer", isActivated: true);
            var firmware = new FirmwareVersion
            {
                Id = Guid.NewGuid(),
                Version = "1.0.0",
                Description = "Test",
                FileSize = 1024,
                Crc32 = 123,
                IsStable = true,
                CreatedAt = DateTime.UtcNow
            };
            var activeUpdate = new FirmwareUpdate
            {
                Id = Guid.NewGuid(),
                AnalyzerId = analyzer.Id,
                FirmwareVersionId = firmware.Id,
                Status = FirmwareUpdate.OtaStatus.Applying,
                Progress = 90,
                StartedAt = DateTime.UtcNow.AddMinutes(-15)
            };
            DbContext.FirmwareVersions.Add(firmware);
            DbContext.FirmwareUpdates.Add(activeUpdate);
            await DbContext.SaveChangesAsync();

            // Act
            var result = await _repository.GetActiveUpdateForAnalyzerAsync(analyzer.Id);

            // Assert
            result.ShouldNotBeNull();
            result.Id.ShouldBe(activeUpdate.Id);
            result.Status.ShouldBe(FirmwareUpdate.OtaStatus.Applying);
        }

        [Fact]
        public async Task GetActiveUpdateForAnalyzerAsync_MultipleUpdates_ReturnsLatestActive()
        {
            // Arrange
            var analyzer = await DbContext.SeedSourdoughAnalyzerAsync(_timeProvider, "AA:BB:CC:DD:EE:FF", "Test Analyzer", isActivated: true);
            var firmware = new FirmwareVersion
            {
                Id = Guid.NewGuid(),
                Version = "1.0.0",
                Description = "Test",
                FileSize = 1024,
                Crc32 = 123,
                IsStable = true,
                CreatedAt = DateTime.UtcNow
            };
            var olderActiveUpdate = new FirmwareUpdate
            {
                Id = Guid.NewGuid(),
                AnalyzerId = analyzer.Id,
                FirmwareVersionId = firmware.Id,
                Status = FirmwareUpdate.OtaStatus.Started,
                Progress = 0,
                StartedAt = DateTime.UtcNow.AddHours(-2)
            };
            var newerActiveUpdate = new FirmwareUpdate
            {
                Id = Guid.NewGuid(),
                AnalyzerId = analyzer.Id,
                FirmwareVersionId = firmware.Id,
                Status = FirmwareUpdate.OtaStatus.Downloading,
                Progress = 25,
                StartedAt = DateTime.UtcNow.AddMinutes(-10)
            };
            var completedUpdate = new FirmwareUpdate
            {
                Id = Guid.NewGuid(),
                AnalyzerId = analyzer.Id,
                FirmwareVersionId = firmware.Id,
                Status = FirmwareUpdate.OtaStatus.Complete,
                Progress = 100,
                StartedAt = DateTime.UtcNow.AddDays(-1),
                CompletedAt = DateTime.UtcNow.AddDays(-1).AddMinutes(30)
            };
            DbContext.FirmwareVersions.Add(firmware);
            DbContext.FirmwareUpdates.AddRange(olderActiveUpdate, newerActiveUpdate, completedUpdate);
            await DbContext.SaveChangesAsync();

            // Act
            var result = await _repository.GetActiveUpdateForAnalyzerAsync(analyzer.Id);

            // Assert
            result.ShouldNotBeNull();
            result.Id.ShouldBe(newerActiveUpdate.Id);
        }

        [Fact]
        public async Task GetActiveUpdateForAnalyzerAsync_DifferentAnalyzer_ReturnsNull()
        {
            // Arrange
            var analyzer1 = await DbContext.SeedSourdoughAnalyzerAsync(_timeProvider, "AA:BB:CC:DD:EE:FF", "Analyzer 1", isActivated: true);
            var analyzer2 = await DbContext.SeedSourdoughAnalyzerAsync(_timeProvider, "11:22:33:44:55:66", "Analyzer 2", isActivated: true);
            var firmware = new FirmwareVersion
            {
                Id = Guid.NewGuid(),
                Version = "1.0.0",
                Description = "Test",
                FileSize = 1024,
                Crc32 = 123,
                IsStable = true,
                CreatedAt = DateTime.UtcNow
            };
            var activeUpdate = new FirmwareUpdate
            {
                Id = Guid.NewGuid(),
                AnalyzerId = analyzer1.Id,
                FirmwareVersionId = firmware.Id,
                Status = FirmwareUpdate.OtaStatus.Downloading,
                Progress = 50,
                StartedAt = DateTime.UtcNow.AddMinutes(-5)
            };
            DbContext.FirmwareVersions.Add(firmware);
            DbContext.FirmwareUpdates.Add(activeUpdate);
            await DbContext.SaveChangesAsync();

            // Act
            var result = await _repository.GetActiveUpdateForAnalyzerAsync(analyzer2.Id);

            // Assert
            result.ShouldBeNull();
        }
    }
}