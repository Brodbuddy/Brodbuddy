using Core.Entities;
using Core.Extensions;
using Infrastructure.Data.Repositories.Sourdough;
using Infrastructure.Data.Tests.Bases;
using Infrastructure.Data.Tests.Database;
using Microsoft.EntityFrameworkCore;
using SharedTestDependencies.Constants;
using SharedTestDependencies.Extensions;
using SharedTestDependencies.Fakes;
using SharedTestDependencies.Fixtures;
using Shouldly;

namespace Infrastructure.Data.Tests.Repositories;

[Collection(TestCollections.Database)]
public class SourdoughAnalyzerRepositoryTests : RepositoryTestBase
{
    private readonly FakeTimeProvider _timeProvider;
    private readonly PgSourdoughAnalyzerRepository _repository;

    private SourdoughAnalyzerRepositoryTests(PostgresFixture fixture) : base(fixture)
    {
        _timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        _repository = new PgSourdoughAnalyzerRepository(DbContext);
    }

    public class GetByMacAddressAsync(PostgresFixture fixture) : SourdoughAnalyzerRepositoryTests(fixture)
    {
        [Fact]
        public async Task GetByMacAddressAsync_WithExistingAnalyzer_ReturnsAnalyzer()
        {
            // Arrange
            var analyzer = await DbContext.SeedSourdoughAnalyzerAsync(_timeProvider, "AA:BB:CC:DD:EE:FF", "Test Analyzer");

            // Act
            var result = await _repository.GetByMacAddressAsync(analyzer.MacAddress);

            // Assert
            result.ShouldNotBeNull();
            result.Id.ShouldBe(analyzer.Id);
            result.MacAddress.ShouldBe(analyzer.MacAddress);
            result.Name.ShouldBe(analyzer.Name);
        }

        [Fact]
        public async Task GetByMacAddressAsync_WithNonExistingMacAddress_ReturnsNull()
        {
            // Arrange
            const string nonExistentMacAddress = "11:22:33:44:55:66";

            // Act
            var result = await _repository.GetByMacAddressAsync(nonExistentMacAddress);

            // Assert
            result.ShouldBeNull();
        }
    }

    public class GetByActivationCodeAsync(PostgresFixture fixture) : SourdoughAnalyzerRepositoryTests(fixture)
    {
        [Fact]
        public async Task GetByActivationCodeAsync_WithExistingAnalyzer_ReturnsAnalyzerWithUserAnalyzers()
        {
            // Arrange
            var analyzer = await DbContext.SeedSourdoughAnalyzerAsync(_timeProvider, "AA:BB:CC:DD:EE:FF", "Test Analyzer");
            var user = await DbContext.SeedUserAsync(_timeProvider);
            await DbContext.SeedUserAnalyzerAsync(_timeProvider, user.Id, analyzer.Id, "Min Analyzer", true);

            // Act
            var result = await _repository.GetByActivationCodeAsync(analyzer.ActivationCode ?? throw new InvalidOperationException("ActivationCode should not be null"));

            // Assert
            result.ShouldNotBeNull();
            result.Id.ShouldBe(analyzer.Id);
            result.ActivationCode.ShouldBe(analyzer.ActivationCode);
            result.UserAnalyzers.Count.ShouldBe(1);
            result.UserAnalyzers.First().UserId.ShouldBe(user.Id);
        }

        [Fact]
        public async Task GetByActivationCodeAsync_WithNonExistingCode_ReturnsNull()
        {
            // Arrange
            const string nonExistentCode = "NONEXISTENT123";

            // Act
            var result = await _repository.GetByActivationCodeAsync(nonExistentCode);

            // Assert
            result.ShouldBeNull();
        }
    }

