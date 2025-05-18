using Application.Interfaces.Data.Repositories;
using Application.Services;
using Core.Entities;
using Core.Exceptions;
using Moq;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Application.Tests.Services;

public class RoleServiceTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IRoleRepository> _repositoryMock;
    private readonly RoleService _service;

    private RoleServiceTests(ITestOutputHelper output)
    {
        _output = output;
        _repositoryMock = new Mock<IRoleRepository>();
        _service = new RoleService(_repositoryMock.Object);
    }

    public class GetByNameAsync(ITestOutputHelper output) : RoleServiceTests(output)
    {
        [Fact]
        public async Task GetByNameAsync_WithExistingRole_ReturnsRole()
        {
            // Arrange
            var expectedRole = new Role { Id = Guid.NewGuid(), Name = "admin" };
            _repositoryMock.Setup(r => r.GetByNameAsync("admin")).ReturnsAsync(expectedRole);

            // Act
            var result = await _service.GetByNameAsync("admin");

            // Assert
            result.ShouldBe(expectedRole);
        }

        [Fact]
        public async Task GetByNameAsync_WithNonExistingRole_ThrowsEntityNotFoundException()
        {
            // Arrange
            _repositoryMock.Setup(r => r.GetByNameAsync(It.IsAny<string>())).ReturnsAsync((Role?)null);

            // Act & Assert
            await Should.ThrowAsync<EntityNotFoundException>(
                async () => await _service.GetByNameAsync("nonexistent"));
        }
    }

    public class GetByIdAsync(ITestOutputHelper output) : RoleServiceTests(output)
    {
        [Fact]
        public async Task GetByIdAsync_WithExistingRole_ReturnsRole()
        {
            // Arrange
            var roleId = Guid.NewGuid();
            var expectedRole = new Role { Id = roleId, Name = "admin" };
            _repositoryMock.Setup(r => r.GetByIdAsync(roleId)).ReturnsAsync(expectedRole);

            // Act
            var result = await _service.GetByIdAsync(roleId);

            // Assert
            result.ShouldBe(expectedRole);
        }

        [Fact]
        public async Task GetByIdAsync_WithNonExistingRole_ThrowsEntityNotFoundException()
        {
            // Arrange
            var roleId = Guid.NewGuid();
            _repositoryMock.Setup(r => r.GetByIdAsync(roleId)).ReturnsAsync((Role?)null);

            // Act & Assert
            await Should.ThrowAsync<EntityNotFoundException>(
                async () => await _service.GetByIdAsync(roleId));
        }
    }

    public class CreateAsync(ITestOutputHelper output) : RoleServiceTests(output)
    {
        [Fact]
        public async Task CreateAsync_WithUniqueRoleName_CreatesAndReturnsRoleId()
        {
            // Arrange
            var expectedId = Guid.NewGuid();
            _repositoryMock.Setup(r => r.ExistsAsync("newrole")).ReturnsAsync(false);
            _repositoryMock.Setup(r => r.CreateAsync("newrole", "description")).ReturnsAsync(expectedId);

            // Act
            var result = await _service.CreateAsync("newrole", "description");

            // Assert
            result.ShouldBe(expectedId);
        }

        [Fact]
        public async Task CreateAsync_WithExistingRoleName_ThrowsBusinessRuleViolationException()
        {
            // Arrange
            _repositoryMock.Setup(r => r.ExistsAsync("admin")).ReturnsAsync(true);

            // Act & Assert
            await Should.ThrowAsync<BusinessRuleViolationException>(async () => await _service.CreateAsync("admin"));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("   ")]
        public async Task CreateAsync_WithInvalidRoleName_ThrowsArgumentException(string? roleName)
        {
            // Act & Assert
            await Should.ThrowAsync<ArgumentException>(async () => await _service.CreateAsync(roleName!));
        }
    }

    public class UpdateAsync(ITestOutputHelper output) : RoleServiceTests(output)
    {
        [Fact]
        public async Task UpdateAsync_WithExistingRoleAndUniqueNewName_UpdatesRole()
        {
            // Arrange
            var roleId = Guid.NewGuid();
            _repositoryMock.Setup(r => r.ExistsByIdAsync(roleId)).ReturnsAsync(true);
            _repositoryMock.Setup(r => r.GetByNameAsync("newname")).ReturnsAsync((Role?)null);

            // Act
            await _service.UpdateAsync(roleId, "newname", "description");

            // Assert
            _repositoryMock.Verify(r => r.UpdateAsync(roleId, "newname", "description"), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WithSameRoleAndSameName_UpdatesRole()
        {
            // Arrange
            var roleId = Guid.NewGuid();
            var existingRole = new Role { Id = roleId, Name = "admin" };
            _repositoryMock.Setup(r => r.ExistsByIdAsync(roleId)).ReturnsAsync(true);
            _repositoryMock.Setup(r => r.GetByNameAsync("admin")).ReturnsAsync(existingRole);

            // Act
            await _service.UpdateAsync(roleId, "admin", "new description");

            // Assert
            _repositoryMock.Verify(r => r.UpdateAsync(roleId, "admin", "new description"), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WithExistingNameForDifferentRole_ThrowsBusinessRuleViolationException()
        {
            // Arrange
            var roleId = Guid.NewGuid();
            var differentRoleId = Guid.NewGuid();
            var existingRole = new Role { Id = differentRoleId, Name = "admin" };
            _repositoryMock.Setup(r => r.ExistsByIdAsync(roleId)).ReturnsAsync(true);
            _repositoryMock.Setup(r => r.GetByNameAsync("admin")).ReturnsAsync(existingRole);

            // Act & Assert
            await Should.ThrowAsync<BusinessRuleViolationException>(async () => await _service.UpdateAsync(roleId, "admin", "description"));
        }

        [Fact]
        public async Task UpdateAsync_WithNonExistingRole_ThrowsEntityNotFoundException()
        {
            // Arrange
            var roleId = Guid.NewGuid();
            _repositoryMock.Setup(r => r.ExistsByIdAsync(roleId))
                .ReturnsAsync(false);

            // Act & Assert
            await Should.ThrowAsync<EntityNotFoundException>(async () => await _service.UpdateAsync(roleId, "name", "description"));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("   ")]
        public async Task UpdateAsync_WithInvalidRoleName_ThrowsArgumentException(string? roleName)
        {
            // Arrange
            var roleId = Guid.NewGuid();
            _repositoryMock.Setup(r => r.ExistsByIdAsync(roleId)).ReturnsAsync(true);

            // Act & Assert
            await Should.ThrowAsync<ArgumentException>(async () => await _service.UpdateAsync(roleId, roleName!, "description"));
        }
    }

    public class DeleteAsync(ITestOutputHelper output) : RoleServiceTests(output)
    {
        [Fact]
        public async Task DeleteAsync_WithExistingRole_DeletesRole()
        {
            // Arrange
            var roleId = Guid.NewGuid();
            _repositoryMock.Setup(r => r.ExistsByIdAsync(roleId)).ReturnsAsync(true);

            // Act
            await _service.DeleteAsync(roleId);

            // Assert
            _repositoryMock.Verify(r => r.DeleteAsync(roleId), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WithNonExistingRole_ThrowsEntityNotFoundException()
        {
            // Arrange
            var roleId = Guid.NewGuid();
            _repositoryMock.Setup(r => r.ExistsByIdAsync(roleId)).ReturnsAsync(false);

            // Act & Assert
            await Should.ThrowAsync<EntityNotFoundException>(async () => await _service.DeleteAsync(roleId));
        }
    }

    public class GetAllAsync(ITestOutputHelper output) : RoleServiceTests(output)
    {
        [Fact]
        public async Task GetAllAsync_WithMultipleRoles_ReturnsAllRoles()
        {
            // Arrange
            var expectedRoles = new List<Role>
            {
                new() { Id = Guid.NewGuid(), Name = "admin" },
                new() { Id = Guid.NewGuid(), Name = "member" },
            };
            _repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(expectedRoles);

            // Act
            var result = await _service.GetAllAsync();

            // Assert
            result.ShouldBe(expectedRoles);
        }

        [Fact]
        public async Task GetAllAsync_WithNoRoles_ReturnsEmptyCollection()
        {
            // Arrange
            _repositoryMock.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<Role>());

            // Act
            var result = await _service.GetAllAsync();

            // Assert
            result.ShouldBeEmpty();
        }
    }

}