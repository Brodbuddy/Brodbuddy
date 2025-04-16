using Core.Entities;
using Infrastructure.Data.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedTestDependencies;
using Shouldly;
using Xunit.Abstractions;

namespace Infrastructure.Data.Tests;

public class PostgresIdentityVerificationRepositoryTest
{
    private readonly PostgresDbContext _dbContext;
    private readonly FakeTimeProvider _timeProvider;
    private readonly PostgresIdentityVerificationRepository _repository;
    private readonly ITestOutputHelper _testOutputHelper;


    public PostgresIdentityVerificationRepositoryTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        var options = new DbContextOptionsBuilder<PostgresDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new PostgresDbContext(options);
        _timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);

        _repository = new PostgresIdentityVerificationRepository(_dbContext, _timeProvider);
    }

    public class CreateAsync : PostgresIdentityVerificationRepositoryTest
    {
        public CreateAsync(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task CreateAsync_WhenUserAndOtpExist_ShouldCreateVerificationContext()
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
        public async Task CreateAsync_WhenCalledMultipleTimes_ShouldGenerateUniqueIds()
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

            // Act
            var resultId1 = await _repository.CreateAsync(user.Id, otp.Id);
            var resultId2 = await _repository.CreateAsync(user.Id, otp.Id);

            // Assert
            resultId1.ShouldNotBe(Guid.Empty);
            resultId2.ShouldNotBe(Guid.Empty);
            resultId1.ShouldNotBe(resultId2);
        }

        [Fact]
        public async Task CreateAsync_WhenCalled_ShouldSaveContextWithCorrectTimestamp()
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

            var expectedTime = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            _timeProvider.SetUtcNow(new DateTimeOffset(expectedTime));

            // Act
            var resultId = await _repository.CreateAsync(user.Id, otp.Id);

            // Assert
            var context = await _dbContext.VerificationContexts.FindAsync(resultId);
            context.ShouldNotBeNull();
            context.CreatedAt.ShouldBe(expectedTime);
        }

        [Fact]
        public async Task CreateAsync_WhenCalledWithSameUser_ShouldCreateMultipleContexts()
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

            // Act
            var resultId1 = await _repository.CreateAsync(user.Id, otp1.Id);
            var resultId2 = await _repository.CreateAsync(user.Id, otp2.Id);

            // Assert
            var contexts = await _dbContext.VerificationContexts
                .Where(vc => vc.UserId == user.Id)
                .ToListAsync();

            contexts.Count.ShouldBe(2);
            contexts.ShouldContain(c => c.Id == resultId1);
            contexts.ShouldContain(c => c.Id == resultId2);
        }
    }

    public class GetLatestAsync : PostgresIdentityVerificationRepositoryTest
    {
        public GetLatestAsync(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task GetLatestAsync_WhenContextsExist_ShouldReturnLatestContext()
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
            var result = await _repository.GetLatestAsync(user.Id);

            // Assert
            result.ShouldNotBeNull();
            result.OtpId.ShouldBe(otp2.Id);
            result.CreatedAt.ShouldBe(newerDate);
        }

        [Fact]
        public async Task GetLatestAsync_WhenNoContextsExistForUser_ShouldReturnNull()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            var result = await _repository.GetLatestAsync(userId);

            // Assert
            result.ShouldBeNull();
        }

        [Fact]
        public async Task GetLatestAsync_WhenContextExists_ShouldIncludeRelatedEntities()
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
            var result = await _repository.GetLatestAsync(user.Id);

            // Assert
            result.ShouldNotBeNull();
            result.User.ShouldNotBeNull();
            result.User.Id.ShouldBe(user.Id);
            result.Otp.ShouldNotBeNull();
            result.Otp.Id.ShouldBe(otp.Id);
        }
    }
}