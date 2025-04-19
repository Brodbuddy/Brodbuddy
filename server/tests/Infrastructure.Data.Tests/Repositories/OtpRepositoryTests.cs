using Core.Extensions;
using Infrastructure.Data.Repositories;
using Infrastructure.Data.Tests.Bases;
using Microsoft.EntityFrameworkCore;
using SharedTestDependencies.Constants;
using SharedTestDependencies.Database;
using SharedTestDependencies.Extensions;
using SharedTestDependencies.Fakes;
using Shouldly;

namespace Infrastructure.Data.Tests.Repositories;

[Collection(TestCollections.Database)]
public class OtpRepositoryTests : RepositoryTestBase
{
    private readonly FakeTimeProvider _timeProvider;
    private readonly PgOtpRepository _repository;

    private OtpRepositoryTests(PostgresFixture fixture) : base(fixture)
    {
        _timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        _repository = new PgOtpRepository(DbContext, _timeProvider);
    }

    public class SaveAsync(PostgresFixture fixture) : OtpRepositoryTests(fixture)
    {
        [Fact]
        public async Task SaveAsync_WithValidOtp_CreatesOtpAndReturnsId()
        {
            // Arrange
            int code = 123123;
            var expectedCreationTime = _timeProvider.Now();
            var expectedExpiryTime = expectedCreationTime.AddMinutes(15);

            // Act
            Guid id = await _repository.SaveAsync(code);

            // Assert
            var savedOtp = await DbContext.OneTimePasswords.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);
            savedOtp.ShouldNotBeNull();
            savedOtp.Code.ShouldBe(code);
            savedOtp.IsUsed.ShouldBeFalse();
            savedOtp.CreatedAt.ShouldBeWithinTolerance(expectedCreationTime);
            savedOtp.ExpiresAt.ShouldBeWithinTolerance(expectedExpiryTime); 
        }
    }

    public class IsValidAsync(PostgresFixture fixture) : OtpRepositoryTests(fixture)
    {
        [Fact]
        public async Task IsValidAsync_WithValidOtp_ReturnsTrue()
        {
            // Arrange
            var otp = await DbContext.SeedOtpAsync(_timeProvider);

            // Act
            bool isValid = await _repository.IsValidAsync(otp.Id, otp.Code);

            // Assert
            isValid.ShouldBeTrue();
        }

        [Fact]
        public async Task IsValidAsync_WithIncorrectCode_ReturnsFalse()
        {
            // Arrange
            var otp = await DbContext.SeedOtpAsync(_timeProvider, code: 333333);

            // Act
            bool isValid = await _repository.IsValidAsync(otp.Id, 444444);

            // Assert
            isValid.ShouldBeFalse();
        }

        [Fact]
        public async Task IsValidAsync_WithExpiredOtp_ReturnsFalse()
        {
            // Arrange
            var otp = await DbContext.SeedOtpAsync(_timeProvider, expiresMinutes: 15);

            // sætter tiden 16 min frem - alt over 15 minutter skal få den til at returne false
            _timeProvider.Advance(TimeSpan.FromMinutes(16));

            // Act
            bool isValid = await _repository.IsValidAsync(otp.Id, otp.Code);

            // Assert
            isValid.ShouldBeFalse();
        }

        [Fact]
        public async Task IsValidAsync_WithUsedOtp_ReturnsFalse()
        {
            // Arrange
            var otp = await DbContext.SeedOtpAsync(_timeProvider);
            
            // Ændrer id'et til brugt.
            await _repository.MarkAsUsedAsync(otp.Id);
            DbContext.ChangeTracker.Clear(); // Tving IsValidAsync til at læse frisk fra DB

            // Act
            bool isValid = await _repository.IsValidAsync(otp.Id, otp.Code);

            // Assert
            isValid.ShouldBeFalse();
        }

        [Fact]
        public async Task IsValidAsync_WhenExpiryIsExactlyNow_ReturnsTrue()
        {
            // Arrange
            var otp = await DbContext.SeedOtpAsync(_timeProvider);

            // Sæt TimeProvider's nuværende tid til at være PRÆCIS udløbstiden
            _timeProvider.SetUtcNow(new DateTimeOffset(otp.ExpiresAt, TimeSpan.Zero));

            // Act
            bool isValid = await _repository.IsValidAsync(otp.Id, otp.Code);

            // Assert
            isValid.ShouldBeTrue();
        }
    }

    public class MarkAsUsedAsync(PostgresFixture fixture) : OtpRepositoryTests(fixture)
    {
        [Fact]
        public async Task MarkAsUsedAsync_WithValidOtp_ReturnsTrue()
        {
            // Arrange
            var otpToMark = await DbContext.SeedOtpAsync(_timeProvider);

            // Pre-assert
            otpToMark.IsUsed.ShouldBeFalse();

            // Act
            bool result = await _repository.MarkAsUsedAsync(otpToMark.Id);

            // Assert
            result.ShouldBeTrue();

            // verificer i databasen at Otp er ændret til brugt
            var updatedOtp = await DbContext.OneTimePasswords
                .AsNoTracking()
                .FirstOrDefaultAsync(otp => otp.Id == otpToMark.Id);
            updatedOtp.ShouldNotBeNull();
            updatedOtp.IsUsed.ShouldBeTrue();
        }

        [Fact]
        public async Task MarkAsUsedAsync_WithNonExistingId_ReturnsFalse()
        {
            // Arrange
            var nonExistingId = Guid.NewGuid();
            await DbContext.SeedOtpAsync(_timeProvider);

            // Act
            bool result = await _repository.MarkAsUsedAsync(nonExistingId);

            // Assert
            result.ShouldBeFalse();
        }
    }
}