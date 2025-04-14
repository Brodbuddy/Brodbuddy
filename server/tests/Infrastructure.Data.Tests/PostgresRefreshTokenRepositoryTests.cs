using Infrastructure.Data.Postgres;
using Microsoft.EntityFrameworkCore;
using SharedTestDependencies;
using Shouldly;

namespace Infrastructure.Data.Tests;

public class PostgresRefreshTokenRepositoryTests
{
    private readonly PostgresDbContext _dbContext;
    private readonly FakeTimeProvider _timeProvider;
    private readonly PostgresRefreshTokenRepository _repository;

    public PostgresRefreshTokenRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<PostgresDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new PostgresDbContext(options);
        _timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        _repository = new PostgresRefreshTokenRepository(_dbContext, _timeProvider);
    }


    public class CreateAsync : PostgresRefreshTokenRepositoryTests
    {
        [Fact]
        public async Task CreateAsync_ShouldCreateTokenInDatabase()
        {
            // Arrange
            string token = "testToken";
            DateTime expiresAt = _timeProvider.GetUtcNow().UtcDateTime.AddDays(30);

            // Act
            Guid id = await _repository.CreateAsync(token, expiresAt);

            // Assert
            var savedToken = await _dbContext.RefreshTokens.FindAsync(id);
            savedToken.ShouldNotBeNull();
            savedToken.Token.ShouldBe(token);
            savedToken.CreatedAt.ShouldBe(_timeProvider.GetUtcNow().UtcDateTime);
            savedToken.ExpiresAt.ShouldBe(expiresAt);
            savedToken.RevokedAt.ShouldBeNull();
            savedToken.ReplacedByTokenId.ShouldBeNull();
        }
    }


    public class TryValidateAsync : PostgresRefreshTokenRepositoryTests
    {
        [Fact]
        public async Task TryValidateAsync_WithValidToken_ShouldReturnTrueAndTokenId()
        {
            // Arrange
            string token = "validToken";
            DateTime expiresAt = _timeProvider.GetUtcNow().UtcDateTime.AddDays(30);
            Guid id = await _repository.CreateAsync(token, expiresAt);

            // Act
            var result = await _repository.TryValidateAsync(token);

            // Assert
            result.isValid.ShouldBeTrue();
            result.tokenId.ShouldBe(id);
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
            Guid id = await _repository.CreateAsync(token, expiresAt);

            await _repository.RevokeAsync(id);

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
            Guid id = await _repository.CreateAsync(token, expiresAt);

            // Act
            var result = await _repository.TryValidateAsync(token);

            // Assert
            result.isValid.ShouldBeTrue();
            result.tokenId.ShouldBe(id);
        }
    }


    public class RevokeAsync : PostgresRefreshTokenRepositoryTests
    {
        [Fact]
        public async Task RevokeAsync_WithValidTokenId_ShouldReturnTrueAndRevokeToken()
        {
            // Arrange
            string token = "tokenToRevoke";
            DateTime expiresAt = _timeProvider.GetUtcNow().UtcDateTime.AddDays(30);
            Guid id = await _repository.CreateAsync(token, expiresAt);

            // Act
            bool result = await _repository.RevokeAsync(id);

            // Assert
            result.ShouldBeTrue();

            var revokedToken = await _dbContext.RefreshTokens.FindAsync(id);
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
        public async Task RevokeAsync_ShouldSaveChangesToDatabase()
        {
            // Arrange
            string token = "tokenToRevoke";
            DateTime expiresAt = _timeProvider.GetUtcNow().UtcDateTime.AddDays(30);
            Guid id = await _repository.CreateAsync(token, expiresAt);

            // Act
            bool result = await _repository.RevokeAsync(id);

            _dbContext.ChangeTracker.Clear();

            // Assert
            result.ShouldBeTrue();

            var revokedToken = await _dbContext.RefreshTokens.FindAsync(id);
            revokedToken.ShouldNotBeNull();
            revokedToken.RevokedAt.ShouldNotBeNull();
            revokedToken.RevokedAt.Value.ShouldBe(_timeProvider.GetUtcNow().UtcDateTime);
        }
    }


    public class RotateAsync : PostgresRefreshTokenRepositoryTests
    {
        [Fact]
        public async Task RotateAsync_WithValidTokenId_ShouldReturnNewTokenAndUpdateOldToken()
        {
            // Arrange
            string token = "tokenToRotate";
            DateTime expiresAt = _timeProvider.GetUtcNow().UtcDateTime.AddDays(30);
            Guid oldTokenId = await _repository.CreateAsync(token, expiresAt);

            // Act
            string newToken = await _repository.RotateAsync(oldTokenId);

            // Assert
            newToken.ShouldNotBeNullOrEmpty();
            newToken.ShouldNotBe(token);

            var oldTokenEntity = await _dbContext.RefreshTokens.FindAsync(oldTokenId);
            oldTokenEntity.ShouldNotBeNull();
            oldTokenEntity.RevokedAt.ShouldNotBeNull();
            oldTokenEntity.RevokedAt.Value.ShouldBe(_timeProvider.GetUtcNow().UtcDateTime);
            oldTokenEntity.ReplacedByTokenId.ShouldNotBeNull();


            var newTokenEntity = await _dbContext.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == newToken);
            newTokenEntity.ShouldNotBeNull();
            newTokenEntity.CreatedAt.ShouldBe(_timeProvider.GetUtcNow().UtcDateTime);
            newTokenEntity.ExpiresAt.ShouldBe(_timeProvider.GetUtcNow().UtcDateTime.AddDays(30));
            newTokenEntity.RevokedAt.ShouldBeNull();


            oldTokenEntity.ReplacedByTokenId.ShouldBe(newTokenEntity.Id);
        }


        [Fact]
        public async Task RotateAsync_WithNonExistentTokenId_ShouldThrowInvalidOperationException()
        {
            // Arrange
            Guid nonExistentId = Guid.NewGuid();

            // Act & Assert
            var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
                await _repository.RotateAsync(nonExistentId));

            exception.Message.ShouldBe("Old token not found");
        }

        [Fact]
        public async Task RotateAsync_ShouldSaveChangesToDatabase()
        {
            // Arrange
            string token = "tokenToRotate";
            DateTime expiresAt = _timeProvider.GetUtcNow().UtcDateTime.AddDays(30);
            Guid oldTokenId = await _repository.CreateAsync(token, expiresAt);

            // Act
            string newToken = await _repository.RotateAsync(oldTokenId);


            _dbContext.ChangeTracker.Clear();

            // Assert 
            var oldTokenEntity = await _dbContext.RefreshTokens.FindAsync(oldTokenId);
            oldTokenEntity.ShouldNotBeNull();
            oldTokenEntity.RevokedAt.ShouldNotBeNull();
            oldTokenEntity.ReplacedByTokenId.ShouldNotBeNull();

            var newTokenEntity = await _dbContext.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == newToken);
            newTokenEntity.ShouldNotBeNull();

            oldTokenEntity.ReplacedByTokenId.ShouldBe(newTokenEntity.Id);
        }
    }
}