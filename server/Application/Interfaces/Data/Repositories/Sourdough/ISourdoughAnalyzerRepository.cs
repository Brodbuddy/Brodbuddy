using Core.Entities;

namespace Application.Interfaces.Data.Repositories.Sourdough;

public interface ISourdoughAnalyzerRepository
{
    Task<SourdoughAnalyzer?> GetByMacAddressAsync(string macAddress);
    Task<SourdoughAnalyzer?> GetByActivationCodeAsync(string activationCode);
    Task<SourdoughAnalyzer?> GetByIdAsync(Guid id);
    Task<Guid> SaveAsync(SourdoughAnalyzer analyzer);
    Task UpdateAsync(SourdoughAnalyzer analyzer);
    Task<IEnumerable<SourdoughAnalyzer>> GetByUserIdAsync(Guid userId);
    Task<IEnumerable<SourdoughAnalyzer>> GetAllAsync();
    Task<Guid?> GetOwnersUserIdAsync(Guid analyzerId);
}