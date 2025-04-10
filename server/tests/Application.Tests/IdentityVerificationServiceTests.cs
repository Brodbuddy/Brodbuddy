using Application.Interfaces;
using Application.Services;
using Core.Entities;
using Moq;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

namespace Application.Tests;

public class IdentityVerificationServiceTests
{
    private readonly Mock<IOtpService> _mockOtpService;
    private readonly Mock<IUserIdentityService> _mockUserIdentityService;
    private readonly Mock<IEmailSender> _mockEmailSender;
    private readonly Mock<ILogger<IdentityVerificationService>> _mockLogger;
    private readonly Mock<IIdentityVerificationRepository> _mockRepository;
    private readonly IdentityVerificationService _service;

    private const string TestEmail = "test@example.com";
    private const int TestCode = 123456;
    private static Guid TestOtpId { get; } = Guid.NewGuid();
    private static Guid TestUserId { get; } = Guid.NewGuid();

    public IdentityVerificationServiceTests()
    {
        _mockOtpService = new Mock<IOtpService>();
        _mockUserIdentityService = new Mock<IUserIdentityService>();
        _mockEmailSender = new Mock<IEmailSender>();
        _mockRepository = new Mock<IIdentityVerificationRepository>();
        _mockLogger = new Mock<ILogger<IdentityVerificationService>>();

        _service = new IdentityVerificationService(
            _mockOtpService.Object,
            _mockUserIdentityService.Object,
            _mockEmailSender.Object,
            _mockRepository.Object,
            _mockLogger.Object);
    }

    public class SendCodeAsync : IdentityVerificationServiceTests
    {
        [Fact]
        public async Task SendCodeAsync_Success_ReturnsTrue()
        {
            // Arrange
            _mockUserIdentityService.Setup(s => s.CreateAsync(TestEmail))
                .ReturnsAsync(TestUserId);

            _mockOtpService.Setup(s => s.GenerateAsync())
                .ReturnsAsync(TestCode);

            _mockOtpService.Setup(s => s.GetLatestAsync())
                .ReturnsAsync(new OneTimePassword { Id = TestOtpId });

            _mockRepository.Setup(r => r.CreateAsync(TestUserId, TestOtpId))
                .ReturnsAsync(Guid.NewGuid());

            _mockEmailSender.Setup(e => e.SendEmailAsync(
                    TestEmail,
                    "Verification Code",
                    $"Your verification code is: {TestCode}"))
                .ReturnsAsync(true);

            // Act
            var result = await _service.SendCodeAsync(TestEmail);

            // Assert
            result.ShouldBeTrue();
            _mockUserIdentityService.Verify(s => s.CreateAsync(TestEmail), Times.Once);
            _mockOtpService.Verify(s => s.GenerateAsync(), Times.Once);
            _mockOtpService.Verify(s => s.GetLatestAsync(), Times.Once);
            _mockRepository.Verify(r => r.CreateAsync(TestUserId, TestOtpId), Times.Once);
            _mockEmailSender.Verify(e => e.SendEmailAsync(
                    TestEmail,
                    "Verification Code",
                    $"Your verification code is: {TestCode}"),
                Times.Once);
        }

        [Fact]
        public async Task SendCodeAsync_NullOtp_ReturnsFalse()
        {
            // Arrange
            _mockUserIdentityService.Setup(s => s.CreateAsync(TestEmail))
                .ReturnsAsync(TestUserId);

            _mockOtpService.Setup(s => s.GenerateAsync())
                .ReturnsAsync(TestCode);

            _mockOtpService.Setup(s => s.GetLatestAsync())
                .ReturnsAsync((OneTimePassword?)null);

            // Act
            var result = await _service.SendCodeAsync(TestEmail);

            // Assert
            result.ShouldBeFalse();
            _mockRepository.Verify(r => r.CreateAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
            _mockEmailSender.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task SendCodeAsync_EmailFailure_ReturnsFalse()
        {
            // Arrange
            _mockUserIdentityService.Setup(s => s.CreateAsync(TestEmail))
                .ReturnsAsync(TestUserId);

            _mockOtpService.Setup(s => s.GenerateAsync())
                .ReturnsAsync(TestCode);

            _mockOtpService.Setup(s => s.GetLatestAsync())
                .ReturnsAsync(new OneTimePassword { Id = TestOtpId });

            _mockRepository.Setup(r => r.CreateAsync(TestUserId, TestOtpId))
                .ReturnsAsync(Guid.NewGuid());

            _mockEmailSender.Setup(e => e.SendEmailAsync(
                    TestEmail,
                    "Verification Code",
                    It.IsAny<string>()))
                .ReturnsAsync(false);

            // Act
            var result = await _service.SendCodeAsync(TestEmail);

            // Assert
            result.ShouldBeFalse();
        }

        [Fact]
        public async Task SendCodeAsync_Exception_ReturnsFalse()
        {
            // Arrange
            _mockUserIdentityService.Setup(s => s.CreateAsync(TestEmail))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _service.SendCodeAsync(TestEmail);

            // Assert
            result.ShouldBeFalse();
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

            _mockUserIdentityService.Setup(s => s.GetAsync(TestEmail))
                .ReturnsAsync(user);

            _mockRepository.Setup(r => r.GetLatestByUserIdAsync(TestUserId))
                .ReturnsAsync(verificationContext);

            _mockOtpService.Setup(o => o.IsValidAsync(TestOtpId, TestCode))
                .ReturnsAsync(true);

            _mockOtpService.Setup(o => o.MarkAsUsedAsync(TestOtpId))
                .ReturnsAsync(true);

            // Act
            var result = await _service.TryVerifyCodeAsync(TestEmail, TestCode);

            // Assert
            result.verified.ShouldBeTrue();
            result.userId.ShouldBe(TestUserId);
            _mockOtpService.Verify(o => o.MarkAsUsedAsync(TestOtpId), Times.Once);
        }

        [Fact]
        public async Task TryVerifyCodeAsync_NoVerificationContext_ReturnsFailure()
        {
            // Arrange
            var user = new User { Id = TestUserId, Email = TestEmail };

            _mockUserIdentityService.Setup(s => s.GetAsync(TestEmail))
                .ReturnsAsync(user);

            _mockRepository.Setup(r => r.GetLatestByUserIdAsync(TestUserId))
                .ReturnsAsync((VerificationContext?)null);

            // Act
            var result = await _service.TryVerifyCodeAsync(TestEmail, TestCode);

            // Assert
            result.verified.ShouldBeFalse();
            result.userId.ShouldBe(Guid.Empty);
            _mockOtpService.Verify(o => o.IsValidAsync(It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
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

            _mockUserIdentityService.Setup(s => s.GetAsync(TestEmail))
                .ReturnsAsync(user);

            _mockRepository.Setup(r => r.GetLatestByUserIdAsync(TestUserId))
                .ReturnsAsync(verificationContext);

            _mockOtpService.Setup(o => o.IsValidAsync(TestOtpId, TestCode))
                .ReturnsAsync(false);

            // Act
            var result = await _service.TryVerifyCodeAsync(TestEmail, TestCode);

            // Assert
            result.verified.ShouldBeFalse();
            result.userId.ShouldBe(Guid.Empty);
            _mockOtpService.Verify(o => o.MarkAsUsedAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task TryVerifyCodeAsync_Exception_ReturnsFailure()
        {
            // Arrange
            _mockUserIdentityService.Setup(s => s.GetAsync(TestEmail))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _service.TryVerifyCodeAsync(TestEmail, TestCode);

            // Assert
            result.verified.ShouldBeFalse();
            result.userId.ShouldBe(Guid.Empty);
        }
    }
}