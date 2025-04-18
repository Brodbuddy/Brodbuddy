using Core.Extensions;
using Infrastructure.Data.Postgres;
using Microsoft.EntityFrameworkCore;
using SharedTestDependencies;
using Shouldly;

namespace Infrastructure.Data.Tests;

[Collection(TestCollections.Database)]
public class MultiDeviceIdentityRepositoryTests : RepositoryTestBase
{
    private FakeTimeProvider _timeProvider;
    private PostgresMultiDeviceIdentityRepository _repository;

    public MultiDeviceIdentityRepositoryTests(PostgresFixture fixture) : base(fixture)
    {
        _timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        _repository = new PostgresMultiDeviceIdentityRepository(_dbContext, _timeProvider);
    }

    public class SaveIdentityAsync(PostgresFixture fixture) : MultiDeviceIdentityRepositoryTests(fixture)
    {
        [Fact]
        public async Task SaveIdentityAsync_WithValidIds_CreatesContextAndReturnsId()
        {
            // Arrange
            var user = await _dbContext.SeedUserAsync(_timeProvider);
            var device = await _dbContext.SeedDeviceAsync(_timeProvider);
            var refreshToken = await _dbContext.SeedRefreshTokenAsync(_timeProvider);
            var expectedTime = _timeProvider.Now();

            // Act
            var id = await _repository.SaveIdentityAsync(user.Id, device.Id, refreshToken.Id);

            // Assert
            id.ShouldNotBe(Guid.Empty);

            var savedContext = await _dbContext.TokenContexts.FindAsync(id);
            
            savedContext.ShouldNotBeNull();
            savedContext.Id.ShouldBe(id);
            savedContext.UserId.ShouldBe(user.Id);
            savedContext.DeviceId.ShouldBe(device.Id);
            savedContext.RefreshTokenId.ShouldBe(refreshToken.Id);
            savedContext.IsRevoked.ShouldBeFalse(); 
            savedContext.CreatedAt.ShouldBeWithinTolerance(expectedTime); 
        }
        
        [Fact]
        public async Task SaveIdentityAsync_WithNonExistingUserId_ThrowsDbUpdateException()
        {
            // Arrange
            var nonExistingUserId = Guid.NewGuid();
            var device = await _dbContext.SeedDeviceAsync(_timeProvider); 
            var refreshToken = await _dbContext.SeedRefreshTokenAsync(_timeProvider); 

            // Act & Assert
            // DbUpdateException er den typiske EF Core exception for FK violations under SaveChanges.
            await Should.ThrowAsync<DbUpdateException>(() => _repository.SaveIdentityAsync(nonExistingUserId, device.Id, refreshToken.Id));
        }
        
        [Fact]
        public async Task SaveIdentityAsync_WithNonExistingDeviceId_ThrowsDbUpdateException()
        {
            // Arrange
            var user = await _dbContext.SeedUserAsync(_timeProvider); 
            var nonExistingDeviceId = Guid.NewGuid();
            var refreshToken = await _dbContext.SeedRefreshTokenAsync(_timeProvider);
            
            // Act & Assert
            await Should.ThrowAsync<DbUpdateException>(() => _repository.SaveIdentityAsync(user.Id, nonExistingDeviceId, refreshToken.Id));
        }
        
        [Fact]
        public async Task SaveIdentityAsync_WithNonExistingRefreshTokenId_ThrowsDbUpdateException()
        {
            // Arrange
            var user = await _dbContext.SeedUserAsync(_timeProvider);
            var device = await _dbContext.SeedDeviceAsync(_timeProvider);
            var nonExistingRefreshTokenId = Guid.NewGuid();

            // Act & Assert
            await Should.ThrowAsync<DbUpdateException>(() => _repository.SaveIdentityAsync(user.Id, device.Id, nonExistingRefreshTokenId));
        }
        
        [Fact]
        public async Task SaveIdentityAsync_WithExistingRefreshTokenId_ThrowsDbUpdateException()
        {
            // Arrange
            var sharedRefreshToken = await _dbContext.SeedRefreshTokenAsync(_timeProvider);
            
            var user1 = await _dbContext.SeedUserAsync(_timeProvider, "user1@test.com");
            var device1 = await _dbContext.SeedDeviceAsync(_timeProvider, "linux", "chrome");
            
            var user2 = await _dbContext.SeedUserAsync(_timeProvider, "user2@test.com");
            var device2 = await _dbContext.SeedDeviceAsync(_timeProvider, "macos", "firefox");

            // Første gem - associer sharedRefreshToken med user1/device1 - burde være 10-4
            await _repository.SaveIdentityAsync(user1.Id, device1.Id, sharedRefreshToken.Id);

            // Act & Assert - Prøv at associer den SAMME sharedRefreshToken med user2/device2 - burde være øv bøv
            await Should.ThrowAsync<DbUpdateException>(() => _repository.SaveIdentityAsync(user2.Id, device2.Id, sharedRefreshToken.Id));
        }
    }

