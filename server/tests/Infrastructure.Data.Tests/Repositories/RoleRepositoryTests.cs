using Core.Exceptions;
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
public class RoleRepositoryTests : RepositoryTestBase
{
    private readonly FakeTimeProvider _timeProvider;
    private readonly PgRoleRepository _repository;

    private RoleRepositoryTests(PostgresFixture fixture) : base(fixture)
    {
        _timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        _repository = new PgRoleRepository(DbContext, _timeProvider);
    }

    public class GetByNameAsync(PostgresFixture fixture) : RoleRepositoryTests(fixture)
    {
        [Fact]
        public async Task GetByNameAsync_WithExistingRole_ReturnsRole()
        {
            // Arrange
            var role = await DbContext.SeedRoleAsync(_timeProvider, "admin");

            // Act
            var result = await _repository.GetByNameAsync("admin");

            // Assert
            result.ShouldNotBeNull();
            result.Id.ShouldBe(role.Id);
            result.Name.ShouldBe("admin");
        }

        [Fact]
        public async Task GetByNameAsync_WithNonExistingRole_ReturnsNull()
        {
            // Arrange

            // Act
            var result = await _repository.GetByNameAsync("nonexistent");

            // Assert
            result.ShouldBeNull();
        }

        [Fact]
        public async Task GetByNameAsync_IsCaseInsensitive()
        {
            // Arrange
            var role = await DbContext.SeedRoleAsync(_timeProvider, "test_role");

            // Act
            var result = await _repository.GetByNameAsync("TEST_ROLE");

            // Assert
            result.ShouldNotBeNull();
            result.Id.ShouldBe(role.Id);
        }
    }

    public class GetByIdAsync(PostgresFixture fixture) : RoleRepositoryTests(fixture)
    {
        [Fact]
        public async Task GetByIdAsync_WithExistingRole_ReturnsRole()
        {
            // Arrange
            var role = await DbContext.SeedRoleAsync(_timeProvider, "admin");

            // Act
            var result = await _repository.GetByIdAsync(role.Id);

            // Assert
            result.ShouldNotBeNull();
            result.Id.ShouldBe(role.Id);
            result.Name.ShouldBe("admin");
        }

        [Fact]
        public async Task GetByIdAsync_WithNonExistingRole_ReturnsNull()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var result = await _repository.GetByIdAsync(nonExistentId);

