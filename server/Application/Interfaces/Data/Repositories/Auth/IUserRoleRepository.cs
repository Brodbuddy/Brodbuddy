using Core.Entities;

namespace Application.Interfaces.Data.Repositories.Auth;

public interface IUserRoleRepository
{
    Task<Guid> AssignRoleAsync(Guid userId, Guid roleId, Guid? assignedBy = null);
    Task RemoveRoleAsync(Guid userId, Guid roleId);
    Task<IEnumerable<Role>> GetUserRolesAsync(Guid userId);
    Task<IEnumerable<User>> GetUsersInRoleAsync(Guid roleId);
    Task<bool> HasRoleAsync(Guid userId, string roleName);
    Task<bool> HasRoleAsync(Guid userId, Guid roleId);
    Task RemoveAllRolesAsync(Guid userId);
}