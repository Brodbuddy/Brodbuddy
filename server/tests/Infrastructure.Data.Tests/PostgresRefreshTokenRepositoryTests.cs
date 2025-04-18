using Infrastructure.Data.Postgres;
using Microsoft.EntityFrameworkCore;
using SharedTestDependencies;
using Shouldly;


namespace Infrastructure.Data.Tests;

[Collection(TestCollections.Database)]
public class PostgresRefreshTokenRepositoryTests : RepositoryTestBase
{
    private readonly FakeTimeProvider _timeProvider;
    private readonly PostgresRefreshTokenRepository _repository;

    private PostgresRefreshTokenRepositoryTests(PostgresFixture fixture) : base(fixture)
    {
        _timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        _repository = new PostgresRefreshTokenRepository(DbContext, _timeProvider);
    }


    public class CreateAsync(PostgresFixture fixture) : PostgresRefreshTokenRepositoryTests(fixture)
    {
        [Fact]
        public async Task CreateAsync_WithValidToken_ShouldCreateTokenInDatabase()
        {
            // Arrange
            string token = "testToken";
            DateTime expiresAt = _timeProvider.GetUtcNow().UtcDateTime.AddDays(30);

            // Act
            var result = await _repository.CreateAsync(token, expiresAt);

            // Assert
            var savedToken = await DbContext.RefreshTokens.FindAsync(result.tokenId);
            savedToken.ShouldNotBeNull();
            savedToken.Token.ShouldBe(token);
            savedToken.CreatedAt.ShouldBe(_timeProvider.GetUtcNow().UtcDateTime);
            savedToken.ExpiresAt.ShouldBe(expiresAt);
            savedToken.RevokedAt.ShouldBeNull();
            savedToken.ReplacedByTokenId.ShouldBeNull();
        }

        [Fact]
        public async Task CreateAsync_WithFarFutureExpiryDate_ShouldCreateValidToken()
        {
            // Arrange
            string token = "farFutureToken";
            DateTime expiresAt = _timeProvider.GetUtcNow().UtcDateTime.AddYears(10);

            // Act
            var result = await _repository.CreateAsync(token, expiresAt);

            // Assert
            var savedToken = await DbContext.RefreshTokens.FindAsync(result.tokenId);
            savedToken.ShouldNotBeNull();
            savedToken.Token.ShouldBe(token);
            savedToken.ExpiresAt.ShouldBe(expiresAt);
        }

        [Fact]
        public async Task CreateAsync_WithPastExpiryDate_ShouldCreateInvalidToken()
        {
            // Arrange
            string token = "alreadyExpiredToken";
            DateTime expiresAt = _timeProvider.GetUtcNow().UtcDateTime.AddDays(-1);

            // Act
            var result = await _repository.CreateAsync(token, expiresAt);

            // Assert
            var savedToken = await DbContext.RefreshTokens.FindAsync(result.tokenId);
            savedToken.ShouldNotBeNull();
            savedToken.Token.ShouldBe(token);
            savedToken.ExpiresAt.ShouldBe(expiresAt);

            var validationResult = await _repository.TryValidateAsync(token);
            validationResult.isValid.ShouldBeFalse();
        }

        [Fact]
        public async Task CreateAsync_WithDuplicateTokenValue_ShouldCreateUniqueRecords()
        {
            // Arrange
            string token = "duplicateToken";
            DateTime expiresAt1 = _timeProvider.GetUtcNow().UtcDateTime.AddDays(10);
            DateTime expiresAt2 = _timeProvider.GetUtcNow().UtcDateTime.AddDays(20);

            // Act
            var result1 = await _repository.CreateAsync(token, expiresAt1);
            var result2 = await _repository.CreateAsync(token, expiresAt2);

            // Assert
            result1.tokenId.ShouldNotBe(result2.tokenId);

            var tokens = await DbContext.RefreshTokens.Where(rt => rt.Token == token).ToListAsync();

            tokens.Count.ShouldBe(2);
            tokens.ShouldContain(t => t.Id == result1.tokenId);
            tokens.ShouldContain(t => t.Id == result2.tokenId);
        }
    }


    public class TryValidateAsync(PostgresFixture fixture) : PostgresRefreshTokenRepositoryTests(fixture)
    {
        [Fact]
        public async Task TryValidateAsync_WithValidToken_ShouldReturnTrueAndTokenId()
        {
            // Arrange
            string token = "validToken";
            DateTime expiresAt = _timeProvider.GetUtcNow().UtcDateTime.AddDays(30);
            var created = await _repository.CreateAsync(token, expiresAt);

            // Act
            var result = await _repository.TryValidateAsync(token);

            // Assert
            result.isValid.ShouldBeTrue();
            result.tokenId.ShouldBe(created.tokenId);
        }

        [Fact]
        public async Task TryValidateAsync_WithNonExistentToken_ShouldReturnFalse()
        {
            // Arrange
            string token = "nonExistentToken";

            // Act
            var result = await _repository.TryValidateAsync(token);

            // Assert
            result.isValid.ShouldBeFalse();
            result.tokenId.ShouldBe(Guid.Empty);
        }

        [Fact]
        public async Task TryValidateAsync_WithExpiredToken_ShouldReturnFalse()
        {
            // Arrange
            string token = "expiredToken";
            DateTime expiresAt = _timeProvider.GetUtcNow().UtcDateTime.AddDays(5);
            await _repository.CreateAsync(token, expiresAt);

            _timeProvider.Advance(TimeSpan.FromDays(6));

            // Act
            var result = await _repository.TryValidateAsync(token);

            // Assert
            result.isValid.ShouldBeFalse();
            result.tokenId.ShouldBe(Guid.Empty);
        }

