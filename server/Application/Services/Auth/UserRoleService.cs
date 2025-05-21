using Application.Interfaces.Data.Repositories;
using Application.Interfaces.Data.Repositories.Auth;
using Core.Entities;
using Core.Exceptions;

namespace Application.Services.Auth;

public interface IUserRoleService
{
    Task<Guid> AssignRoleAsync(Guid userId, string roleName, Guid? assignedBy = null);
    Task<Guid> AssignRoleAsync(Guid userId, Guid roleId, Guid? assignedBy = null);
    Task RemoveRoleAsync(Guid userId, string roleName);
    Task RemoveRoleAsync(Guid userId, Guid roleId);
    Task<IEnumerable<Role>> GetUserRolesAsync(Guid userId);
    Task<IEnumerable<User>> GetUsersInRoleAsync(string roleName);
    Task<IEnumerable<User>> GetUsersInRoleAsync(Guid roleId);
    Task<bool> UserHasRoleAsync(Guid userId, string roleName);
    Task<bool> UserHasRoleAsync(Guid userId, Guid roleId);
    Task<bool> UserHasAnyRoleAsync(Guid userId, params string[] roleNames);
    Task RemoveAllRolesAsync(Guid userId);
}

public class UserRoleService : IUserRoleService
{
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUserIdentityService _userService;

    public UserRoleService(
        IUserRoleRepository userRoleRepository, 
        IRoleRepository roleRepository,
        IUserIdentityService userService)
    {
        _userRoleRepository = userRoleRepository;
        _roleRepository = roleRepository;
        _userService = userService;
    }

    public async Task<Guid> AssignRoleAsync(Guid userId, string roleName, Guid? assignedBy = null)
    {
        var role = await _roleRepository.GetByNameAsync(roleName);
        if (role == null) throw new EntityNotFoundException($"Role '{roleName}' not found");

        return await AssignRoleAsync(userId, role.Id, assignedBy);
    }

    public async Task<Guid> AssignRoleAsync(Guid userId, Guid roleId, Guid? assignedBy = null)
    {
        if (!await _userService.ExistsAsync(userId)) throw new EntityNotFoundException($"User with ID {userId} not found");
        if (!await _roleRepository.ExistsByIdAsync(roleId)) throw new EntityNotFoundException($"Role with ID {roleId} not found");

        return await _userRoleRepository.AssignRoleAsync(userId, roleId, assignedBy);
    }

    public async Task RemoveRoleAsync(Guid userId, string roleName)
    {
        var role = await _roleRepository.GetByNameAsync(roleName);
        if (role == null) throw new EntityNotFoundException($"Role '{roleName}' not found");

        await RemoveRoleAsync(userId, role.Id);
    }

    public async Task RemoveRoleAsync(Guid userId, Guid roleId)
    {
        if (!await _userService.ExistsAsync(userId)) throw new EntityNotFoundException($"User with ID {userId} not found");
        if (!await _roleRepository.ExistsByIdAsync(roleId)) throw new EntityNotFoundException($"Role with ID {roleId} not found");

        await _userRoleRepository.RemoveRoleAsync(userId, roleId);
    }

    public async Task<IEnumerable<Role>> GetUserRolesAsync(Guid userId)
    {
        if (!await _userService.ExistsAsync(userId)) throw new EntityNotFoundException($"User with ID {userId} not found");

        return await _userRoleRepository.GetUserRolesAsync(userId);
    }

    public async Task<IEnumerable<User>> GetUsersInRoleAsync(string roleName)
    {
        var role = await _roleRepository.GetByNameAsync(roleName);
        if (role == null) throw new EntityNotFoundException($"Role '{roleName}' not found");

        return await _userRoleRepository.GetUsersInRoleAsync(role.Id);
    }

    public async Task<IEnumerable<User>> GetUsersInRoleAsync(Guid roleId)
    {
        if (!await _roleRepository.ExistsByIdAsync(roleId)) throw new EntityNotFoundException($"Role with ID {roleId} not found");

        return await _userRoleRepository.GetUsersInRoleAsync(roleId);
    }

    public async Task<bool> UserHasRoleAsync(Guid userId, string roleName)
    {
        if (!await _userService.ExistsAsync(userId)) return false;

        return await _userRoleRepository.HasRoleAsync(userId, roleName);
    }

    public async Task<bool> UserHasRoleAsync(Guid userId, Guid roleId)
    {
        if (!await _userService.ExistsAsync(userId)) return false;

        return await _userRoleRepository.HasRoleAsync(userId, roleId);
    }

    public async Task<bool> UserHasAnyRoleAsync(Guid userId, params string[] roleNames)
    {
        if (!await _userService.ExistsAsync(userId)) return false;

        foreach (var roleName in roleNames)
        {
            if (await _userRoleRepository.HasRoleAsync(userId, roleName)) return true;
        }

        return false;
    }

    public async Task RemoveAllRolesAsync(Guid userId)
    {
        if (!await _userService.ExistsAsync(userId)) throw new EntityNotFoundException($"User with ID {userId} not found");

        await _userRoleRepository.RemoveAllRolesAsync(userId);
    }
}