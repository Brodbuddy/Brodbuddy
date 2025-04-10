using Core.Entities;
using Infrastructure.Data.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SharedTestDependencies;
using Shouldly;

namespace Infrastructure.Data.Tests;

public class IdentityVerificationRepositoryTest
{
    private readonly PostgresDbContext _dbContext;
    private readonly FakeTimeProvider _timeProvider;
    private readonly Mock<ILogger<IdentityVerificationRepository>> _mockLogger;
    private readonly IdentityVerificationRepository _repository;

    public IdentityVerificationRepositoryTest()
    {
        var options = new DbContextOptionsBuilder<PostgresDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new PostgresDbContext(options);
        _timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        _mockLogger = new Mock<ILogger<IdentityVerificationRepository>>();
        _repository = new IdentityVerificationRepository(_dbContext, _timeProvider, _mockLogger.Object);
    }


    public class CreateAsync : IdentityVerificationRepositoryTest
    {
        [Fact]
        public async Task CreateAsync_ShouldCreateVerificationContext_WhenUserAndOtpExist()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@example.com"
            };
            var otp = new OneTimePassword { Id = Guid.NewGuid() };

            await _dbContext.Users.AddAsync(user);
            await _dbContext.OneTimePasswords.AddAsync(otp);
            await _dbContext.SaveChangesAsync();

            var currentTime = DateTime.UtcNow;
            _timeProvider.SetUtcNow(new DateTimeOffset(currentTime));

            // Act
            var resultId = await _repository.CreateAsync(user.Id, otp.Id);

            // Assert
            resultId.ShouldNotBe(Guid.Empty);

            var context = await _dbContext.VerificationContexts.FindAsync(resultId);
            context.ShouldNotBeNull();
            context.UserId.ShouldBe(user.Id);
            context.OtpId.ShouldBe(otp.Id);
            context.CreatedAt.ShouldBe(currentTime);
        }
        
        [Fact]
        public async Task CreateAsync_ShouldLogError_WhenOtpDoesNotExist()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@example.com"
            };
            var otpId = Guid.NewGuid();

            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();

            // Act & Assert
            var exception = await Should.ThrowAsync<InvalidOperationException>(
                async () => await _repository.CreateAsync(user.Id, otpId));
    
            
            exception.Message.ShouldBe($"OTP with ID {otpId} not found");
            
        }

        [Fact]
        public async Task CreateAsync_ShouldLogError_WhenUserDoesNotExist()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var otpId = Guid.NewGuid();
            var otp = new OneTimePassword { Id = otpId };

            await _dbContext.OneTimePasswords.AddAsync(otp);
            await _dbContext.SaveChangesAsync();

            // Act & Assert
            await Should.ThrowAsync<InvalidOperationException>(
                async () => await _repository.CreateAsync(userId, otpId));
            
        }
        
        [Fact]
        public async Task GetLatestByUserIdAsync_ShouldLogWithContextFound_WhenContextExists()
        {
            // Arrange
            var user = new User { Id = Guid.NewGuid(), Email = "test@example.com" };
            var otp = new OneTimePassword { Id = Guid.NewGuid() };

            await _dbContext.Users.AddAsync(user);
            await _dbContext.OneTimePasswords.AddAsync(otp);
            await _dbContext.SaveChangesAsync();

            var context = new VerificationContext
            {
                UserId = user.Id,
                OtpId = otp.Id,
                CreatedAt = DateTime.UtcNow,
                User = user,
                Otp = otp
            };

            await _dbContext.VerificationContexts.AddAsync(context);
            await _dbContext.SaveChangesAsync();

            // Act
            await _repository.GetLatestByUserIdAsync(user.Id);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Retrieved a latest verification context for user")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetLatestByUserIdAsync_ShouldLogWithNoContextFound_WhenContextDoesNotExist()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            await _repository.GetLatestByUserIdAsync(userId);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Retrieved no latest verification context for user")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
        
        [Fact]
        public async Task CreateAsync_ShouldThrowException_WhenUserDoesNotExist()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var otp = new OneTimePassword { Id = Guid.NewGuid() };

            await _dbContext.OneTimePasswords.AddAsync(otp);
            await _dbContext.SaveChangesAsync();

            // Act & Assert
            await Should.ThrowAsync<InvalidOperationException>(
                async () => await _repository.CreateAsync(userId, otp.Id));
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowException_WhenOtpDoesNotExist()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@example.com"
            };
            var otpId = Guid.NewGuid();

            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();

            // Act & Assert
            await Should.ThrowAsync<InvalidOperationException>(
                async () => await _repository.CreateAsync(user.Id, otpId));
        }
    }

    public class GetLatestByUserIdAsync : IdentityVerificationRepositoryTest
    {
        [Fact]
        public async Task GetLatestByUserIdAsync_ShouldReturnLatestContext_WhenContextsExist()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@example.com"
            };
            var otp1 = new OneTimePassword { Id = Guid.NewGuid() };
            var otp2 = new OneTimePassword { Id = Guid.NewGuid() };

            await _dbContext.Users.AddAsync(user);
            await _dbContext.OneTimePasswords.AddRangeAsync(otp1, otp2);
            await _dbContext.SaveChangesAsync();

            var olderDate = DateTime.UtcNow.AddDays(-2);
            var newerDate = DateTime.UtcNow.AddDays(-1);

            var context1 = new VerificationContext
            {
                UserId = user.Id,
                OtpId = otp1.Id,
                CreatedAt = olderDate,
                User = user,
                Otp = otp1
            };

            var context2 = new VerificationContext
            {
                UserId = user.Id,
                OtpId = otp2.Id,
                CreatedAt = newerDate,
                User = user,
                Otp = otp2
            };

            await _dbContext.VerificationContexts.AddRangeAsync(context1, context2);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _repository.GetLatestByUserIdAsync(user.Id);

            // Assert
            result.ShouldNotBeNull();
            result.OtpId.ShouldBe(otp2.Id);
            result.CreatedAt.ShouldBe(newerDate);
        }

        [Fact]
        public async Task GetLatestByUserIdAsync_ShouldReturnNull_WhenNoContextsExistForUser()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            var result = await _repository.GetLatestByUserIdAsync(userId);

            // Assert
            result.ShouldBeNull();
        }

        [Fact]
        public async Task GetLatestByUserIdAsync_ShouldIncludeRelatedEntities()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@example.com"
            };
            var otp = new OneTimePassword { Id = Guid.NewGuid() };

            await _dbContext.Users.AddAsync(user);
            await _dbContext.OneTimePasswords.AddAsync(otp);
            await _dbContext.SaveChangesAsync();

            var context = new VerificationContext
            {
                UserId = user.Id,
                OtpId = otp.Id,
                CreatedAt = DateTime.UtcNow,
                User = user,
                Otp = otp
            };

            await _dbContext.VerificationContexts.AddAsync(context);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _repository.GetLatestByUserIdAsync(user.Id);

            // Assert
            result.ShouldNotBeNull();
            result.User.ShouldNotBeNull();
            result.User.Id.ShouldBe(user.Id);
            result.Otp.ShouldNotBeNull();
            result.Otp.Id.ShouldBe(otp.Id);
        }
    }
}