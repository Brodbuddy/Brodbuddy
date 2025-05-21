using Application.Interfaces.Data.Repositories.Auth;
using Core.Entities;
using Core.Exceptions;
using Core.Extensions;
using Infrastructure.Data.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Repositories.Auth;

public class PgRoleRepository : IRoleRepository
{
    private readonly PgDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public PgRoleRepository(PgDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<Role?> GetByNameAsync(string name)
    {
        return await _dbContext.Roles.FirstOrDefaultAsync(r => EF.Functions.ILike(r.Name, name));
    }

    public async Task<Role?> GetByIdAsync(Guid id)
    {
        return await _dbContext.Roles.FindAsync(id);
    }

    public async Task<IEnumerable<Role>> GetAllAsync()
    {
        return await _dbContext.Roles.OrderBy(r => r.Name).ToListAsync();
    }

    public async Task<Guid> CreateAsync(string name, string? description)
    {
        var role = new Role
        {
            Name = name,
            Description = description,
            CreatedAt = _timeProvider.Now()
        };

        await _dbContext.Roles.AddAsync(role);
        await _dbContext.SaveChangesAsync();

        return role.Id;
    }

    public async Task<bool> ExistsAsync(string name)
    {
        return await _dbContext.Roles
            .AnyAsync(r => EF.Functions.ILike(r.Name, name));
    }

    public async Task<bool> ExistsByIdAsync(Guid id)
    {
        return await _dbContext.Roles.AnyAsync(r => r.Id == id);
    }

    public async Task UpdateAsync(Guid id, string name, string? description)
    {
        var role = await _dbContext.Roles.FindAsync(id);
        if (role == null) throw new EntityNotFoundException($"Role with ID {id} not found", nameof(id));

        role.Name = name;
        role.Description = description;
        role.UpdatedAt = _timeProvider.Now();

        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var role = await _dbContext.Roles.FindAsync(id);
        if (role == null) throw new EntityNotFoundException($"Role with ID {id} not found", nameof(id));

        _dbContext.Roles.Remove(role);
        await _dbContext.SaveChangesAsync();
    }
}