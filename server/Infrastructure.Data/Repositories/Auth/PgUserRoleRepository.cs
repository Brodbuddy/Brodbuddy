using Application.Interfaces.Data.Repositories.Auth;
using Core.Entities;
using Core.Extensions;
using Infrastructure.Data.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Repositories.Auth;

public class PgUserRoleRepository : IUserRoleRepository
{
    private readonly PgDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public PgUserRoleRepository(PgDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<Guid> AssignRoleAsync(Guid userId, Guid roleId, Guid? assignedBy = null)
    {
        var existing = await _dbContext.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

        if (existing != null)
            return existing.Id;

        var userRole = new UserRole
        {
            UserId = userId,
            RoleId = roleId,
            CreatedAt = _timeProvider.Now(),
            CreatedBy = assignedBy
        };

        await _dbContext.UserRoles.AddAsync(userRole);
        await _dbContext.SaveChangesAsync();

        return userRole.Id;
    }

    public async Task RemoveRoleAsync(Guid userId, Guid roleId)
    {
        var userRole = await _dbContext.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

        if (userRole != null)
        {
            _dbContext.UserRoles.Remove(userRole);
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Role>> GetUserRolesAsync(Guid userId)
    {
        return await _dbContext.UserRoles
            .Where(ur => ur.UserId == userId)
            .Include(ur => ur.Role)
            .Select(ur => ur.Role)
            .OrderBy(r => r.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<User>> GetUsersInRoleAsync(Guid roleId)
    {
        return await _dbContext.UserRoles
            .Where(ur => ur.RoleId == roleId)
            .Include(ur => ur.User)
            .Select(ur => ur.User)
            .OrderBy(u => u.Email)
            .ToListAsync();
    }

    public async Task<bool> HasRoleAsync(Guid userId, string roleName)
    {
        return await _dbContext.UserRoles.Include(ur => ur.Role)
                                         .AnyAsync(ur => ur.UserId == userId && EF.Functions.ILike(ur.Role.Name, roleName));
    }

    public async Task<bool> HasRoleAsync(Guid userId, Guid roleId)
    {
        return await _dbContext.UserRoles.AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId);
    }

    public async Task RemoveAllRolesAsync(Guid userId)
    {
        var userRoles = await _dbContext.UserRoles.Where(ur => ur.UserId == userId)
                                                  .ToListAsync();

        _dbContext.UserRoles.RemoveRange(userRoles);
        await _dbContext.SaveChangesAsync();
    }
}