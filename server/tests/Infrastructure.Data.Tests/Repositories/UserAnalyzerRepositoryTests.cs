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
public class PgUserAnalyzerRepositoryTests : RepositoryTestBase
{
    private readonly FakeTimeProvider _timeProvider;
    private readonly PgUserAnalyzerRepository _repository;

    private PgUserAnalyzerRepositoryTests(PostgresFixture fixture) : base(fixture)
    {
        _timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        _repository = new PgUserAnalyzerRepository(DbContext);
    }

    public class SaveAsync(PostgresFixture fixture) : PgUserAnalyzerRepositoryTests(fixture)
    {
        [Fact]
        public async Task SaveAsync_WithUserAnalyzer_SavesAndReturnsId()
        {
            // Arrange
            var user = await DbContext.SeedUserAsync(_timeProvider);
            var analyzer = await DbContext.SeedSourdoughAnalyzerAsync(_timeProvider, "AA:BB:CC:DD:EE:FF", "Test Analyzer");
            
            var userAnalyzer = new UserAnalyzer
            {
                UserId = user.Id,
                AnalyzerId = analyzer.Id,
                Nickname = "My Test Analyzer",
                IsOwner = true
            };

            // Act
            var id = await _repository.SaveAsync(userAnalyzer);

            // Assert
            id.ShouldNotBe(Guid.Empty);
            var savedUserAnalyzer = await DbContext.UserAnalyzers.AsNoTracking().FirstOrDefaultAsync(ua => ua.Id == id);
            savedUserAnalyzer.ShouldNotBeNull();
            savedUserAnalyzer.UserId.ShouldBe(user.Id);
            savedUserAnalyzer.AnalyzerId.ShouldBe(analyzer.Id);
            savedUserAnalyzer.Nickname.ShouldBe(userAnalyzer.Nickname);
            savedUserAnalyzer.IsOwner.ShouldBe(userAnalyzer.IsOwner);
        }
    }
} 