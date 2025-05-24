using Core.Extensions;
using Infrastructure.Data.Repositories;
using Infrastructure.Data.Repositories.Auth;
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
public class UserIdentityRepositoryTests : RepositoryTestBase
{
    private readonly FakeTimeProvider _timeProvider;
    private readonly PgUserIdentityRepository _repository;

    private UserIdentityRepositoryTests(PostgresFixture fixture) : base(fixture)
    {
        _timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        _repository = new PgUserIdentityRepository(DbContext, _timeProvider);
    }

    public class SaveAsync(PostgresFixture fixture) : UserIdentityRepositoryTests(fixture)
    {
        [Fact]
        public async Task SaveAsync_WithValidEmail_SavesUserAndReturnsId()
        {
            // Arrange
            var email = "Test@email.com";
            var expectedCreationTime = _timeProvider.Now();

            // Act
            Guid userId = await _repository.SaveAsync(email);

            //Assert
            var savedUser = await DbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            savedUser.ShouldNotBeNull();
            savedUser.Email.ShouldBe(email);
            savedUser.CreatedAt.ShouldBeWithinTolerance(expectedCreationTime);
        }
    }

    public class ExistsAsync(PostgresFixture fixture) : UserIdentityRepositoryTests(fixture)
    {
        [Fact]
        public async Task ExistsAsync_WithExistingUserId_ReturnsTrue()
        {
            // Arrange
            var seededUser = await DbContext.SeedUserAsync(_timeProvider);

            // Act
            var exists = await _repository.ExistsAsync(seededUser.Id);

            // Assert
            exists.ShouldBeTrue();
        }

        [Fact]
        public async Task ExistsAsync_WithNonExistingUserId_ReturnsFalse()
        {
            // Arrange
            var nonExistingId = Guid.NewGuid();

            // Act
            var exists = await _repository.ExistsAsync(nonExistingId);

            // Assert
            exists.ShouldBeFalse();
        }

        [Fact]
        public async Task ExistsAsync_WithExistingEmail_ReturnsTrue()
        {
            // Arrange
            var seededUser = await DbContext.SeedUserAsync(_timeProvider);

            // Act
            var exists = await _repository.ExistsAsync(seededUser.Email);

            // Assert
            exists.ShouldBeTrue();
        }

        [Fact]
        public async Task ExistsAsync_WithNonExistingEmail_ReturnsFalse()
        {
            // Arrange
            const string nonExistingEmail = "nonexisting@email.com";

            // Act
            var exists = await _repository.ExistsAsync(nonExistingEmail);

            // Assert
            exists.ShouldBeFalse();
        }
    }

    public class GetAsync(PostgresFixture fixture) : UserIdentityRepositoryTests(fixture)
    {
        [Fact]
        public async Task GetAsync_WithExistingUserId_ReturnsUser()
        {
            // Arrange
            var seededUser = await DbContext.SeedUserAsync(_timeProvider);

            // Act
            var user = await _repository.GetAsync(seededUser.Id); 

            // Assert
            user.ShouldNotBeNull();
            user.Id.ShouldBe(seededUser.Id);
            user.Email.ShouldBe(seededUser.Email); 
        }

        [Fact]
        public async Task GetAsync_WithNonExistingUserId_ReturnsNull()
        {
            // Arrange
            var nonExistingId = Guid.NewGuid();

            // Act
            var result = await _repository.GetAsync(nonExistingId);

            // Assert
            result.ShouldBeNull();
        }

        [Fact]
        public async Task GetAsync_WithExistingEmail_ReturnsUser()
        {
            // Arrange
            var seededUser = await DbContext.SeedUserAsync(_timeProvider);

            // Act
            var user = await _repository.GetAsync(seededUser.Email);

            // Assert
            user.ShouldNotBeNull();
            user.Id.ShouldBe(seededUser.Id);
            user.Email.ShouldBe(seededUser.Email);
        }

        [Fact]
        public async Task GetAsync_WithNonExistingEmail_ReturnsNull()
        {
            // Arrange
            const string nonExistingEmail = "nonexisting@email.com";

            // Act 
            var result = await _repository.GetAsync(nonExistingEmail);

            // Assert
            result.ShouldBeNull();
        }

        [Fact]
        public async Task GetAsync_WithDifferentCaseEmail_ReturnsCorrectUser()
        {
            // Arrange
            const string email = "Test@Email.com";
            var seededUser = await DbContext.SeedUserAsync(_timeProvider, email);

            // Act
            var user = await _repository.GetAsync("test@email.com");

            // Assert
            user.ShouldNotBeNull();
            user.Id.ShouldBe(seededUser.Id);
            user.Email.ShouldBe(email);
        }
    }
}