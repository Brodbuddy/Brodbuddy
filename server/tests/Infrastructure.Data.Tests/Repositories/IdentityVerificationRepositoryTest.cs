using Core.Extensions;
using Infrastructure.Data.Repositories;
using Infrastructure.Data.Tests.Bases;
using Microsoft.EntityFrameworkCore;
using SharedTestDependencies.Constants;
using SharedTestDependencies.Database;
using SharedTestDependencies.Extensions;
using SharedTestDependencies.Fakes;
using Shouldly;

namespace Infrastructure.Data.Tests.Repositories;

[Collection(TestCollections.Database)]
public class IdentityVerificationRepositoryTest : RepositoryTestBase
{
    private readonly FakeTimeProvider _timeProvider;
    private readonly PgIdentityVerificationRepository _repository;

    private IdentityVerificationRepositoryTest(PostgresFixture fixture) :  base(fixture)
    {
        _timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        _repository = new PgIdentityVerificationRepository(DbContext, _timeProvider);
    }

    public class CreateAsync(PostgresFixture fixture) : IdentityVerificationRepositoryTest(fixture)
    {
        [Fact]
        public async Task CreateAsync_WhenUserAndOtpExist_ShouldCreateVerificationContext()
        {
            // Arrange
            var user = await DbContext.SeedUserAsync(_timeProvider);
            var otp = await DbContext.SeedOtpAsync(_timeProvider);
            var expectedTime = _timeProvider.Now(); 

            // Act
            var resultId = await _repository.CreateAsync(user.Id, otp.Id);

            // Assert
            resultId.ShouldNotBe(Guid.Empty);

            var context = await DbContext.VerificationContexts.AsNoTracking().FirstOrDefaultAsync(vc => vc.Id == resultId);
            context.ShouldNotBeNull();
            context.UserId.ShouldBe(user.Id);
            context.OtpId.ShouldBe(otp.Id);
            context.CreatedAt.ShouldBeWithinTolerance(expectedTime);
        }

        
        [Fact]
        public async Task CreateAsync_WhenCalledMultipleTimes_ShouldGenerateUniqueIds()
        {
            // Arrange
            var user1 = await DbContext.SeedUserAsync(_timeProvider);
            var otp1 = await DbContext.SeedOtpAsync(_timeProvider);
            
            var user2 = await DbContext.SeedUserAsync(_timeProvider);
            var otp2 = await DbContext.SeedOtpAsync(_timeProvider);
            
            // Act
            var resultId1 = await _repository.CreateAsync(user1.Id, otp1.Id);
            var resultId2 = await _repository.CreateAsync(user2.Id, otp2.Id);

            // Assert
            resultId1.ShouldNotBe(Guid.Empty);
            resultId2.ShouldNotBe(Guid.Empty);
            resultId1.ShouldNotBe(resultId2);
        }

        [Fact]
        public async Task CreateAsync_WhenUserHasMultipleOtps_ShouldCreateSeparateContexts()
        {
            // Arrange
            var user = await DbContext.SeedUserAsync(_timeProvider);
            var otp1 = await DbContext.SeedOtpAsync(_timeProvider);
            var otp2 = await DbContext.SeedOtpAsync(_timeProvider);
            
            // Act
            var resultId1 = await _repository.CreateAsync(user.Id, otp1.Id);
            var resultId2 = await _repository.CreateAsync(user.Id, otp2.Id);

            // Assert
            var contexts = await DbContext.VerificationContexts
                .Where(vc => vc.UserId == user.Id)
                .ToListAsync();

            contexts.Count.ShouldBe(2);
            contexts.ShouldContain(c => c.Id == resultId1);
            contexts.ShouldContain(c => c.Id == resultId2);
        }
        
        [Fact]
        public async Task CreateAsync_WhenUserAlreadyHasContextForOtp_ShouldThrowDbUpdateException()
        {
            // Arrange
            var user = await DbContext.SeedUserAsync(_timeProvider);
            var otp = await DbContext.SeedOtpAsync(_timeProvider);
    
            // Første oprettelse - 10-4
            await _repository.CreateAsync(user.Id, otp.Id);

            // Act & Assert
            // Anden oprettese - fejler
            await Should.ThrowAsync<DbUpdateException>(() => _repository.CreateAsync(user.Id, otp.Id));
        }
    }

    public class GetLatestAsync(PostgresFixture fixture) : IdentityVerificationRepositoryTest(fixture)
    {
        [Fact]
        public async Task GetLatestAsync_WhenContextsExist_ShouldReturnLatestContext()
        {
            // Arrange
            var user = await DbContext.SeedUserAsync(_timeProvider);
            var otp1 = await DbContext.SeedOtpAsync(_timeProvider, code: 111111);
            var otp2 = await DbContext.SeedOtpAsync(_timeProvider, code: 222222);
            
            _timeProvider.Advance(TimeSpan.FromDays(-2));
            await _repository.CreateAsync(user.Id, otp1.Id);
            
            _timeProvider.Advance(TimeSpan.FromDays(1));
            await _repository.CreateAsync(user.Id, otp2.Id);
            
            // Act
            var result = await _repository.GetLatestAsync(user.Id);

            // Assert
            result.ShouldNotBeNull();
            result.OtpId.ShouldBe(otp2.Id);
            result.CreatedAt.ShouldBeWithinTolerance(_timeProvider.Now());
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
            var user = await DbContext.SeedUserAsync(_timeProvider);
            var otp = await DbContext.SeedOtpAsync(_timeProvider);
            await _repository.CreateAsync(user.Id, otp.Id);

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