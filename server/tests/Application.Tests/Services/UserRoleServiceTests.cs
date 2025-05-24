using Application.Interfaces.Data.Repositories;
using Application.Interfaces.Data.Repositories.Auth;
using Application.Services;
using Application.Services.Auth;
using Core.Entities;
using Core.Exceptions;
using Moq;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Application.Tests.Services;

public class UserRoleServiceTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IUserRoleRepository> _userRoleRepositoryMock;
    private readonly Mock<IRoleRepository> _roleRepositoryMock;
    private readonly Mock<IUserIdentityService> _userServiceMock;
    private readonly UserRoleService _service;

    private UserRoleServiceTests(ITestOutputHelper output)
    {
        _output = output;
        _userRoleRepositoryMock = new Mock<IUserRoleRepository>();
        _roleRepositoryMock = new Mock<IRoleRepository>();
        _userServiceMock = new Mock<IUserIdentityService>();
        _service = new UserRoleService(
            _userRoleRepositoryMock.Object,
            _roleRepositoryMock.Object,
            _userServiceMock.Object);
    }

    public class AssignRoleAsync(ITestOutputHelper output) : UserRoleServiceTests(output)
    {
        [Fact]
        public async Task AssignRoleAsync_WithValidUserAndRole_AssignsRoleAndReturnsId()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var roleId = Guid.NewGuid();
            var expectedId = Guid.NewGuid();
            var role = new Role { Id = roleId, Name = "admin" };

            _userServiceMock.Setup(s => s.ExistsAsync(userId)).ReturnsAsync(true);
            _roleRepositoryMock.Setup(r => r.GetByNameAsync("admin")).ReturnsAsync(role);
            _roleRepositoryMock.Setup(r => r.ExistsByIdAsync(roleId)).ReturnsAsync(true);
            _userRoleRepositoryMock.Setup(r => r.AssignRoleAsync(userId, roleId, null)).ReturnsAsync(expectedId);

            // Act
            var result = await _service.AssignRoleAsync(userId, "admin");

            // Assert
            result.ShouldBe(expectedId);
        }

        [Fact]
        public async Task AssignRoleAsync_WithAssignedBy_PassesAssignedByToRepository()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var roleId = Guid.NewGuid();
            var assignedBy = Guid.NewGuid();
            var expectedId = Guid.NewGuid();
            var role = new Role { Id = roleId, Name = "admin" };

            _userServiceMock.Setup(s => s.ExistsAsync(userId)).ReturnsAsync(true);
            _roleRepositoryMock.Setup(r => r.GetByNameAsync("admin")).ReturnsAsync(role);
            _roleRepositoryMock.Setup(r => r.ExistsByIdAsync(roleId)).ReturnsAsync(true);
            _userRoleRepositoryMock.Setup(r => r.AssignRoleAsync(userId, roleId, assignedBy)).ReturnsAsync(expectedId);

            // Act
            var result = await _service.AssignRoleAsync(userId, "admin", assignedBy);

            // Assert
            result.ShouldBe(expectedId);
        }

        [Fact]
        public async Task AssignRoleAsync_WithNonExistingUser_ThrowsEntityNotFoundException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _userServiceMock.Setup(s => s.ExistsAsync(userId)).ReturnsAsync(false);

            // Act & Assert
            await Should.ThrowAsync<EntityNotFoundException>(async () => await _service.AssignRoleAsync(userId, "admin"));
        }

        [Fact]
        public async Task AssignRoleAsync_WithNonExistingRoleName_ThrowsEntityNotFoundException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _userServiceMock.Setup(s => s.ExistsAsync(userId)).ReturnsAsync(true);
            _roleRepositoryMock.Setup(r => r.GetByNameAsync("invalidrole")).ReturnsAsync((Role?)null);

            // Act & Assert
            await Should.ThrowAsync<EntityNotFoundException>(async () => await _service.AssignRoleAsync(userId, "invalidrole"));
        }

        [Fact]
        public async Task AssignRoleAsync_WithNonExistingRoleId_ThrowsEntityNotFoundException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var roleId = Guid.NewGuid();
            _userServiceMock.Setup(s => s.ExistsAsync(userId)).ReturnsAsync(true);
            _roleRepositoryMock.Setup(r => r.ExistsByIdAsync(roleId)).ReturnsAsync(false);

            // Act & Assert
            await Should.ThrowAsync<EntityNotFoundException>(async () => await _service.AssignRoleAsync(userId, roleId));
        }
    }

    public class RemoveRoleAsync(ITestOutputHelper output) : UserRoleServiceTests(output)
    {
        [Fact]
        public async Task RemoveRoleAsync_WithValidUserAndRoleName_RemovesRole()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var roleId = Guid.NewGuid();
            var role = new Role { Id = roleId, Name = "admin" };

            _userServiceMock.Setup(s => s.ExistsAsync(userId)).ReturnsAsync(true);
            _roleRepositoryMock.Setup(r => r.GetByNameAsync("admin")).ReturnsAsync(role);
            _roleRepositoryMock.Setup(r => r.ExistsByIdAsync(roleId)).ReturnsAsync(true);

            // Act
            await _service.RemoveRoleAsync(userId, "admin");

            // Assert
            _userRoleRepositoryMock.Verify(r => r.RemoveRoleAsync(userId, roleId), Times.Once);
        }

        [Fact]
        public async Task RemoveRoleAsync_WithValidUserAndRoleId_RemovesRole()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var roleId = Guid.NewGuid();

            _userServiceMock.Setup(s => s.ExistsAsync(userId)).ReturnsAsync(true);
            _roleRepositoryMock.Setup(r => r.ExistsByIdAsync(roleId)).ReturnsAsync(true);

            // Act
            await _service.RemoveRoleAsync(userId, roleId);

            // Assert
            _userRoleRepositoryMock.Verify(r => r.RemoveRoleAsync(userId, roleId), Times.Once);
        }

        [Fact]
        public async Task RemoveRoleAsync_WithNonExistingUser_ThrowsEntityNotFoundException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var roleId = Guid.NewGuid();
            _userServiceMock.Setup(s => s.ExistsAsync(userId)).ReturnsAsync(false);

            // Act & Assert
            await Should.ThrowAsync<EntityNotFoundException>(
                async () => await _service.RemoveRoleAsync(userId, roleId));
        }

        [Fact]
        public async Task RemoveRoleAsync_WithNonExistingRoleName_ThrowsEntityNotFoundException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _userServiceMock.Setup(s => s.ExistsAsync(userId)).ReturnsAsync(true);
            _roleRepositoryMock.Setup(r => r.GetByNameAsync("invalidrole")).ReturnsAsync((Role?)null);

            // Act & Assert
            await Should.ThrowAsync<EntityNotFoundException>(async () => await _service.RemoveRoleAsync(userId, "invalidrole"));
        }

        [Fact]
        public async Task RemoveRoleAsync_WithNonExistingRoleId_ThrowsEntityNotFoundException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var roleId = Guid.NewGuid();
            _userServiceMock.Setup(s => s.ExistsAsync(userId)).ReturnsAsync(true);
            _roleRepositoryMock.Setup(r => r.ExistsByIdAsync(roleId)).ReturnsAsync(false);

            // Act & Assert
            await Should.ThrowAsync<EntityNotFoundException>(async () => await _service.RemoveRoleAsync(userId, roleId));
        }
    }

    public class GetUserRolesAsync(ITestOutputHelper output) : UserRoleServiceTests(output)
    {
        [Fact]
        public async Task GetUserRolesAsync_WithExistingUser_ReturnsUserRoles()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var expectedRoles = new List<Role>
            {
                new() { Id = Guid.NewGuid(), Name = "admin" },
                new() { Id = Guid.NewGuid(), Name = "user" }
            };

            _userServiceMock.Setup(s => s.ExistsAsync(userId)).ReturnsAsync(true);
            _userRoleRepositoryMock.Setup(r => r.GetUserRolesAsync(userId)).ReturnsAsync(expectedRoles);

            // Act
            var result = await _service.GetUserRolesAsync(userId);

            // Assert
            result.ShouldBe(expectedRoles);
        }

        [Fact]
        public async Task GetUserRolesAsync_WithNonExistingUser_ThrowsEntityNotFoundException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _userServiceMock.Setup(s => s.ExistsAsync(userId)).ReturnsAsync(false);

            // Act & Assert
            await Should.ThrowAsync<EntityNotFoundException>(async () => await _service.GetUserRolesAsync(userId));
        }
    }

    public class GetUsersInRoleAsync(ITestOutputHelper output) : UserRoleServiceTests(output)
    {
        [Fact]
        public async Task GetUsersInRoleAsync_WithExistingRoleName_ReturnsUsersInRole()
        {
            // Arrange
            var roleId = Guid.NewGuid();
            var role = new Role { Id = roleId, Name = "admin" };
            var expectedUsers = new List<User>
            {
                new() { Id = Guid.NewGuid(), Email = "user1@test.dk" },
                new() { Id = Guid.NewGuid(), Email = "user2@test.dk" }
            };

            _roleRepositoryMock.Setup(r => r.GetByNameAsync("admin")).ReturnsAsync(role);
            _userRoleRepositoryMock.Setup(r => r.GetUsersInRoleAsync(roleId)).ReturnsAsync(expectedUsers);

            // Act
            var result = await _service.GetUsersInRoleAsync("admin");

            // Assert
            result.ShouldBe(expectedUsers);
        }

        [Fact]
        public async Task GetUsersInRoleAsync_WithExistingRoleId_ReturnsUsersInRole()
        {
            // Arrange
            var roleId = Guid.NewGuid();
            var expectedUsers = new List<User>
            {
                new() { Id = Guid.NewGuid(), Email = "user1@test.dk" },
                new() { Id = Guid.NewGuid(), Email = "user2@test.dk" }
            };

            _roleRepositoryMock.Setup(r => r.ExistsByIdAsync(roleId)).ReturnsAsync(true);
            _userRoleRepositoryMock.Setup(r => r.GetUsersInRoleAsync(roleId)).ReturnsAsync(expectedUsers);

            // Act
            var result = await _service.GetUsersInRoleAsync(roleId);

            // Assert
            result.ShouldBe(expectedUsers);
        }

        [Fact]
        public async Task GetUsersInRoleAsync_WithNonExistingRoleName_ThrowsEntityNotFoundException()
        {
            // Arrange
            _roleRepositoryMock.Setup(r => r.GetByNameAsync("invalidrole")).ReturnsAsync((Role?)null);

            // Act & Assert
            await Should.ThrowAsync<EntityNotFoundException>(async () => await _service.GetUsersInRoleAsync("invalidrole"));
        }

        [Fact]
        public async Task GetUsersInRoleAsync_WithNonExistingRoleId_ThrowsEntityNotFoundException()
        {
            // Arrange
            var roleId = Guid.NewGuid();
            _roleRepositoryMock.Setup(r => r.ExistsByIdAsync(roleId)).ReturnsAsync(false);

            // Act & Assert
            await Should.ThrowAsync<EntityNotFoundException>(async () => await _service.GetUsersInRoleAsync(roleId));
        }
    }

    public class UserHasRoleAsync(ITestOutputHelper output) : UserRoleServiceTests(output)
    {
        [Fact]
        public async Task UserHasRoleAsync_WithUserInRole_ReturnsTrue()
        {
            // Arrange
            var userId = Guid.NewGuid();
            
            _userServiceMock.Setup(s => s.ExistsAsync(userId)).ReturnsAsync(true);
            _userRoleRepositoryMock.Setup(r => r.HasRoleAsync(userId, "admin")).ReturnsAsync(true);

            // Act
            var result = await _service.UserHasRoleAsync(userId, "admin");

            // Assert
            result.ShouldBeTrue();
        }

        [Fact]
        public async Task UserHasRoleAsync_WithUserNotInRole_ReturnsFalse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            
            _userServiceMock.Setup(s => s.ExistsAsync(userId)).ReturnsAsync(true);
            _userRoleRepositoryMock.Setup(r => r.HasRoleAsync(userId, "admin")).ReturnsAsync(false);

            // Act
            var result = await _service.UserHasRoleAsync(userId, "admin");

            // Assert
            result.ShouldBeFalse();
        }

        [Fact]
        public async Task UserHasRoleAsync_WithNonExistingUser_ReturnsFalse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _userServiceMock.Setup(s => s.ExistsAsync(userId)).ReturnsAsync(false);

            // Act
            var result = await _service.UserHasRoleAsync(userId, "admin");

            // Assert
            result.ShouldBeFalse();
        }
    }

    public class UserHasAnyRoleAsync(ITestOutputHelper output) : UserRoleServiceTests(output)
    {
        [Fact]
        public async Task UserHasAnyRoleAsync_WithUserHavingOneMatchingRole_ReturnsTrue()
        {
            // Arrange
            var userId = Guid.NewGuid();
            
            _userServiceMock.Setup(s => s.ExistsAsync(userId)).ReturnsAsync(true);
            _userRoleRepositoryMock.Setup(r => r.HasRoleAsync(userId, "admin")).ReturnsAsync(false);
            _userRoleRepositoryMock.Setup(r => r.HasRoleAsync(userId, "moderator")).ReturnsAsync(true);

            // Act
            var result = await _service.UserHasAnyRoleAsync(userId, "admin", "moderator");

            // Assert
            result.ShouldBeTrue();
        }

        [Fact]
        public async Task UserHasAnyRoleAsync_WithUserHavingNoMatchingRoles_ReturnsFalse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            
            _userServiceMock.Setup(s => s.ExistsAsync(userId)).ReturnsAsync(true);
            _userRoleRepositoryMock.Setup(r => r.HasRoleAsync(userId, It.IsAny<string>())).ReturnsAsync(false);

            // Act
            var result = await _service.UserHasAnyRoleAsync(userId, "admin", "moderator");

            // Assert
            result.ShouldBeFalse();
        }

        [Fact]
        public async Task UserHasAnyRoleAsync_WithNonExistingUser_ReturnsFalse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _userServiceMock.Setup(s => s.ExistsAsync(userId)).ReturnsAsync(false);

            // Act
            var result = await _service.UserHasAnyRoleAsync(userId, "admin", "moderator");

            // Assert
            result.ShouldBeFalse();
        }

        [Fact]
        public async Task UserHasAnyRoleAsync_WithEmptyRoleList_ReturnsFalse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _userServiceMock.Setup(s => s.ExistsAsync(userId)).ReturnsAsync(true);

            // Act
            var result = await _service.UserHasAnyRoleAsync(userId);

            // Assert
            result.ShouldBeFalse();
        }
    }

    public class RemoveAllRolesAsync(ITestOutputHelper output) : UserRoleServiceTests(output)
    {
        [Fact]
        public async Task RemoveAllRolesAsync_WithExistingUser_RemovesAllRoles()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _userServiceMock.Setup(s => s.ExistsAsync(userId)).ReturnsAsync(true);

            // Act
            await _service.RemoveAllRolesAsync(userId);

            // Assert
            _userRoleRepositoryMock.Verify(r => r.RemoveAllRolesAsync(userId), Times.Once);
        }

        [Fact]
        public async Task RemoveAllRolesAsync_WithNonExistingUser_ThrowsEntityNotFoundException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _userServiceMock.Setup(s => s.ExistsAsync(userId)).ReturnsAsync(false);

            // Act & Assert
            await Should.ThrowAsync<EntityNotFoundException>(async () => await _service.RemoveAllRolesAsync(userId));
        }
    }
}