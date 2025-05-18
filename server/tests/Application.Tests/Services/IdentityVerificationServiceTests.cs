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
    private readonly Mock<IOtpService> _mockOtpService;
    private readonly Mock<IUserIdentityService> _mockUserIdentityService;
    private readonly Mock<IUserRoleService> _mockUserRoleService;
    private readonly FakeEmailSender _emailSender;

    private readonly Mock<IIdentityVerificationRepository> _mockRepository;
    private readonly IdentityVerificationService _service;

    private const string TestEmail = "test@example.com";
    private const int TestCode = 123456;
    private static Guid TestOtpId { get; } = Guid.NewGuid();
    private static Guid TestUserId { get; } = Guid.NewGuid();

    protected IdentityVerificationServiceTests()
    {
        _mockOtpService = new Mock<IOtpService>();
        _mockUserIdentityService = new Mock<IUserIdentityService>();
        _mockUserRoleService = new Mock<IUserRoleService>();
        _emailSender = new FakeEmailSender();
        _mockRepository = new Mock<IIdentityVerificationRepository>();
        
        _service = new IdentityVerificationService(
            _mockOtpService.Object,
            _mockUserIdentityService.Object,
            _mockUserRoleService.Object,
            _emailSender,
            _mockRepository.Object);
    }

    public class SendCodeAsync : IdentityVerificationServiceTests
    {
        [Fact]
        public async Task SendCodeAsync_Success_ReturnsTrue()
        {
            // Arrange
            _mockUserIdentityService.Setup(s => s.CreateAsync(TestEmail))
                .ReturnsAsync(TestUserId);

            _mockUserRoleService.Setup(s => s.GetUserRolesAsync(TestUserId))
                .ReturnsAsync(new List<Role>());

            _mockUserRoleService.Setup(s => s.AssignRoleAsync(TestUserId, Role.Member, null))
                .ReturnsAsync(Guid.NewGuid());

            _mockOtpService.Setup(s => s.GenerateAsync())
                .ReturnsAsync((TestOtpId, TestCode));

            _mockRepository.Setup(r => r.CreateAsync(TestUserId, TestOtpId))
                .ReturnsAsync(Guid.NewGuid());

            // Act
            var result = await _service.SendCodeAsync(TestEmail);

            // Assert
            result.ShouldBeTrue();
            _mockUserIdentityService.Verify(s => s.CreateAsync(TestEmail), Times.Once);
            _mockUserRoleService.Verify(s => s.GetUserRolesAsync(TestUserId), Times.Once);
            _mockUserRoleService.Verify(s => s.AssignRoleAsync(TestUserId, Role.Member, null), Times.Once);
            _mockOtpService.Verify(s => s.GenerateAsync(), Times.Once);
            _mockRepository.Verify(r => r.CreateAsync(TestUserId, TestOtpId), Times.Once);
        }

        [Fact]
        public async Task SendCodeAsync_UserAlreadyHasRole_DoesNotAssignRole()
        {
            // Arrange
            var existingRole = new Role { Id = Guid.NewGuid(), Name = Role.Member };
            _mockUserIdentityService.Setup(s => s.CreateAsync(TestEmail))
                .ReturnsAsync(TestUserId);

            _mockUserRoleService.Setup(s => s.GetUserRolesAsync(TestUserId))
                .ReturnsAsync(new List<Role> { existingRole });

            _mockOtpService.Setup(s => s.GenerateAsync())
                .ReturnsAsync((TestOtpId, TestCode));

            _mockRepository.Setup(r => r.CreateAsync(TestUserId, TestOtpId))
                .ReturnsAsync(Guid.NewGuid());

            // Act
            var result = await _service.SendCodeAsync(TestEmail);

            // Assert
            result.ShouldBeTrue();
            _mockUserIdentityService.Verify(s => s.CreateAsync(TestEmail), Times.Once);
            _mockUserRoleService.Verify(s => s.GetUserRolesAsync(TestUserId), Times.Once);
            _mockUserRoleService.Verify(s => s.AssignRoleAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid?>()), Times.Never);
            _mockOtpService.Verify(s => s.GenerateAsync(), Times.Once);
            _mockRepository.Verify(r => r.CreateAsync(TestUserId, TestOtpId), Times.Once);
        }

        [Fact]
        public async Task SendCodeAsync_EmailFailure_ReturnsFalse()
        {
            // Arrange
            _mockUserIdentityService.Setup(s => s.CreateAsync(TestEmail))
                .ReturnsAsync(TestUserId);

            _mockUserRoleService.Setup(s => s.GetUserRolesAsync(TestUserId))
                .ReturnsAsync(new List<Role>());

            _mockUserRoleService.Setup(s => s.AssignRoleAsync(TestUserId, Role.Member, null))
                .ReturnsAsync(Guid.NewGuid());

            _mockOtpService.Setup(s => s.GenerateAsync())
                .ReturnsAsync((TestOtpId, TestCode));

            _mockRepository.Setup(r => r.CreateAsync(TestUserId, TestOtpId))
                .ReturnsAsync(Guid.NewGuid());

            _emailSender.SimulateFailure = true;

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

            _mockRepository.Setup(r => r.GetLatestAsync(TestUserId))
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

            _mockRepository.Setup(r => r.GetLatestAsync(TestUserId))
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

            _mockRepository.Setup(r => r.GetLatestAsync(TestUserId))
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
    }
}