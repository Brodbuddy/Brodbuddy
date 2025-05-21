using Application.Interfaces.Data.Repositories;
using Application.Interfaces.Data.Repositories.Auth;
using Core.Entities;
using Core.Exceptions;

namespace Application.Services.Auth;

public interface IRoleService
{
    Task<Role> GetByNameAsync(string name);
    Task<Role> GetByIdAsync(Guid id);
    Task<IEnumerable<Role>> GetAllAsync();
    Task<Guid> CreateAsync(string name, string? description = null);
    Task UpdateAsync(Guid id, string name, string? description = null);
    Task DeleteAsync(Guid id);
}

public class RoleService : IRoleService
{
    private readonly IRoleRepository _repository;

    public RoleService(IRoleRepository repository)
    {
        _repository = repository;
    }

    public async Task<Role> GetByNameAsync(string name)
    {
        var role = await _repository.GetByNameAsync(name);
        if (role == null) throw new EntityNotFoundException($"Role '{name}' not found");
        
        return role;
    }

    public async Task<Role> GetByIdAsync(Guid id)
    {
        var role = await _repository.GetByIdAsync(id);
        if (role == null) throw new EntityNotFoundException($"Role with ID {id} not found");
        
        return role;
    }

    public async Task<IEnumerable<Role>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<Guid> CreateAsync(string name, string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        
        if (await _repository.ExistsAsync(name)) throw new BusinessRuleViolationException($"Role '{name}' already exists");

        return await _repository.CreateAsync(name, description);
    }

    public async Task UpdateAsync(Guid id, string name, string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (!await _repository.ExistsByIdAsync(id)) throw new EntityNotFoundException($"Role with ID {id} not found");

        var existingRole = await _repository.GetByNameAsync(name);
        if (existingRole != null && existingRole.Id != id) throw new BusinessRuleViolationException($"Role '{name}' already exists");

        await _repository.UpdateAsync(id, name, description);
    }

    public async Task DeleteAsync(Guid id)
    {
        if (!await _repository.ExistsByIdAsync(id)) throw new EntityNotFoundException($"Role with ID {id} not found");

        await _repository.DeleteAsync(id);
    }
}