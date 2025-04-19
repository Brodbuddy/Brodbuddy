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
public class RefreshTokenRepositoryTests : RepositoryTestBase
{
    private readonly FakeTimeProvider _timeProvider;
    private readonly PgRefreshTokenRepository _repository;

    private RefreshTokenRepositoryTests(PostgresFixture fixture) : base(fixture)
    {
        _timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        _repository = new PgRefreshTokenRepository(DbContext, _timeProvider);
    }


    public class CreateAsync(PostgresFixture fixture) : RefreshTokenRepositoryTests(fixture)
    {
        [Fact]
        public async Task CreateAsync_WithValidToken_ShouldCreateToken()
        {
            // Arrange
            string token = "testToken";
            DateTime expiresAt = _timeProvider.Tomorrow();
            var expectedTime = _timeProvider.Now();

            // Act
            var result = await _repository.CreateAsync(token, expiresAt);

            // Assert
            var savedToken = await DbContext.RefreshTokens.FindAsync(result.tokenId);
            savedToken.ShouldNotBeNull();
            savedToken.Id.ShouldBe(result.tokenId);
            savedToken.Token.ShouldBe(token);
            savedToken.ExpiresAt.ShouldBe(expiresAt);
            savedToken.CreatedAt.ShouldBeWithinTolerance(expectedTime);
            savedToken.RevokedAt.ShouldBeNull();
            savedToken.ReplacedByTokenId.ShouldBeNull();
        }

        [Fact]
        public async Task CreateAsync_WithPastExpiryDate_ShouldCreateExpiredToken()
        {
            // Arrange
            string token = "alreadyExpiredToken";
            DateTime expiresAt = _timeProvider.Yesterday();

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
            DateTime expiresAt1 = _timeProvider.Now().AddDays(10);
            DateTime expiresAt2 = _timeProvider.Now().AddDays(20);

            // Act
            var result1 = await _repository.CreateAsync(token, expiresAt1);
            var result2 = await _repository.CreateAsync(token, expiresAt2);

            // Assert
            result1.tokenId.ShouldNotBe(result2.tokenId);

            var tokens = await DbContext.RefreshTokens.Where(rt => rt.Token == token).ToListAsync();
            tokens.Count.ShouldBe(2);
        }


        [Fact]
        public async Task CreateAsync_WithInvalidTokenString_ShouldThrowDbUpdateException()
        {
            // Arrange
            DateTime expiresAt = _timeProvider.Tomorrow();

            // Act & Assert
            await Should.ThrowAsync<DbUpdateException>(() => _repository.CreateAsync(null!, expiresAt));
        }
    }


    public class TryValidateAsync(PostgresFixture fixture) : RefreshTokenRepositoryTests(fixture)
    {
        [Fact]
        public async Task TryValidateAsync_WithValidToken_ShouldReturnTrueAndTokenId()
        {
            // Arrange
            var created = await DbContext.SeedRefreshTokenAsync(_timeProvider);

            // Act
            var result = await _repository.TryValidateAsync(created.Token);

            // Assert
            result.isValid.ShouldBeTrue();
            result.tokenId.ShouldBe(created.Id);
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
            var created = await DbContext.SeedRefreshTokenAsync(_timeProvider, expiresDays: 5);
            _timeProvider.Advance(TimeSpan.FromDays(6));

            // Act
            var result = await _repository.TryValidateAsync(created.Token);

            // Assert
            result.isValid.ShouldBeFalse();
            result.tokenId.ShouldBe(Guid.Empty);
        }

        [Fact]
        public async Task TryValidateAsync_WithRevokedToken_ShouldReturnFalse()
        {
            // Arrange
            var created = await DbContext.SeedRefreshTokenAsync(_timeProvider);

            bool revoked = await _repository.RevokeAsync(created.Id);
            revoked.ShouldBeTrue();
            DbContext.ChangeTracker.Clear();

            // Act
            var result = await _repository.TryValidateAsync(created.Token);

            // Assert
            result.isValid.ShouldBeFalse();
            result.tokenId.ShouldBe(Guid.Empty);
        }

        [Fact]
        public async Task TryValidateAsync_WithExpiryClearlyInThePast_ShouldReturnFalse()
        {
            // Arrange
            const int expiryDays = 30;
            var created = await DbContext.SeedRefreshTokenAsync(_timeProvider, expiresDays: expiryDays);

            _timeProvider.Advance(TimeSpan.FromDays(expiryDays).Add(TimeSpan.FromSeconds(1)));

            // Act
            var result = await _repository.TryValidateAsync(created.Token);

            // Assert
            result.isValid.ShouldBeFalse();
            result.tokenId.ShouldBe(Guid.Empty);
        }

        [Fact]
        public async Task TryValidateAsync_WithExpiryClearlyInTheFuture_ShouldReturnTrue()
        {
            // Arrange
            const int expiryDays = 30;
            var created = await DbContext.SeedRefreshTokenAsync(_timeProvider, expiresDays: expiryDays);

            _timeProvider.Advance(TimeSpan.FromDays(expiryDays).Subtract(TimeSpan.FromSeconds(1)));

            // Act
            var result = await _repository.TryValidateAsync(created.Token);

            // Assert
            result.isValid.ShouldBeTrue();
            result.tokenId.ShouldBe(created.Id);
        }
    }