        [Fact]
        public async Task TryValidateAsync_WithRevokedToken_ShouldReturnFalse()
        {
            // Arrange
            string token = "revokedToken";
            DateTime expiresAt = _timeProvider.GetUtcNow().UtcDateTime.AddDays(30);
            var created= await _repository.CreateAsync(token, expiresAt);

            await _repository.RevokeAsync(created.tokenId);

            // Act
            var result = await _repository.TryValidateAsync(token);

            // Assert
            result.isValid.ShouldBeFalse();
            result.tokenId.ShouldBe(Guid.Empty);
        }

        [Fact]
        public async Task TryValidateAsync_WithTokenExpiringExactlyNow_ShouldReturnFalse()
        {
            // Arrange
            string token = "expiringToken";

            DateTime expiresAt = _timeProvider.GetUtcNow().UtcDateTime.AddMilliseconds(-1);
            await _repository.CreateAsync(token, expiresAt);

            // Act
            var result = await _repository.TryValidateAsync(token);

            // Assert
            result.isValid.ShouldBeFalse();
            result.tokenId.ShouldBe(Guid.Empty);
        }

        [Fact]
        public async Task TryValidateAsync_WithTokenExpiringJustAfterNow_ShouldReturnTrue()
        {
            // Arrange
            const string token = "barelyValidToken";
            DateTime expiresAt = _timeProvider.GetUtcNow().UtcDateTime.AddMilliseconds(1);
            var created = await _repository.CreateAsync(token, expiresAt);

            // Act
            var result = await _repository.TryValidateAsync(token);

            // Assert
            result.isValid.ShouldBeTrue();
            result.tokenId.ShouldBe(created.tokenId);
        }
    }


    public class RevokeAsync(PostgresFixture fixture) : PostgresRefreshTokenRepositoryTests(fixture)
    {
        [Fact]
        public async Task RevokeAsync_WithValidTokenId_ShouldReturnTrueAndRevokeToken()
        {
            // Arrange
            string token = "tokenToRevoke";
            DateTime expiresAt = _timeProvider.GetUtcNow().UtcDateTime.AddDays(30);
            var created = await _repository.CreateAsync(token, expiresAt);

            // Act
            bool result = await _repository.RevokeAsync(created.tokenId);

            // Assert
            result.ShouldBeTrue();

            var revokedToken = await DbContext.RefreshTokens.FindAsync(created.tokenId);
            revokedToken.ShouldNotBeNull();
            revokedToken.RevokedAt.ShouldNotBeNull();
            revokedToken.RevokedAt.Value.ShouldBe(_timeProvider.GetUtcNow().UtcDateTime);
        }

        [Fact]
        public async Task RevokeAsync_WithNonExistentTokenId_ShouldReturnFalse()
        {
            // Arrange
            Guid nonExistentId = Guid.NewGuid();

            // Act
            bool result = await _repository.RevokeAsync(nonExistentId);

            // Assert
            result.ShouldBeFalse();
        }

        [Fact]
        public async Task RevokeAsync_WithAlreadyRevokedToken_ShouldReturnTrueAndUpdateRevokedAt()
        {
            // Arrange
            string token = "alreadyRevokedToken";
            DateTime expiresAt = _timeProvider.GetUtcNow().UtcDateTime.AddDays(30);
            var created = await _repository.CreateAsync(token, expiresAt);

            var firstRevocationTime = _timeProvider.GetUtcNow().UtcDateTime;
            await _repository.RevokeAsync(created.tokenId);

            _timeProvider.Advance(TimeSpan.FromHours(1));

            // Act
            bool result = await _repository.RevokeAsync(created.tokenId);

            // Assert
            result.ShouldBeTrue();

            var revokedToken = await DbContext.RefreshTokens.FindAsync(created.tokenId);
            revokedToken.ShouldNotBeNull();
            revokedToken.RevokedAt.ShouldNotBeNull();

            revokedToken.RevokedAt.Value.ShouldBe(_timeProvider.GetUtcNow().UtcDateTime);
            revokedToken.RevokedAt.Value.ShouldNotBe(firstRevocationTime);
        }

        [Fact]
        public async Task RevokeAsync_WithExpiredToken_ShouldRevokeSuccessfully()
        {
            // Arrange
            string token = "expiredToken";
            DateTime expiresAt = _timeProvider.GetUtcNow().UtcDateTime.AddDays(1);
            var created = await _repository.CreateAsync(token, expiresAt);

            _timeProvider.Advance(TimeSpan.FromDays(2));

            // Act
            bool result = await _repository.RevokeAsync(created.tokenId);

            // Assert
            result.ShouldBeTrue();

            var revokedToken = await DbContext.RefreshTokens.FindAsync(created.tokenId);
            revokedToken.ShouldNotBeNull();
            revokedToken.RevokedAt.ShouldNotBeNull();
            revokedToken.RevokedAt.Value.ShouldBe(_timeProvider.GetUtcNow().UtcDateTime);

            var validationResult = await _repository.TryValidateAsync(token);
            validationResult.isValid.ShouldBeFalse();
        }
    }


    public class RotateAsync(PostgresFixture fixture) : PostgresRefreshTokenRepositoryTests(fixture)
    {
        [Fact]
        public async Task RotateAsync_WithNonExistentTokenId_ShouldThrowInvalidOperationException()
        {
            // Arrange
            Guid nonExistentId = Guid.NewGuid();

            // Act & Assert
            await Should.ThrowAsync<InvalidOperationException>(async () => await _repository.RotateAsync(nonExistentId));
        }
    }
}