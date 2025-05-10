using Application.Interfaces;
using Application.Interfaces.Data.Repositories;
using Application.Services;
using Application.Tests.Fakes;
using Core.Entities;
using Moq;
using Shouldly;
using Xunit;

namespace Application.Tests.Services;

public class IdentityVerificationServiceTests
{
    private readonly Mock<IOtpService> _otpServiceMock;
    private readonly Mock<IUserIdentityService> _userIdentityServiceMock;
    private readonly Mock<IIdentityVerificationRepository> _repositoryMock;
    private readonly Mock<ITransactionManager> _transactionManagerMock;
    private readonly IdentityVerificationService _identityVerificationService;
    private readonly FakeEmailSender _emailSender;

    
    private const string TestEmail = "test@example.com";
    private const int TestCode = 123456;
    private static Guid TestOtpId { get; } = Guid.NewGuid();
    private static Guid TestUserId { get; } = Guid.NewGuid();

    protected IdentityVerificationServiceTests()
    {
        _otpServiceMock = new Mock<IOtpService>();
        _userIdentityServiceMock = new Mock<IUserIdentityService>();
        _emailSender = new FakeEmailSender();
        _repositoryMock = new Mock<IIdentityVerificationRepository>();
        _transactionManagerMock = new Mock<ITransactionManager>();
        
        _identityVerificationService = new IdentityVerificationService(
            _otpServiceMock.Object,
            _userIdentityServiceMock.Object,
            _emailSender,
            _repositoryMock.Object,
            _transactionManagerMock.Object);
    }

    public class SendCodeAsync : IdentityVerificationServiceTests
    {
        [Fact]
        public async Task SendCodeAsync_Success_ReturnsTrue()
        {
            // Arrange
            _userIdentityServiceMock.Setup(s => s.CreateAsync(TestEmail))
                .ReturnsAsync(TestUserId);


            _otpServiceMock.Setup(s => s.GenerateAsync())
                .ReturnsAsync((TestOtpId, TestCode));

            _repositoryMock.Setup(r => r.CreateAsync(TestUserId, TestOtpId))
                .ReturnsAsync(Guid.NewGuid());

            _transactionManagerMock
                .Setup(tm => tm.ExecuteInTransactionAsync(It.IsAny<Func<Task<bool>>>()))
                .Returns((Func<Task<bool>> func) => func());

            // Act
            var result = await _identityVerificationService.SendCodeAsync(TestEmail);

            // Assert
            result.ShouldBeTrue();
            _userIdentityServiceMock.Verify(s => s.CreateAsync(TestEmail), Times.Once);
            _otpServiceMock.Verify(s => s.GenerateAsync(), Times.Once);
            _repositoryMock.Verify(r => r.CreateAsync(TestUserId, TestOtpId), Times.Once);
            _transactionManagerMock.Verify(tm => tm.ExecuteInTransactionAsync(It.IsAny<Func<Task<bool>>>()), Times.Once);
        }

        [Fact]
        public async Task SendCodeAsync_EmailFailure_ReturnsFalse()
        {
            // Arrange
            _userIdentityServiceMock.Setup(s => s.CreateAsync(TestEmail))
                .ReturnsAsync(TestUserId);

            _otpServiceMock.Setup(s => s.GenerateAsync())
                .ReturnsAsync((TestOtpId, TestCode));


            _repositoryMock.Setup(r => r.CreateAsync(TestUserId, TestOtpId))
                .ReturnsAsync(Guid.NewGuid());

            _emailSender.SimulateFailure = true;
            
            _transactionManagerMock
                .Setup(tm => tm.ExecuteInTransactionAsync(It.IsAny<Func<Task<bool>>>()))
                .Returns((Func<Task<bool>> func) => func());

            // Act
            var result = await _identityVerificationService.SendCodeAsync(TestEmail);

            // Assert
            result.ShouldBeFalse();
            _transactionManagerMock.Verify(tm => tm.ExecuteInTransactionAsync(It.IsAny<Func<Task<bool>>>()), Times.Once);
        }
    }