            // Assert
            result.ShouldBeNull();
        }
    }

    public class GetAllAsync(PostgresFixture fixture) : RoleRepositoryTests(fixture)
    {
        [Fact]
        public async Task GetAllAsync_WithMultipleRoles_ReturnsAllRolesOrderedByName()
        {
            // Arrange
            await DbContext.SeedRoleAsync(_timeProvider, "test_user");
            await DbContext.SeedRoleAsync(_timeProvider, "test_admin");
            await DbContext.SeedRoleAsync(_timeProvider, "test_moderator");

            // Act
            var result = await _repository.GetAllAsync();

            // Assert
            var roles = result.ToList();
            roles.Count.ShouldBeGreaterThan(5); // Inklusiv admin og member (pre-seeded) 
            roles.Any(r => r.Name == "test_admin").ShouldBeTrue();
            roles.Any(r => r.Name == "test_moderator").ShouldBeTrue();
            roles.Any(r => r.Name == "test_user").ShouldBeTrue();
            
            // Verificer sortering
            var roleNames = roles.Select(r => r.Name).ToList();
            var sortedNames = roleNames.OrderBy(n => n).ToList();
            roleNames.ShouldBe(sortedNames);
        }

        [Fact]
        public async Task GetAllAsync_WithRoles_ReturnsAtLeastSeededRoles()
        {
            // Arrange
            // Database har pre-seedede roller fra migrations

            // Act
            var result = await _repository.GetAllAsync();

            // Assert
            var roles = result.ToList();
            roles.Count.ShouldBeGreaterThanOrEqualTo(2); // Admin og member tilføjet fra seeding 
            roles.Any(r => r.Name == "admin").ShouldBeTrue();
            roles.Any(r => r.Name == "member").ShouldBeTrue();
        }
    }

    public class CreateAsync(PostgresFixture fixture) : RoleRepositoryTests(fixture)
    {
        [Fact]
        public async Task CreateAsync_WithValidData_CreatesRoleAndReturnsId()
        {
            // Arrange
            var now = _timeProvider.Now();

            // Act
            var id = await _repository.CreateAsync("newrole", "Role description");

            // Assert
            id.ShouldNotBe(Guid.Empty);
            var savedRole = await DbContext.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
            
            savedRole.ShouldNotBeNull();
            savedRole.Name.ShouldBe("newrole");
            savedRole.Description.ShouldBe("Role description");
            savedRole.CreatedAt.ShouldBeWithinTolerance(now);
            savedRole.UpdatedAt.ShouldBeNull();
        }

        [Fact]
        public async Task CreateAsync_WithoutDescription_CreatesRoleWithNullDescription()
        {
            // Arrange

            // Act
            var id = await _repository.CreateAsync($"test_role_{Guid.NewGuid()}", null);

            // Assert
            var savedRole = await DbContext.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
            savedRole.ShouldNotBeNull();
            savedRole.Description.ShouldBeNull();
        }
    }

    public class ExistsAsync(PostgresFixture fixture) : RoleRepositoryTests(fixture)
    {
        [Fact]
        public async Task ExistsAsync_WithExistingRole_ReturnsTrue()
        {
            // Arrange
            await DbContext.SeedRoleAsync(_timeProvider, "admin");

            // Act
            var result = await _repository.ExistsAsync("admin");

            // Assert
            result.ShouldBeTrue();
        }

        [Fact]
        public async Task ExistsAsync_WithNonExistingRole_ReturnsFalse()
        {
            // Arrange

            // Act
            var result = await _repository.ExistsAsync("nonexistent");

            // Assert
            result.ShouldBeFalse();
        }

        [Fact]
        public async Task ExistsAsync_IsCaseInsensitive()
        {
            // Arrange
            await DbContext.SeedRoleAsync(_timeProvider, "Admin");

            // Act
            var result = await _repository.ExistsAsync("admin");

            // Assert
            result.ShouldBeTrue();
        }
    }

    public class ExistsByIdAsync(PostgresFixture fixture) : RoleRepositoryTests(fixture)
    {
        [Fact]
        public async Task ExistsByIdAsync_WithExistingRole_ReturnsTrue()
        {
            // Arrange
            var role = await DbContext.SeedRoleAsync(_timeProvider, "admin");

            // Act
            var result = await _repository.ExistsByIdAsync(role.Id);

            // Assert
            result.ShouldBeTrue();
        }

        [Fact]
        public async Task ExistsByIdAsync_WithNonExistingRole_ReturnsFalse()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var result = await _repository.ExistsByIdAsync(nonExistentId);

            // Assert
            result.ShouldBeFalse();
        }
    }

    public class UpdateAsync(PostgresFixture fixture) : RoleRepositoryTests(fixture)
    {
        [Fact]
        public async Task UpdateAsync_WithExistingRole_UpdatesRoleProperties()
        {
            // Arrange
            var role = await DbContext.SeedRoleAsync(_timeProvider, "admin", "Old description");
            var updateTime = _timeProvider.Now();

            // Act
            await _repository.UpdateAsync(role.Id, "newname", "New description");

            // Assert
            var updatedRole = await DbContext.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Id == role.Id);
            
            updatedRole.ShouldNotBeNull();
            updatedRole.Name.ShouldBe("newname");
            updatedRole.Description.ShouldBe("New description");
            updatedRole.UpdatedAt.ShouldNotBeNull();
            updatedRole.UpdatedAt!.Value.ShouldBeWithinTolerance(updateTime);
        }

        [Fact]
        public async Task UpdateAsync_WithNonExistingRole_ThrowsEntityNotFoundException()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act & Assert
            await Should.ThrowAsync<EntityNotFoundException>(() => _repository.UpdateAsync(nonExistentId, "name", "description"));
        }
    }

    public class DeleteAsync(PostgresFixture fixture) : RoleRepositoryTests(fixture)
    {
        [Fact]
        public async Task DeleteAsync_WithExistingRole_DeletesRole()
        {
            // Arrange
            var role = await DbContext.SeedRoleAsync(_timeProvider, "admin");

            // Act
            await _repository.DeleteAsync(role.Id);

            // Assert
            var deletedRole = await DbContext.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Id == role.Id);
            deletedRole.ShouldBeNull();
        }

        [Fact]
        public async Task DeleteAsync_WithNonExistingRole_ThrowsEntityNotFoundException()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act & Assert
            await Should.ThrowAsync<EntityNotFoundException>(() => _repository.DeleteAsync(nonExistentId));
        }
    }
}