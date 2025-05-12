using Core.Extensions;
using Infrastructure.Data.Repositories;
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
public class MultiDeviceIdentityRepositoryTests : RepositoryTestBase
{
    private readonly FakeTimeProvider _timeProvider;
    private readonly PgMultiDeviceIdentityRepository _repository;

    private MultiDeviceIdentityRepositoryTests(PostgresFixture fixture) : base(fixture)
    {
        _timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        _repository = new PgMultiDeviceIdentityRepository(DbContext, _timeProvider);
    }

    public class SaveIdentityAsync(PostgresFixture fixture) : MultiDeviceIdentityRepositoryTests(fixture)
    {
        [Fact]
        public async Task SaveIdentityAsync_WithValidIds_CreatesContextAndReturnsId()
        {
            // Arrange
            var user = await DbContext.SeedUserAsync(_timeProvider);
            var device = await DbContext.SeedDeviceAsync(_timeProvider);
            var refreshToken = await DbContext.SeedRefreshTokenAsync(_timeProvider);
            var expectedTime = _timeProvider.Now();

            // Act
            var id = await _repository.SaveIdentityAsync(user.Id, device.Id, refreshToken.Id);

            // Assert
            id.ShouldNotBe(Guid.Empty);

            var savedContext = await DbContext.TokenContexts.AsNoTracking().FirstOrDefaultAsync(tc => tc.Id == id);
            
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
            var device = await DbContext.SeedDeviceAsync(_timeProvider); 
            var refreshToken = await DbContext.SeedRefreshTokenAsync(_timeProvider); 

            // Act & Assert
            // DbUpdateException er den typiske EF Core exception for FK violations under SaveChanges.
            await Should.ThrowAsync<DbUpdateException>(() => _repository.SaveIdentityAsync(nonExistingUserId, device.Id, refreshToken.Id));
        }
        
        [Fact]
        public async Task SaveIdentityAsync_WithNonExistingDeviceId_ThrowsDbUpdateException()
        {
            // Arrange
            var user = await DbContext.SeedUserAsync(_timeProvider); 
            var nonExistingDeviceId = Guid.NewGuid();
            var refreshToken = await DbContext.SeedRefreshTokenAsync(_timeProvider);
            
            // Act & Assert
            await Should.ThrowAsync<DbUpdateException>(() => _repository.SaveIdentityAsync(user.Id, nonExistingDeviceId, refreshToken.Id));
        }
        
        [Fact]
        public async Task SaveIdentityAsync_WithNonExistingRefreshTokenId_ThrowsDbUpdateException()
        {
            // Arrange
            var user = await DbContext.SeedUserAsync(_timeProvider);
            var device = await DbContext.SeedDeviceAsync(_timeProvider);
            var nonExistingRefreshTokenId = Guid.NewGuid();

            // Act & Assert
            await Should.ThrowAsync<DbUpdateException>(() => _repository.SaveIdentityAsync(user.Id, device.Id, nonExistingRefreshTokenId));
        }
        
        [Fact]
        public async Task SaveIdentityAsync_WithExistingRefreshTokenId_ThrowsDbUpdateException()
        {
            // Arrange
            var sharedRefreshToken = await DbContext.SeedRefreshTokenAsync(_timeProvider);
            
            var user1 = await DbContext.SeedUserAsync(_timeProvider, "user1@test.com");
            var device1 = await DbContext.SeedDeviceAsync(_timeProvider, "linux", "chrome");
            
            var user2 = await DbContext.SeedUserAsync(_timeProvider, "user2@test.com");
            var device2 = await DbContext.SeedDeviceAsync(_timeProvider, "macos", "safari");

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
            var tokenContext = await DbContext.SeedTokenContextAsync(_timeProvider, isRevoked: false);
            var refreshTokenIdToRevoke = tokenContext.RefreshTokenId;

            // Pre-assert
            var initialContext = await DbContext.TokenContexts.FindAsync(tokenContext.Id);
            initialContext.ShouldNotBeNull();
            initialContext.IsRevoked.ShouldBeFalse();

            // Act
            var result = await _repository.RevokeTokenContextAsync(refreshTokenIdToRevoke);

            // Assert
            result.ShouldBeTrue();

            var updatedContext = await DbContext.TokenContexts.AsNoTracking().FirstOrDefaultAsync(tc => tc.Id == tokenContext.Id);
            updatedContext.ShouldNotBeNull();
            updatedContext.IsRevoked.ShouldBeTrue(); 
        }
        
        [Fact]
        public async Task RevokeTokenContextAsync_WithNonExistingRefreshTokenId_ReturnsFalse()
        {
            // Arrange
            var nonExistingRefreshTokenId = Guid.NewGuid();
            await DbContext.SeedTokenContextAsync(_timeProvider); 

            // Act
            var result = await _repository.RevokeTokenContextAsync(nonExistingRefreshTokenId);

            // Assert
            result.ShouldBeFalse();
        }
        
        [Fact]
        public async Task RevokeTokenContextAsync_WithAlreadyRevokedContext_ReturnsTrueAndContextRemainsRevoked()
        {
            // Arrange
            var tokenContext = await DbContext.SeedTokenContextAsync(_timeProvider, isRevoked: true);
            var refreshTokenIdToRevoke = tokenContext.RefreshTokenId;

            // Pre-assert
            var initialContext = await DbContext.TokenContexts.FindAsync(tokenContext.Id);
            initialContext.ShouldNotBeNull();
            initialContext.IsRevoked.ShouldBeTrue();

            // Act
            var result = await _repository.RevokeTokenContextAsync(refreshTokenIdToRevoke);

            // Assert
            result.ShouldBeTrue(); 

            var updatedContext = await DbContext.TokenContexts.AsNoTracking().FirstOrDefaultAsync(tc => tc.Id == tokenContext.Id);
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
            var seededContext = await DbContext.SeedTokenContextAsync(_timeProvider, isRevoked: false);
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
            var seededContext = await DbContext.SeedTokenContextAsync(_timeProvider, isRevoked: true);
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
            await DbContext.SeedTokenContextAsync(_timeProvider);

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
            await DbContext.SeedTokenContextAsync(_timeProvider); 

            // Act
            var result = await _repository.GetAsync(emptyRefreshTokenId);

            // Assert
            result.ShouldBeNull();
        }
    }
}