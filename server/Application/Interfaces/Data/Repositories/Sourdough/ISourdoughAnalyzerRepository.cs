using Core.Entities;

namespace Application.Interfaces.Data.Repositories.Sourdough;

public interface ISourdoughAnalyzerRepository
{
    Task<SourdoughAnalyzer?> GetByMacAddressAsync(string macAddress);
    Task<SourdoughAnalyzer?> GetByActivationCodeAsync(string activationCode);
    Task<Guid> SaveAsync(SourdoughAnalyzer analyzer);
    Task<IEnumerable<SourdoughAnalyzer>> GetByUserIdAsync(Guid userId);
}