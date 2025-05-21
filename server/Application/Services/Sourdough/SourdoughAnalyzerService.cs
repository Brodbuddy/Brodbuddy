using System.Security.Cryptography;
using Application.Interfaces.Data;
using Application.Interfaces.Data.Repositories.Auth;
using Application.Interfaces.Data.Repositories.Sourdough;
using Application.Models.DTOs;
using Application.Models.Results;
using Core.Entities;
using Core.Exceptions;
using Core.Extensions;

namespace Application.Services.Sourdough;

public interface ISourdoughAnalyzerService
{
    Task<RegisterAnalyzerResult> RegisterAnalyzerAsync(RegisterAnalyzerInput input);
    Task<IEnumerable<SourdoughAnalyzer>> GetUserAnalyzersAsync(Guid userId);
    Task<SourdoughAnalyzer> CreateAnalyzerAsync(string macAddress, string name);
}

public class SourdoughAnalyzerService : ISourdoughAnalyzerService
{
    private readonly ISourdoughAnalyzerRepository _analyzerRepository;
    private readonly IUserAnalyzerRepository _userAnalyzerRepository;
    private readonly IUserIdentityRepository _userRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly TimeProvider _timeProvider;

    public SourdoughAnalyzerService(
        ISourdoughAnalyzerRepository analyzerRepository,
        IUserAnalyzerRepository userAnalyzerRepository,
        IUserIdentityRepository userRepository,
        ITransactionManager transactionManager,
        TimeProvider timeProvider)
    {
        _analyzerRepository = analyzerRepository;
        _userAnalyzerRepository = userAnalyzerRepository;
        _userRepository = userRepository;
        _transactionManager = transactionManager;
        _timeProvider = timeProvider;
    }

    public async Task<RegisterAnalyzerResult> RegisterAnalyzerAsync(RegisterAnalyzerInput input)
    {
        if (string.IsNullOrWhiteSpace(input.ActivationCode)) throw new ArgumentException("Activation code is required");
        if (!await _userRepository.ExistsAsync(input.UserId)) throw new EntityNotFoundException("User not found");

        var normalizedCode = input.ActivationCode.ToUpperInvariant();
        
        return await _transactionManager.ExecuteInTransactionAsync(async () =>
        {
            var analyzer = await _analyzerRepository.GetByActivationCodeAsync(normalizedCode);
            
            if (analyzer == null) throw new EntityNotFoundException("Invalid activation code");
                
            if (analyzer.IsActivated) throw new BusinessRuleViolationException("This analyzer has already been activated");
            
            var existingUserAnalyzer = analyzer.UserAnalyzers.FirstOrDefault(ua => ua.UserId == input.UserId);
            if (existingUserAnalyzer != null) throw new BusinessRuleViolationException("You have already registered this analyzer");
            
            analyzer.IsActivated = true;
            analyzer.ActivatedAt = _timeProvider.Now();
            analyzer.UpdatedAt = _timeProvider.Now();
            
            var userAnalyzer = new UserAnalyzer
            {
                UserId = input.UserId,
                AnalyzerId = analyzer.Id,
                IsOwner = analyzer.UserAnalyzers.Count == 0,
                Nickname = input.Nickname,
                CreatedAt = _timeProvider.Now()
            };
            
            await _userAnalyzerRepository.SaveAsync(userAnalyzer);
            
            return new RegisterAnalyzerResult(analyzer, userAnalyzer, false);
        });
    }

    public async Task<IEnumerable<SourdoughAnalyzer>> GetUserAnalyzersAsync(Guid userId)
    {
        return await _analyzerRepository.GetByUserIdAsync(userId);
    }

    public async Task<SourdoughAnalyzer> CreateAnalyzerAsync(string macAddress, string name)
    {
        var activationCode = GenerateActivationCode();
        var analyzer = new SourdoughAnalyzer
        {
            MacAddress = macAddress.ToUpperInvariant(),
            Name = name,
            ActivationCode = activationCode,
            IsActivated = false,
            CreatedAt = _timeProvider.Now(),
            UpdatedAt = _timeProvider.Now()
        };
        
        await _analyzerRepository.SaveAsync(analyzer);
        return analyzer;
    }
    
    public static string GenerateActivationCode()
    {
        const string charset = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        const int codeLength = 12;
        
        var codeBytes = new byte[codeLength];
        var result = new char[codeLength + 2];
        
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(codeBytes);
        }
        
        int resultIndex = 0;
        for (int i = 0; i < codeLength; i++)
        {
            result[resultIndex++] = charset[codeBytes[i] % charset.Length];
            if (i == 3 || i == 7)
            {
                result[resultIndex++] = '-';
            }
        }
        
        return new string(result);
    }
}