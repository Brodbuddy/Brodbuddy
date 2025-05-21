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
public class UserRoleRepositoryTests : RepositoryTestBase
{
    private readonly FakeTimeProvider _timeProvider;
    private readonly PgUserRoleRepository _repository;

    private UserRoleRepositoryTests(PostgresFixture fixture) : base(fixture)
    {
        _timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        _repository = new PgUserRoleRepository(DbContext, _timeProvider);
    }

    public class AssignRoleAsync(PostgresFixture fixture) : UserRoleRepositoryTests(fixture)
    {
        [Fact]
        public async Task AssignRoleAsync_WithNewUserRole_CreatesAssignmentAndReturnsId()
        {
            // Arrange
            var user = await DbContext.SeedUserAsync(_timeProvider);
            var role = await DbContext.SeedRoleAsync(_timeProvider, "admin");
            var assignedBy = await DbContext.SeedUserAsync(_timeProvider, "assigner@test.dk");
            var now = _timeProvider.Now();

            // Act
            var id = await _repository.AssignRoleAsync(user.Id, role.Id, assignedBy.Id);

            // Assert
            id.ShouldNotBe(Guid.Empty);
            var userRole = await DbContext.UserRoles.AsNoTracking().FirstOrDefaultAsync(ur => ur.Id == id);
            
            userRole.ShouldNotBeNull();
            userRole.UserId.ShouldBe(user.Id);
            userRole.RoleId.ShouldBe(role.Id);
            userRole.CreatedBy.ShouldBe(assignedBy.Id);
            userRole.CreatedAt.ShouldBeWithinTolerance(now);
        }

        [Fact]
        public async Task AssignRoleAsync_WithExistingUserRole_ReturnsExistingId()
        {
            // Arrange
            var user = await DbContext.SeedUserAsync(_timeProvider);
            var role = await DbContext.SeedRoleAsync(_timeProvider, "admin");
            var existingId = await _repository.AssignRoleAsync(user.Id, role.Id);

            // Act
            var id = await _repository.AssignRoleAsync(user.Id, role.Id);

            // Assert
            id.ShouldBe(existingId);
            var userRoles = await DbContext.UserRoles.AsNoTracking().Where(ur => ur.UserId == user.Id && ur.RoleId == role.Id).ToListAsync();
            userRoles.Count.ShouldBe(1);
        }

        [Fact]
        public async Task AssignRoleAsync_WithoutAssignedBy_CreatesAssignmentWithNullCreatedBy()
        {
            // Arrange
            var user = await DbContext.SeedUserAsync(_timeProvider);
            var role = await DbContext.SeedRoleAsync(_timeProvider, "admin");

            // Act
            var id = await _repository.AssignRoleAsync(user.Id, role.Id, null);

            // Assert
            var userRole = await DbContext.UserRoles.AsNoTracking().FirstOrDefaultAsync(ur => ur.Id == id);
            
            userRole.ShouldNotBeNull();
            userRole.CreatedBy.ShouldBeNull();
        }
    }

    public class RemoveRoleAsync(PostgresFixture fixture) : UserRoleRepositoryTests(fixture)
    {
        [Fact]
        public async Task RemoveRoleAsync_WithExistingUserRole_RemovesAssignment()
        {
            // Arrange
            var user = await DbContext.SeedUserAsync(_timeProvider);
            var role = await DbContext.SeedRoleAsync(_timeProvider, "admin");
            await _repository.AssignRoleAsync(user.Id, role.Id);

            // Act
            await _repository.RemoveRoleAsync(user.Id, role.Id);

            // Assert
            var userRole = await DbContext.UserRoles.AsNoTracking().FirstOrDefaultAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id);
            userRole.ShouldBeNull();
        }