    public class RevokeAsync(PostgresFixture fixture) : RefreshTokenRepositoryTests(fixture)
    {
        [Fact]
        public async Task RevokeAsync_WithValidTokenId_ShouldReturnTrueAndRevokeToken()
        {
            // Arrange
            var createdToken = await DbContext.SeedRefreshTokenAsync(_timeProvider);
            var expectedRevocationTime = _timeProvider.Now();

            // Pre-assert
            createdToken.RevokedAt.ShouldBeNull();

            // Act
            bool result = await _repository.RevokeAsync(createdToken.Id);

            // Assert
            result.ShouldBeTrue();

            var revokedToken = await DbContext.RefreshTokens
                .AsNoTracking()
                .FirstOrDefaultAsync(rt => rt.Id == createdToken.Id);

            revokedToken.ShouldNotBeNull();
            revokedToken.RevokedAt.ShouldNotBeNull();
            revokedToken.RevokedAt.Value.ShouldBeWithinTolerance(expectedRevocationTime);
        }

        [Fact]
        public async Task RevokeAsync_WithNonExistentTokenId_ShouldReturnFalse()
        {
            // Arrange
            Guid nonExistentId = Guid.NewGuid();
            await DbContext.SeedRefreshTokenAsync(_timeProvider);

            // Act
            bool result = await _repository.RevokeAsync(nonExistentId);

            // Assert
            result.ShouldBeFalse();
        }

        [Fact]
        public async Task RevokeAsync_WithAlreadyRevokedToken_ShouldReturnFalse()
        {
            // Arrange
            var revokedTime = _timeProvider.Yesterday();
            var createdToken = await DbContext.SeedRefreshTokenAsync(_timeProvider, revokedAt: revokedTime);

            // Pre-assert 
            createdToken.RevokedAt.ShouldBe(revokedTime);
            _timeProvider.Advance(TimeSpan.FromMinutes(1));

            // Act
            bool result = await _repository.RevokeAsync(createdToken.Id);

            // Assert
            result.ShouldBeFalse();

            var recheckedToken = await DbContext.RefreshTokens
                .AsNoTracking()
                .FirstOrDefaultAsync(rt => rt.Id == createdToken.Id);

            recheckedToken.ShouldNotBeNull();
            recheckedToken.RevokedAt.ShouldNotBeNull();
            recheckedToken.RevokedAt.Value.ShouldBeWithinTolerance(revokedTime);
        }

        [Fact]
        public async Task RevokeAsync_WithExpiredToken_ShouldRevokeSuccessfully()
        {
            // Arrange
            var created = await DbContext.SeedRefreshTokenAsync(_timeProvider, expiresDays: -1);
            var expectedRevocationTime = _timeProvider.Now();

            // Act
            bool result = await _repository.RevokeAsync(created.Id);

            // Assert
            result.ShouldBeTrue();

            var revokedToken = await DbContext.RefreshTokens
                .AsNoTracking()
                .FirstOrDefaultAsync(rt => rt.Id == created.Id);

            revokedToken.ShouldNotBeNull();
            revokedToken.RevokedAt.ShouldNotBeNull();
            revokedToken.RevokedAt.Value.ShouldBeWithinTolerance(expectedRevocationTime);
        }

        [Fact]
        public async Task RevokeAsync_WithEmptyId_ShouldReturnFalse()
        {
            // Arrange
            var emptyId = Guid.Empty;
            await DbContext.SeedRefreshTokenAsync(_timeProvider);

            // Act
            bool result = await _repository.RevokeAsync(emptyId);

            // Assert
            result.ShouldBeFalse();
        }
    }


    public class RotateAsync(PostgresFixture fixture) : RefreshTokenRepositoryTests(fixture)
    {
        [Fact]
        public async Task RotateAsync_WithExistingTokenId_ShouldCreateNewTokenAndRevokeOldOne()
        {
            // Arrange
            var oldToken = await DbContext.SeedRefreshTokenAsync(_timeProvider);
            var timeBeforeAct = _timeProvider.Now();

            // Act
            var (newTokenString, newTokenId) = await _repository.RotateAsync(oldToken.Id);

            // Assert
            newTokenString.ShouldNotBeNullOrWhiteSpace();
            newTokenString.ShouldNotBe(oldToken.Token);
            newTokenId.ShouldNotBe(Guid.Empty);
            newTokenId.ShouldNotBe(oldToken.Id);

            // Verificer ny token
            DbContext.ChangeTracker.Clear();
            var newTokenEntity = await DbContext.RefreshTokens.FindAsync(newTokenId);
            newTokenEntity.ShouldNotBeNull();
            newTokenEntity.Token.ShouldBe(newTokenString);
            newTokenEntity.CreatedAt.ShouldBeWithinTolerance(timeBeforeAct);
            newTokenEntity.ExpiresAt.ShouldBeGreaterThan(timeBeforeAct);
            newTokenEntity.RevokedAt.ShouldBeNull();
            newTokenEntity.ReplacedByTokenId.ShouldBeNull();

            // Verificer gammel token
            var oldTokenEntity = await DbContext.RefreshTokens.FindAsync(oldToken.Id);
            oldTokenEntity.ShouldNotBeNull();
            oldTokenEntity.RevokedAt.ShouldNotBeNull();
            oldTokenEntity.RevokedAt.Value.ShouldBeWithinTolerance(timeBeforeAct);
            oldTokenEntity.ReplacedByTokenId.ShouldBe(newTokenId);
        }

        [Fact]
        public async Task RotateAsync_WithNonExistentTokenId_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act & Assert
            await Should.ThrowAsync<InvalidOperationException>(async () => await _repository.RotateAsync(nonExistentId));
        }
        
        [Fact]
        public async Task RotateAsync_WithEmptyTokenId_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var emptyId = Guid.Empty;
            await DbContext.SeedRefreshTokenAsync(_timeProvider);

            // Act & Assert
            await Should.ThrowAsync<InvalidOperationException>(() => _repository.RotateAsync(emptyId));
        }
    }
}