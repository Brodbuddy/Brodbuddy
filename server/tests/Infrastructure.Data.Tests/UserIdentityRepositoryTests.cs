using Infrastructure.Data.Postgres;
using SharedTestDependencies;
using Shouldly;

namespace Infrastructure.Data.Tests;

[Collection(TestCollections.Database)]
public class UserIdentityRepositoryTests : RepositoryTestBase
{
    private FakeTimeProvider _timeProvider;
    private PostgresUserIdentityRepository _repository;

    public UserIdentityRepositoryTests(PostgresFixture fixture) : base(fixture)
    {
        _timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        _repository = new PostgresUserIdentityRepository(_dbContext, _timeProvider);
    }

    public class SaveAsync(PostgresFixture fixture) : UserIdentityRepositoryTests(fixture)
    {
        [Fact]
        public async Task SaveAsync_WithValidEmail_SavesUserAndReturnsId()
        {
            // Arrange
            var email = "Test@email.com";

            // Act
            Guid userId = await _repository.SaveAsync(email);

            //Assert
            var savedUser = await _dbContext.Users.FindAsync(userId);
            savedUser.ShouldNotBeNull();
            savedUser.Email.ShouldBe(email);
            savedUser.CreatedAt.ShouldBe(_timeProvider.GetUtcNow().UtcDateTime);
        }
    }

    public class ExistsAsync(PostgresFixture fixture) : UserIdentityRepositoryTests(fixture)
    {
        [Fact]
        public async Task ExistsAsync_WithExistingUserId_ReturnsTrue()
        {
            // Arrange
            var email = "test@email.com";
            var userId = await _repository.SaveAsync(email);

            // Act
            var exists = await _repository.ExistsAsync(userId);

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
            var email = "test@email.com";
            await _repository.SaveAsync(email);

            // Act
            var exists = await _repository.ExistsAsync(email);

            // Assert
            exists.ShouldBeTrue();
        }

        [Fact]
        public async Task ExistsAsync_WithNonExistingEmail_ReturnsFalse()
        {
            // Arrange
            var nonExistingEmail = "nonexisting@email.com";

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
            var email = "test@email.com";
            var userId = await _repository.SaveAsync(email);

            // Act
            var user = await _repository.GetAsync(userId);

            // Assert
            user.ShouldNotBeNull();
            user.Id.ShouldBe(userId);
            user.Email.ShouldBe(email);
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
            var email = "test@email.com";
            var userId = await _repository.SaveAsync(email);

            // Act
            var user = await _repository.GetAsync(email);

            // Assert
            user.ShouldNotBeNull();
            user.Id.ShouldBe(userId);
            user.Email.ShouldBe(email);
        }

        [Fact]
        public async Task GetAsync_WithNonExistingEmail_ReturnsNull()
        {
            // Arrange
            var nonExistingEmail = "nonexisting@email.com";

            // Act 
            var result = await _repository.GetAsync(nonExistingEmail);

            // Assert
            result.ShouldBeNull();
        }

        [Fact]
        public async Task GetAsync_WithDifferentCaseEmail_ReturnsCorrectUser()
        {
            // Arrange
            var email = "Test@Email.com";
            var userId = await _repository.SaveAsync(email);

            // Act
            var user = await _repository.GetAsync("test@email.com");

            // Assert
            user.ShouldNotBeNull();
            user.Id.ShouldBe(userId);
            user.Email.ShouldBe(email);
        }
    }
}