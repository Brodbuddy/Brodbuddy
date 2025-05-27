using Application.Interfaces.Data;
using Application.Interfaces.Data.Repositories;
using Application.Interfaces.Data.Repositories.Auth;
using Application.Interfaces.Data.Repositories.Sourdough;
using Application.Models.DTOs;
using Application.Models.Results;
using Application.Services.Sourdough;
using Core.Entities;
using Core.Exceptions;
using Core.Extensions;
using Moq;
using SharedTestDependencies.Extensions;
using SharedTestDependencies.Fakes;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Application.Tests.Services;

public class SourdoughAnalyzerServiceTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly Mock<ISourdoughAnalyzerRepository> _analyzerRepositoryMock;
    private readonly Mock<IUserAnalyzerRepository> _userAnalyzerRepositoryMock;
    private readonly Mock<IUserIdentityRepository> _userRepositoryMock;
    private readonly Mock<ITransactionManager> _transactionManagerMock;
    private readonly Mock<IFirmwareRepository> _firmwareRepositoryMock;
    private readonly FakeTimeProvider _timeProvider;
    private readonly SourdoughAnalyzerService _service;

    private SourdoughAnalyzerServiceTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _analyzerRepositoryMock = new Mock<ISourdoughAnalyzerRepository>();
        _userAnalyzerRepositoryMock = new Mock<IUserAnalyzerRepository>();
        _userRepositoryMock = new Mock<IUserIdentityRepository>();
        _transactionManagerMock = new Mock<ITransactionManager>();
        _firmwareRepositoryMock = new Mock<IFirmwareRepository>();
        _timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        
        _transactionManagerMock
            .Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task<RegisterAnalyzerResult>>>()))
            .Returns<Func<Task<RegisterAnalyzerResult>>>(func => func());
        
        _service = new SourdoughAnalyzerService(
            _analyzerRepositoryMock.Object,
            _userAnalyzerRepositoryMock.Object,
            _userRepositoryMock.Object,
            _firmwareRepositoryMock.Object,
            _transactionManagerMock.Object,
            _timeProvider);
    }

    public class RegisterAnalyzerAsync(ITestOutputHelper outputHelper) : SourdoughAnalyzerServiceTests(outputHelper)
    {
        [Fact]
        public async Task RegisterAnalyzerAsync_WithValidInput_RegistersAnalyzerSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var analyzerId = Guid.NewGuid();
            var activationCode = "ABCD1234EFGH";
            var input = new RegisterAnalyzerInput(userId, activationCode, "My Analyzer");
            
            var analyzer = new SourdoughAnalyzer
            {
                Id = analyzerId,
                ActivationCode = activationCode,
                IsActivated = false,
                UserAnalyzers = new List<UserAnalyzer>()
            };
            
            _userRepositoryMock.Setup(x => x.ExistsAsync(userId)).ReturnsAsync(true);
            _analyzerRepositoryMock.Setup(x => x.GetByActivationCodeAsync(activationCode)).ReturnsAsync(analyzer);
            _userAnalyzerRepositoryMock.Setup(x => x.SaveAsync(It.IsAny<UserAnalyzer>())).ReturnsAsync(Guid.NewGuid());
            
            // Act
            var result = await _service.RegisterAnalyzerAsync(input);
            
            // Assert
            result.ShouldNotBeNull();
            result.Analyzer.ShouldBe(analyzer);
            analyzer.IsActivated.ShouldBeTrue();
            analyzer.ActivatedAt.ShouldNotBeNull();
            analyzer.ActivatedAt.Value.ShouldBeWithinTolerance(_timeProvider.GetUtcNow().DateTime, TimeSpan.FromSeconds(1).TotalSeconds);
            result.UserAnalyzer.UserId.ShouldBe(userId);
            result.UserAnalyzer.AnalyzerId.ShouldBe(analyzerId);
            result.UserAnalyzer.IsOwner.ShouldBe(true);
            result.UserAnalyzer.Nickname.ShouldBe("My Analyzer");
        }

        [Fact]
        public async Task RegisterAnalyzerAsync_WithDashes_NormalizesCode()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var activationCode = "ABCD-1234-EFGH";
            var normalizedCode = "ABCD1234EFGH";
            var input = new RegisterAnalyzerInput(userId, activationCode, "My Analyzer");
            
            var analyzer = new SourdoughAnalyzer
            {
                Id = Guid.NewGuid(),
                ActivationCode = normalizedCode,
                IsActivated = false,
                UserAnalyzers = new List<UserAnalyzer>()
            };
            
            _userRepositoryMock.Setup(x => x.ExistsAsync(userId)).ReturnsAsync(true);
            _analyzerRepositoryMock.Setup(x => x.GetByActivationCodeAsync(normalizedCode)).ReturnsAsync(analyzer);
            _userAnalyzerRepositoryMock.Setup(x => x.SaveAsync(It.IsAny<UserAnalyzer>())).ReturnsAsync(Guid.NewGuid());
            
            // Act
            await _service.RegisterAnalyzerAsync(input);
            
            // Assert
            _analyzerRepositoryMock.Verify(x => x.GetByActivationCodeAsync(normalizedCode), Times.Once);
        }

        [Fact]
        public async Task RegisterAnalyzerAsync_AsSecondUser_SetsIsOwnerToFalse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var existingUserId = Guid.NewGuid();
            var analyzerId = Guid.NewGuid();
            var activationCode = "ABCD1234EFGH";
            var input = new RegisterAnalyzerInput(userId, activationCode, "My Analyzer");
            
            var analyzer = new SourdoughAnalyzer
            {
                Id = analyzerId,
                ActivationCode = activationCode,
                IsActivated = false,
                ActivatedAt = null,
                UserAnalyzers = new List<UserAnalyzer>
                {
                    new() { UserId = existingUserId, AnalyzerId = analyzerId, IsOwner = true }
                }
            };
            
            UserAnalyzer capturedUserAnalyzer = null!;
            _userRepositoryMock.Setup(x => x.ExistsAsync(userId)).ReturnsAsync(true);
            _analyzerRepositoryMock.Setup(x => x.GetByActivationCodeAsync(activationCode)).ReturnsAsync(analyzer);
            _userAnalyzerRepositoryMock.Setup(x => x.SaveAsync(It.IsAny<UserAnalyzer>()))
                .Callback<UserAnalyzer>(ua => capturedUserAnalyzer = ua)
                .ReturnsAsync(Guid.NewGuid());
            
            // Act
            await _service.RegisterAnalyzerAsync(input);
            
            // Assert
            capturedUserAnalyzer.IsOwner.ShouldBe(false);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("   ")]
        public async Task RegisterAnalyzerAsync_WithInvalidActivationCode_ThrowsArgumentException(string invalidCode)
        {
            // Arrange
            var input = new RegisterAnalyzerInput(Guid.NewGuid(), invalidCode, "My Analyzer");
            
            // Act & Assert
            await Should.ThrowAsync<ArgumentException>(() => _service.RegisterAnalyzerAsync(input));
        }

        [Fact]
        public async Task RegisterAnalyzerAsync_WithNonExistentUser_ThrowsEntityNotFoundException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var input = new RegisterAnalyzerInput(userId, "ABCD1234EFGH", "My Analyzer");
            
            _userRepositoryMock.Setup(x => x.ExistsAsync(userId)).ReturnsAsync(false);
            
            // Act & Assert
            await Should.ThrowAsync<EntityNotFoundException>(() => _service.RegisterAnalyzerAsync(input));
        }

        [Fact]
        public async Task RegisterAnalyzerAsync_WithInvalidActivationCode_ThrowsEntityNotFoundException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var activationCode = "INVALIDCODE1";
            var input = new RegisterAnalyzerInput(userId, activationCode, "My Analyzer");
            
            _userRepositoryMock.Setup(x => x.ExistsAsync(userId)).ReturnsAsync(true);
            _analyzerRepositoryMock.Setup(x => x.GetByActivationCodeAsync(activationCode)).ReturnsAsync((SourdoughAnalyzer)null!);
            
            // Act & Assert
            await Should.ThrowAsync<EntityNotFoundException>(() => _service.RegisterAnalyzerAsync(input));
        }

        [Fact]
        public async Task RegisterAnalyzerAsync_WithAlreadyActivatedAnalyzer_ThrowsBusinessRuleViolationException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var analyzerId = Guid.NewGuid();
            var activationCode = "ABCD1234EFGH";
            var input = new RegisterAnalyzerInput(userId, activationCode, "My Analyzer");
            
            var analyzer = new SourdoughAnalyzer
            {
                Id = analyzerId,
                ActivationCode = activationCode,
                IsActivated = true,
                UserAnalyzers = new List<UserAnalyzer>()
            };
            
            _userRepositoryMock.Setup(x => x.ExistsAsync(userId)).ReturnsAsync(true);
            _analyzerRepositoryMock.Setup(x => x.GetByActivationCodeAsync(activationCode)).ReturnsAsync(analyzer);
            
            // Act & Assert
            await Should.ThrowAsync<BusinessRuleViolationException>(() => _service.RegisterAnalyzerAsync(input));
        }

        [Fact]
        public async Task RegisterAnalyzerAsync_UserAlreadyRegistered_ThrowsBusinessRuleViolationException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var analyzerId = Guid.NewGuid();
            var activationCode = "ABCD1234EFGH";
            var input = new RegisterAnalyzerInput(userId, activationCode, "My Analyzer");
            
            var analyzer = new SourdoughAnalyzer
            {
                Id = analyzerId,
                ActivationCode = activationCode,
                IsActivated = false,
                UserAnalyzers = new List<UserAnalyzer>
                {
                    new() { UserId = userId, AnalyzerId = analyzerId }
                }
            };
            
            _userRepositoryMock.Setup(x => x.ExistsAsync(userId)).ReturnsAsync(true);
            _analyzerRepositoryMock.Setup(x => x.GetByActivationCodeAsync(activationCode)).ReturnsAsync(analyzer);
            
            // Act & Assert
            await Should.ThrowAsync<BusinessRuleViolationException>(() => _service.RegisterAnalyzerAsync(input));
        }
    }

    public class GetUserAnalyzersAsync(ITestOutputHelper outputHelper) : SourdoughAnalyzerServiceTests(outputHelper)
    {
        [Fact]
        public async Task GetUserAnalyzersAsync_WithValidUserId_ReturnsAnalyzers()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var analyzers = new List<SourdoughAnalyzer>
            {
                new() { Id = Guid.NewGuid(), Name = "Analyzer 1" },
                new() { Id = Guid.NewGuid(), Name = "Analyzer 2" }
            };
            
            _analyzerRepositoryMock.Setup(x => x.GetByUserIdAsync(userId)).ReturnsAsync(analyzers);
            
            // Act
            var result = await _service.GetUserAnalyzersAsync(userId);
            
            // Assert
            var analyzerWithUpdateStatusEnumerable = result.ToList();
            analyzerWithUpdateStatusEnumerable.ShouldNotBeNull();
            analyzerWithUpdateStatusEnumerable.Count.ShouldBe(2);
        }

        [Fact]
        public async Task GetUserAnalyzersAsync_WithNoAnalyzers_ReturnsEmptyList()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _analyzerRepositoryMock.Setup(x => x.GetByUserIdAsync(userId)).ReturnsAsync(new List<SourdoughAnalyzer>());
            
            // Act
            var result = await _service.GetUserAnalyzersAsync(userId);
            
            // Assert
            var analyzerWithUpdateStatusEnumerable = result.ToList();
            analyzerWithUpdateStatusEnumerable.ShouldNotBeNull();
            analyzerWithUpdateStatusEnumerable.ShouldBeEmpty();
        }
    }

    public class GetAllAnalyzersAsync(ITestOutputHelper outputHelper) : SourdoughAnalyzerServiceTests(outputHelper)
    {
        [Fact]
        public async Task GetAllAnalyzersAsync_ReturnsAllAnalyzers()
        {
            // Arrange
            var analyzers = new List<SourdoughAnalyzer>
            {
                new() { Id = Guid.NewGuid(), Name = "Analyzer 1" },
                new() { Id = Guid.NewGuid(), Name = "Analyzer 2" },
                new() { Id = Guid.NewGuid(), Name = "Analyzer 3" }
            };
            
            _analyzerRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(analyzers);
            
            // Act
            var result = await _service.GetAllAnalyzersAsync();
            
            // Assert
            var sourdoughAnalyzers = result.ToList();
            sourdoughAnalyzers.ShouldNotBeNull();
            sourdoughAnalyzers.Count.ShouldBe(3);
        }

        [Fact]
        public async Task GetAllAnalyzersAsync_WithNoAnalyzers_ReturnsEmptyList()
        {
            // Arrange
            _analyzerRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<SourdoughAnalyzer>());
            
            // Act
            var result = await _service.GetAllAnalyzersAsync();
            
            // Assert
            var sourdoughAnalyzers = result.ToList();
            sourdoughAnalyzers.ShouldNotBeNull();
            sourdoughAnalyzers.ShouldBeEmpty();
        }
    }

    public class CreateAnalyzerAsync(ITestOutputHelper outputHelper) : SourdoughAnalyzerServiceTests(outputHelper)
    {
        [Fact]
        public async Task CreateAnalyzerAsync_WithValidInput_CreatesAnalyzer()
        {
            // Arrange
            var macAddress = "aa:bb:cc:dd:ee:ff";
            var name = "Test Analyzer";
            SourdoughAnalyzer capturedAnalyzer = null!;
            
            _analyzerRepositoryMock.Setup(x => x.SaveAsync(It.IsAny<SourdoughAnalyzer>()))
                .Callback<SourdoughAnalyzer>(a => capturedAnalyzer = a)
                .ReturnsAsync(Guid.NewGuid());
            
            // Act
            var result = await _service.CreateAnalyzerAsync(macAddress, name);
            
            // Assert
            result.ShouldNotBeNull();
            result.MacAddress.ShouldBe("AA:BB:CC:DD:EE:FF");
            result.Name.ShouldBe(name);
            result.ActivationCode.ShouldNotBeNullOrEmpty();
            result.ActivationCode.Length.ShouldBe(12);
            result.IsActivated.ShouldBeFalse();
            result.CreatedAt.ShouldBeWithinTolerance(_timeProvider.GetUtcNow().DateTime, TimeSpan.FromSeconds(1).TotalSeconds);
            result.UpdatedAt.ShouldBeWithinTolerance(_timeProvider.GetUtcNow().DateTime, TimeSpan.FromSeconds(1).TotalSeconds);
        }

        [Fact]
        public async Task CreateAnalyzerAsync_NormalizesMacAddress()
        {
            // Arrange
            var macAddress = "aa:bb:cc:dd:ee:ff";
            var name = "Test Analyzer";
            
            _analyzerRepositoryMock.Setup(x => x.SaveAsync(It.IsAny<SourdoughAnalyzer>())).ReturnsAsync(Guid.NewGuid());
            
            // Act
            var result = await _service.CreateAnalyzerAsync(macAddress, name);
            
            // Assert
            result.MacAddress.ShouldBe("AA:BB:CC:DD:EE:FF");
        }
    }

    public class GenerateActivationCode(ITestOutputHelper outputHelper) : SourdoughAnalyzerServiceTests(outputHelper)
    {
        [Fact]
        public void GenerateActivationCode_GeneratesValidCode()
        {
            // Act
            var code = SourdoughAnalyzerService.GenerateActivationCode();
            
            // Assert
            code.ShouldNotBeNullOrEmpty();
            code.Length.ShouldBe(12);
            code.ShouldMatch("^[A-Z0-9]+$");
        }

        [Fact]
        public void GenerateActivationCode_GeneratesUniqueCodes()
        {
            // Arrange
            var generatedCodes = new HashSet<string>();
            
            // Act
            for (int i = 0; i < 1000; i++)
            {
                var code = SourdoughAnalyzerService.GenerateActivationCode();
                generatedCodes.Add(code);
            }
            
            // Assert
            generatedCodes.Count.ShouldBe(1000);
        }
    }

    public class FormatActivationCodeForDisplay(ITestOutputHelper outputHelper) : SourdoughAnalyzerServiceTests(outputHelper)
    {
        [Theory]
        [InlineData("ABCD1234EFGH", "ABCD-1234-EFGH")]
        [InlineData("123456789012", "1234-5678-9012")]
        public void FormatActivationCodeForDisplay_WithValidCode_FormatsCorrectly(string input, string expected)
        {
            // Act
            var result = SourdoughAnalyzerService.FormatActivationCodeForDisplay(input);
            
            // Assert
            result.ShouldBe(expected);
        }

        [Theory]
        [InlineData("")]
        [InlineData("ABC")]
        [InlineData("ABCD1234EFGH1")]
        public void FormatActivationCodeForDisplay_WithInvalidLength_ReturnsOriginal(string input)
        {
            // Act
            var result = SourdoughAnalyzerService.FormatActivationCodeForDisplay(input);
            
            // Assert
            result.ShouldBe(input);
        }

        [Fact]
        public void FormatActivationCodeForDisplay_WithNull_ReturnsNull()
        {
            // Act
            var result = SourdoughAnalyzerService.FormatActivationCodeForDisplay(null!);
            
            // Assert
            result.ShouldBeNull();
        }
    }
}