    public class TryVerifyCodeAsync : IdentityVerificationServiceTests
    {
        [Fact]
        public async Task TryVerifyCodeAsync_ValidCode_ReturnsSuccessAndUserId()
        {
            // Arrange
            var user = new User { Id = TestUserId, Email = TestEmail };
            var verificationContext = new VerificationContext
            {
                Id = Guid.NewGuid(),
                UserId = TestUserId,
                OtpId = TestOtpId
            };

            _userIdentityServiceMock.Setup(s => s.GetAsync(TestEmail))
                .ReturnsAsync(user);

            _repositoryMock.Setup(r => r.GetLatestAsync(TestUserId))
                .ReturnsAsync(verificationContext);

            _otpServiceMock.Setup(o => o.IsValidAsync(TestOtpId, TestCode))
                .ReturnsAsync(true);

            _otpServiceMock.Setup(o => o.MarkAsUsedAsync(TestOtpId))
                .ReturnsAsync(true);
            
            _transactionManagerMock
                .Setup(tm => tm.ExecuteInTransactionAsync(It.IsAny<Func<Task<(bool verified, Guid userId)>>>()))
                .Returns((Func<Task<(bool verified, Guid userId)>> func) => func());

            // Act
            var result = await _identityVerificationService.TryVerifyCodeAsync(TestEmail, TestCode);

            // Assert
            result.verified.ShouldBeTrue();
            result.userId.ShouldBe(TestUserId);
            _otpServiceMock.Verify(o => o.MarkAsUsedAsync(TestOtpId), Times.Once);
            _transactionManagerMock.Verify(tm => tm.ExecuteInTransactionAsync(It.IsAny<Func<Task<(bool verified, Guid userId)>>>()), Times.Once);
        }

        [Fact]
        public async Task TryVerifyCodeAsync_NoVerificationContext_ReturnsFailure()
        {
            // Arrange
            var user = new User { Id = TestUserId, Email = TestEmail };

            _userIdentityServiceMock.Setup(s => s.GetAsync(TestEmail))
                .ReturnsAsync(user);

            _repositoryMock.Setup(r => r.GetLatestAsync(TestUserId))
                .ReturnsAsync((VerificationContext?)null);
            
            _transactionManagerMock
                .Setup(tm => tm.ExecuteInTransactionAsync(It.IsAny<Func<Task<(bool verified, Guid userId)>>>()))
                .Returns((Func<Task<(bool verified, Guid userId)>> func) => func());
            
            // Act
            var result = await _identityVerificationService.TryVerifyCodeAsync(TestEmail, TestCode);

            // Assert
            result.verified.ShouldBeFalse();
            result.userId.ShouldBe(Guid.Empty);
            _otpServiceMock.Verify(o => o.IsValidAsync(It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
            _transactionManagerMock.Verify(tm => tm.ExecuteInTransactionAsync(It.IsAny<Func<Task<(bool verified, Guid userId)>>>()), Times.Once);
        }

        [Fact]
        public async Task TryVerifyCodeAsync_InvalidCode_ReturnsFailure()
        {
            // Arrange
            var user = new User { Id = TestUserId, Email = TestEmail };
            var verificationContext = new VerificationContext
            {
                Id = Guid.NewGuid(),
                UserId = TestUserId,
                OtpId = TestOtpId
            };

            _userIdentityServiceMock.Setup(s => s.GetAsync(TestEmail))
                .ReturnsAsync(user);

            _repositoryMock.Setup(r => r.GetLatestAsync(TestUserId))
                .ReturnsAsync(verificationContext);

            _otpServiceMock.Setup(o => o.IsValidAsync(TestOtpId, TestCode))
                .ReturnsAsync(false);
            
            _transactionManagerMock
                .Setup(tm => tm.ExecuteInTransactionAsync(It.IsAny<Func<Task<(bool verified, Guid userId)>>>()))
                .Returns((Func<Task<(bool verified, Guid userId)>> func) => func());

            // Act
            var result = await _identityVerificationService.TryVerifyCodeAsync(TestEmail, TestCode);

            // Assert
            result.verified.ShouldBeFalse();
            result.userId.ShouldBe(Guid.Empty);
            _otpServiceMock.Verify(o => o.MarkAsUsedAsync(It.IsAny<Guid>()), Times.Never);
            _transactionManagerMock.Verify(tm => tm.ExecuteInTransactionAsync(It.IsAny<Func<Task<(bool verified, Guid userId)>>>()), Times.Once);
        }
    }
}