using Infrastructure.Data.Postgres;
using SharedTestDependencies;
using Shouldly;

namespace Infrastructure.Data.Tests;

[Collection(TestCollections.Database)]
public class OtpRepositoryTests : RepositoryTestBase
{
    private readonly FakeTimeProvider _timeProvider;
    private readonly PostgresOtpRepository _repository;

    public OtpRepositoryTests(PostgresFixture fixture) : base(fixture)
    {
        _timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        _repository = new PostgresOtpRepository(_dbContext, _timeProvider);
    }

    [Fact]
    public async Task SaveAsync_WithValidOtp_SavesInDatabase()
    {
        // Arrange
        int code = 123123;

        // Act
        Guid id = await _repository.SaveAsync(code);

        // Assert
        var savedOtp = await _dbContext.OneTimePasswords.FindAsync(id);
        savedOtp.ShouldNotBeNull();
        savedOtp.Code.ShouldBe(code);
        savedOtp.IsUsed.ShouldBeFalse();
        savedOtp.CreatedAt.ShouldBe(_timeProvider.GetUtcNow().UtcDateTime);
        savedOtp.ExpiresAt.ShouldBe(_timeProvider.GetUtcNow().UtcDateTime.AddMinutes(15));
    }

    [Fact]
    public async Task IsValidAsync_WithValidOtp_ReturnsTrue()
    {
        // Arrange
        int code = 333333;
        Guid id = await _repository.SaveAsync(code);

        // Act
        bool isValid = await _repository.IsValidAsync(id, code);

        // Assert
        isValid.ShouldBeTrue();
    }

    [Fact]
    public async Task IsValidAsync_WithIncorrectCode_ReturnsFalse()
    {
        // Arrange
        int code = 333333;
        Guid id = await _repository.SaveAsync(code);

        // Act
        bool isValid = await _repository.IsValidAsync(id, 444444);

        // Assert
        isValid.ShouldBeFalse();
    }

    [Fact]
    public async Task IsValidAsync_WithExpiredOtp_ReturnsFalse()
    {
        // Arrange
        int code = 444444;
        Guid id = await _repository.SaveAsync(code);

        // sætter tiden 16 min frem - alt over 15 minutter skal få den til at returne false
        _timeProvider.Advance(TimeSpan.FromMinutes(16));

        // Act
        bool isValid = await _repository.IsValidAsync(id, code);

        // Assert
        isValid.ShouldBeFalse();
    }

    [Fact]
    public async Task IsValidAsync_WithUsedOtp_ReturnsFalse()
    {
        // Arrange
        int code = 444444;
        Guid id = await _repository.SaveAsync(code);

        // Ændrer id'et til brugt.
        await _repository.MarkAsUsedAsync(id);

        // Act
        bool isValid = await _repository.IsValidAsync(id, code);

        // Assert
        isValid.ShouldBeFalse();
    }

    [Fact]
    public async Task MarkAsUsedAsync_WithValidOtp_ReturnsTrue()
    {
        // Arrange
        int code = 555555;
        Guid id = await _repository.SaveAsync(code);

        // Act
        bool result = await _repository.MarkAsUsedAsync(id);

        // Assert
        result.ShouldBeTrue();

        // verificer i databasen at Otp er ændret til brugt
        var otp = await _dbContext.OneTimePasswords.FindAsync(id);
        otp.ShouldNotBeNull();
        otp.IsUsed.ShouldBeTrue();
    }
}