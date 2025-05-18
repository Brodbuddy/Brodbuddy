using Core.Entities;

namespace Application.Interfaces.Data.Repositories;

public interface IRoleRepository
{
    Task<Role?> GetByNameAsync(string name);
    Task<Role?> GetByIdAsync(Guid id);
    Task<IEnumerable<Role>> GetAllAsync();
    Task<Guid> CreateAsync(string name, string? description);
    Task<bool> ExistsAsync(string name);
    Task<bool> ExistsByIdAsync(Guid id);
    Task UpdateAsync(Guid id, string name, string? description);
    Task DeleteAsync(Guid id);
}