        [Fact]
        public async Task RemoveRoleAsync_WithNonExistingUserRole_DoesNotThrow()
        {
            // Arrange
            var user = await DbContext.SeedUserAsync(_timeProvider);
            var role = await DbContext.SeedRoleAsync(_timeProvider, "admin");

            // Act
            await _repository.RemoveRoleAsync(user.Id, role.Id);

            // Assert
            Assert.True(true);
        }
    }

    public class GetUserRolesAsync(PostgresFixture fixture) : UserRoleRepositoryTests(fixture)
    {
        [Fact]
        public async Task GetUserRolesAsync_WithMultipleRoles_ReturnsRolesOrderedByName()
        {
            // Arrange
            var user = await DbContext.SeedUserAsync(_timeProvider);
            var adminRole = await DbContext.SeedRoleAsync(_timeProvider, "admin");
            var userRole = await DbContext.SeedRoleAsync(_timeProvider, "user");
            var moderatorRole = await DbContext.SeedRoleAsync(_timeProvider, "moderator");
            
            await _repository.AssignRoleAsync(user.Id, userRole.Id);
            await _repository.AssignRoleAsync(user.Id, adminRole.Id);
            await _repository.AssignRoleAsync(user.Id, moderatorRole.Id);

            // Act
            var roles = await _repository.GetUserRolesAsync(user.Id);

            // Assert
            var roleList = roles.ToList();
            roleList.Count.ShouldBe(3);
            roleList[0].Name.ShouldBe("admin");
            roleList[1].Name.ShouldBe("moderator");
            roleList[2].Name.ShouldBe("user");
        }

        [Fact]
        public async Task GetUserRolesAsync_WithNoRoles_ReturnsEmptyList()
        {
            // Arrange
            var user = await DbContext.SeedUserAsync(_timeProvider);

            // Act
            var roles = await _repository.GetUserRolesAsync(user.Id);

            // Assert
            roles.ShouldBeEmpty();
        }
    }

    public class GetUsersInRoleAsync(PostgresFixture fixture) : UserRoleRepositoryTests(fixture)
    {
        [Fact]
        public async Task GetUsersInRoleAsync_WithMultipleUsers_ReturnsUsersOrderedByEmail()
        {
            // Arrange
            var role = await DbContext.SeedRoleAsync(_timeProvider, "admin");
            var user1 = await DbContext.SeedUserAsync(_timeProvider, "charlie@test.dk");
            var user2 = await DbContext.SeedUserAsync(_timeProvider, "alice@test.dk");
            var user3 = await DbContext.SeedUserAsync(_timeProvider, "bob@test.dk");
            
            await _repository.AssignRoleAsync(user1.Id, role.Id);
            await _repository.AssignRoleAsync(user2.Id, role.Id);
            await _repository.AssignRoleAsync(user3.Id, role.Id);

            // Act
            var users = await _repository.GetUsersInRoleAsync(role.Id);

            // Assert
            var userList = users.ToList();
            userList.Count.ShouldBe(3);
            userList[0].Email.ShouldBe("alice@test.dk");
            userList[1].Email.ShouldBe("bob@test.dk");
            userList[2].Email.ShouldBe("charlie@test.dk");
        }

        [Fact]
        public async Task GetUsersInRoleAsync_WithNoUsers_ReturnsEmptyList()
        {
            // Arrange
            var role = await DbContext.SeedRoleAsync(_timeProvider, "admin");

            // Act
            var users = await _repository.GetUsersInRoleAsync(role.Id);

            // Assert
            users.ShouldBeEmpty();
        }
    }

    public class HasRoleAsync(PostgresFixture fixture) : UserRoleRepositoryTests(fixture)
    {
        [Fact]
        public async Task HasRoleAsync_UserWithRole_ReturnsTrue()
        {
            // Arrange
            var user = await DbContext.SeedUserAsync(_timeProvider);
            var role = await DbContext.SeedRoleAsync(_timeProvider, "admin");
            await _repository.AssignRoleAsync(user.Id, role.Id);

            // Act
            var result = await _repository.HasRoleAsync(user.Id, "admin");

            // Assert
            result.ShouldBeTrue();
        }

        [Fact]
        public async Task HasRoleAsync_UserWithoutRole_ReturnsFalse()
        {
            // Arrange
            var user = await DbContext.SeedUserAsync(_timeProvider);
            await DbContext.SeedRoleAsync(_timeProvider, "admin");

            // Act
            var result = await _repository.HasRoleAsync(user.Id, "admin");

            // Assert
            result.ShouldBeFalse();
        }

        [Fact]
        public async Task HasRoleAsync_IsCaseInsensitive()
        {
            // Arrange
            var user = await DbContext.SeedUserAsync(_timeProvider);
            var role = await DbContext.SeedRoleAsync(_timeProvider, "Admin");
            await _repository.AssignRoleAsync(user.Id, role.Id);

            // Act
            var result = await _repository.HasRoleAsync(user.Id, "admin");

            // Assert
            result.ShouldBeTrue();
        }

        [Fact]
        public async Task HasRoleAsync_ByRoleId_UserWithRole_ReturnsTrue()
        {
            // Arrange
            var user = await DbContext.SeedUserAsync(_timeProvider);
            var role = await DbContext.SeedRoleAsync(_timeProvider, "admin");
            await _repository.AssignRoleAsync(user.Id, role.Id);

            // Act
            var result = await _repository.HasRoleAsync(user.Id, role.Id);

            // Assert
            result.ShouldBeTrue();
        }

        [Fact]
        public async Task HasRoleAsync_ByRoleId_UserWithoutRole_ReturnsFalse()
        {
            // Arrange
            var user = await DbContext.SeedUserAsync(_timeProvider);
            var role = await DbContext.SeedRoleAsync(_timeProvider, "admin");

            // Act
            var result = await _repository.HasRoleAsync(user.Id, role.Id);

            // Assert
            result.ShouldBeFalse();
        }
    }

    public class RemoveAllRolesAsync(PostgresFixture fixture) : UserRoleRepositoryTests(fixture)
    {
        [Fact]
        public async Task RemoveAllRolesAsync_WithMultipleRoles_RemovesAllUserRoles()
        {
            // Arrange
            var user = await DbContext.SeedUserAsync(_timeProvider);
            var adminRole = await DbContext.SeedRoleAsync(_timeProvider, "admin");
            var userRole = await DbContext.SeedRoleAsync(_timeProvider, "user");
            var moderatorRole = await DbContext.SeedRoleAsync(_timeProvider, "moderator");
            
            await _repository.AssignRoleAsync(user.Id, adminRole.Id);
            await _repository.AssignRoleAsync(user.Id, userRole.Id);
            await _repository.AssignRoleAsync(user.Id, moderatorRole.Id);

            // Act
            await _repository.RemoveAllRolesAsync(user.Id);

            // Assert
            var remainingRoles = await DbContext.UserRoles.AsNoTracking().Where(ur => ur.UserId == user.Id).ToListAsync();
            remainingRoles.ShouldBeEmpty();
        }

        [Fact]
        public async Task RemoveAllRolesAsync_WithNoRoles_DoesNotThrow()
        {
            // Arrange
            var user = await DbContext.SeedUserAsync(_timeProvider);

            // Act
            await _repository.RemoveAllRolesAsync(user.Id);

            // Assert
            Assert.True(true);
        }
    }
}