    public class RevokeTokenContextAsync(PostgresFixture fixture) : MultiDeviceIdentityRepositoryTests(fixture)
    {
        [Fact]
        public async Task RevokeTokenContextAsync_WithExistingActiveContext_ReturnsTrueAndRevokesContext()
        {
            // Arrange
            var tokenContext = await _dbContext.SeedTokenContextAsync(_timeProvider, isRevoked: false);
            var refreshTokenIdToRevoke = tokenContext.RefreshTokenId;
            var contextId = tokenContext.Id;

            // Pre-assert
            var initialContext = await _dbContext.TokenContexts.FindAsync(contextId);
            initialContext.ShouldNotBeNull();
            initialContext.IsRevoked.ShouldBeFalse();
            _dbContext.ChangeTracker.Clear();

            // Act
            var result = await _repository.RevokeTokenContextAsync(refreshTokenIdToRevoke);

            // Assert
            result.ShouldBeTrue();

            var updatedContext = await _dbContext.TokenContexts.FindAsync(contextId);
            updatedContext.ShouldNotBeNull();
            updatedContext.IsRevoked.ShouldBeTrue(); 
        }
        
        [Fact]
        public async Task RevokeTokenContextAsync_WithNonExistingRefreshTokenId_ReturnsFalse()
        {
            // Arrange
            var nonExistingRefreshTokenId = Guid.NewGuid();
            await _dbContext.SeedTokenContextAsync(_timeProvider); 

            // Act
            var result = await _repository.RevokeTokenContextAsync(nonExistingRefreshTokenId);

            // Assert
            result.ShouldBeFalse();
        }
        
        [Fact]
        public async Task RevokeTokenContextAsync_WithAlreadyRevokedContext_ReturnsTrueAndContextRemainsRevoked()
        {
            // Arrange
            var tokenContext = await _dbContext.SeedTokenContextAsync(_timeProvider, isRevoked: true);
            var refreshTokenIdToRevoke = tokenContext.RefreshTokenId;
            var contextId = tokenContext.Id;

            // Pre-assert
            var initialContext = await _dbContext.TokenContexts.FindAsync(contextId);
            initialContext.ShouldNotBeNull();
            initialContext.IsRevoked.ShouldBeTrue();

            // Act
            var result = await _repository.RevokeTokenContextAsync(refreshTokenIdToRevoke);
            _dbContext.ChangeTracker.Clear();

            // Assert
            result.ShouldBeTrue(); 

            var updatedContext = await _dbContext.TokenContexts.FindAsync(contextId);
            updatedContext.ShouldNotBeNull();
            updatedContext.IsRevoked.ShouldBeTrue();
        }
    }

    public class GetAsync(PostgresFixture fixture) : MultiDeviceIdentityRepositoryTests(fixture)
    {
        [Fact]
        public async Task GetAsync_WithExistingActiveTokenId_ReturnsContextWithIncludes()
        {
            // Arrange
            var seededContext = await _dbContext.SeedTokenContextAsync(_timeProvider, isRevoked: false);
            var refreshTokenIdToFind = seededContext.RefreshTokenId;

            // Act
            var result = await _repository.GetAsync(refreshTokenIdToFind);

            // Assert
            result.ShouldNotBeNull();
            result.Id.ShouldBe(seededContext.Id);
            result.RefreshTokenId.ShouldBe(refreshTokenIdToFind);
            result.IsRevoked.ShouldBeFalse();
            
            // Verificer den har fået navigational properties med
            result.User.ShouldNotBeNull();
            result.User.Id.ShouldBe(seededContext.UserId);
            result.Device.ShouldNotBeNull();
            result.Device.Id.ShouldBe(seededContext.DeviceId);
            result.RefreshToken.ShouldNotBeNull();
            result.RefreshToken.Id.ShouldBe(seededContext.RefreshTokenId);
        }
        
        [Fact]
        public async Task GetAsync_WithRevokedTokenId_ReturnsNull()
        {
            // Arrange
            var seededContext = await _dbContext.SeedTokenContextAsync(_timeProvider, isRevoked: true);
            var revokedRefreshTokenId = seededContext.RefreshTokenId;

            // Act
            var result = await _repository.GetAsync(revokedRefreshTokenId);

            // Assert
            result.ShouldBeNull();
        }

        [Fact]
        public async Task GetAsync_WithNonExistingTokenId_ReturnsNull()
        {
            // Arrange
            var nonExistingRefreshTokenId = Guid.NewGuid();
            await _dbContext.SeedTokenContextAsync(_timeProvider);

            // Act
            var result = await _repository.GetAsync(nonExistingRefreshTokenId);

            // Assert
            result.ShouldBeNull();
        }
        
        [Fact]
        public async Task GetAsync_WithEmptyTokenId_ReturnsNull()
        {
            // Arrange
            var emptyRefreshTokenId = Guid.Empty;
            await _dbContext.SeedTokenContextAsync(_timeProvider); 

            // Act
            var result = await _repository.GetAsync(emptyRefreshTokenId);

            // Assert
            result.ShouldBeNull();
        }
    }
}