    public class SaveAsync(PostgresFixture fixture) : SourdoughAnalyzerRepositoryTests(fixture)
    {
        [Fact]
        public async Task SaveAsync_WithNewAnalyzer_SavesAndReturnsId()
        {
            // Arrange
            var analyzer = new SourdoughAnalyzer
            {
                MacAddress = "AA:BB:CC:DD:EE:FF",
                Name = "Test Analyzer",
                ActivationCode = "TEST12345678",
                IsActivated = false,
                CreatedAt = _timeProvider.Now(),
                UpdatedAt = _timeProvider.Now()
            };

            // Act
            var id = await _repository.SaveAsync(analyzer);

            // Assert
            id.ShouldNotBe(Guid.Empty);
            var savedAnalyzer = await DbContext.SourdoughAnalyzers.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
            savedAnalyzer.ShouldNotBeNull();
            savedAnalyzer.MacAddress.ShouldBe(analyzer.MacAddress);
            savedAnalyzer.Name.ShouldBe(analyzer.Name);
            savedAnalyzer.ActivationCode.ShouldBe(analyzer.ActivationCode);
            savedAnalyzer.IsActivated.ShouldBe(analyzer.IsActivated);
            savedAnalyzer.CreatedAt.ShouldBeWithinTolerance(analyzer.CreatedAt);
        }
    }

    public class GetByUserIdAsync(PostgresFixture fixture) : SourdoughAnalyzerRepositoryTests(fixture)
    {
        [Fact]
        public async Task GetByUserIdAsync_WithExistingUserAnalyzers_ReturnsAnalyzers()
        {
            // Arrange
            var user = await DbContext.SeedUserAsync(_timeProvider);
            var analyzer1 = await DbContext.SeedSourdoughAnalyzerAsync(_timeProvider, "AA:BB:CC:DD:EE:FF", "Analyzer 1");
            var analyzer2 = await DbContext.SeedSourdoughAnalyzerAsync(_timeProvider, "11:22:33:44:55:66", "Analyzer 2");
            
            await DbContext.SeedUserAnalyzerAsync(_timeProvider, user.Id, analyzer1.Id, "Min første Analyzer", true);
            await DbContext.SeedUserAnalyzerAsync(_timeProvider, user.Id, analyzer2.Id, "Min anden Analyzer", false);

            // Act
            var results = await _repository.GetByUserIdAsync(user.Id);

            // Assert
            var analyzerList = results.ToList();
            analyzerList.Count.ShouldBe(2);
            analyzerList.ShouldContain(a => a.Id == analyzer1.Id);
            analyzerList.ShouldContain(a => a.Id == analyzer2.Id);
            analyzerList.All(a => a.UserAnalyzers.Count > 0).ShouldBeTrue();
        }

        [Fact]
        public async Task GetByUserIdAsync_WithNoAnalyzers_ReturnsEmptyList()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            var results = await _repository.GetByUserIdAsync(userId);

            // Assert
            results.ShouldBeEmpty();
        }
    }

    public class GetAllAsync(PostgresFixture fixture) : SourdoughAnalyzerRepositoryTests(fixture)
    {
        [Fact]
        public async Task GetAllAsync_WithMultipleAnalyzers_ReturnsAllAnalyzers()
        {
            // Arrange
            var analyzer1 = await DbContext.SeedSourdoughAnalyzerAsync(_timeProvider, "AA:BB:CC:DD:EE:FF", "Analyzer 1");
            var analyzer2 = await DbContext.SeedSourdoughAnalyzerAsync(_timeProvider, "11:22:33:44:55:66", "Analyzer 2");
            var user = await DbContext.SeedUserAsync(_timeProvider);
            await DbContext.SeedUserAnalyzerAsync(_timeProvider, user.Id, analyzer1.Id, "Min Analyzer", true);

            // Act
            var results = await _repository.GetAllAsync();

            // Assert
            var analyzerList = results.ToList();
            analyzerList.Count.ShouldBe(2);
            analyzerList.ShouldContain(a => a.Id == analyzer1.Id);
            analyzerList.ShouldContain(a => a.Id == analyzer2.Id);
            analyzerList.First(a => a.Id == analyzer1.Id).UserAnalyzers.Count.ShouldBeGreaterThan(0);
            analyzerList.First(a => a.Id == analyzer2.Id).UserAnalyzers.Count.ShouldBe(0);
        }

        [Fact]
        public async Task GetAllAsync_WithNoAnalyzers_ReturnsEmptyList()
        {
            // Act
            var results = await _repository.GetAllAsync();

            // Assert
            results.ShouldBeEmpty();
        }
    }
} 