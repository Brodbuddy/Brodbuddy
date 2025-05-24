using System.Security.Claims;
using Api.Http.Controllers;
using Api.Http.Models;
using Application.Models.DTOs;
using Application.Models.Results;
using Application.Services.Sourdough;
using Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SharedTestDependencies.Constants;
using Shouldly;

namespace Api.Http.Tests.Controllers;

[Collection(TestCollections.Http)]
public class AnalyzerControllerTests
{
    private readonly Mock<ISourdoughAnalyzerService> _mockAnalyzerService;
    private readonly AnalyzerController _controller;
    private readonly Guid _testUserId = Guid.NewGuid();

    protected AnalyzerControllerTests()
    {
        _mockAnalyzerService = new Mock<ISourdoughAnalyzerService>();
        _controller = new AnalyzerController(_mockAnalyzerService.Object);

        SetupHttpContext();
    }

    // Hjælpemetode til at simulere en HTTP-kontekst med bruger og autorisation (til enhedstest)
    private void SetupHttpContext(bool isAdmin = false)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, _testUserId.ToString())
        };

        if (isAdmin) claims.Add(new Claim(ClaimTypes.Role, Role.Admin));

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext
        {
            User = principal
        };

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        
        var authServiceMock = new Mock<IAuthorizationService>();
        authServiceMock
            .Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(), It.IsAny<string>()))
            .ReturnsAsync(isAdmin ? AuthorizationResult.Success() : AuthorizationResult.Failed());

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(x => x.GetService(typeof(IAuthorizationService)))
            .Returns(authServiceMock.Object);

        httpContext.RequestServices = serviceProviderMock.Object;
    }

    public class RegisterAnalyzer : AnalyzerControllerTests
    {
        [Fact]
        public async Task RegisterAnalyzer_WithValidRequest_ReturnsOkWithResponse()
        {
            // Arrange
            var request = new RegisterAnalyzerRequest("TEST123", "Min Analyzer");
            var serviceResult = new RegisterAnalyzerResult(
                new SourdoughAnalyzer { Id = Guid.NewGuid(), Name = "Test Analyzer" },
                new UserAnalyzer { Nickname = "Kakao analyzer", IsOwner = true },
                true
            );

            _mockAnalyzerService
                .Setup(x => x.RegisterAnalyzerAsync(It.IsAny<RegisterAnalyzerInput>()))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _controller.RegisterAnalyzer(request);

            // Assert
            result.Result.ShouldBeOfType<OkObjectResult>();
            var okResult = (OkObjectResult)result.Result;
            var response = okResult.Value.ShouldBeOfType<RegisterAnalyzerResponse>();

            response.AnalyzerId.ShouldBe(serviceResult.Analyzer.Id);
            response.Nickname.ShouldBe("Kakao analyzer");
            response.IsNewAnalyzer.ShouldBeTrue();
            response.IsOwner.ShouldBeTrue();
        }

        [Fact]
        public async Task RegisterAnalyzer_CallsServiceWithCorrectParameters()
        {
            // Arrange
            var request = new RegisterAnalyzerRequest("TEST123", "Maelke-kakao Analyzer");
            var serviceResult = new RegisterAnalyzerResult(
                new SourdoughAnalyzer { Id = Guid.NewGuid(), Name = "Test Analyzer" },
                new UserAnalyzer { Nickname = "My Analyzer", IsOwner = true },
                true
            );

            _mockAnalyzerService
                .Setup(x => x.RegisterAnalyzerAsync(It.IsAny<RegisterAnalyzerInput>()))
                .ReturnsAsync(serviceResult);

            // Act
            await _controller.RegisterAnalyzer(request);

            // Assert
            _mockAnalyzerService.Verify(x => x.RegisterAnalyzerAsync(
                    It.Is<RegisterAnalyzerInput>(input =>
                        input.UserId == _testUserId &&
                        input.ActivationCode == "TEST123" &&
                        input.Nickname == "Maelke-kakao Analyzer")),
                Times.Once);
        }
    }

    public class GetUserAnalyzers : AnalyzerControllerTests
    {
        [Fact]
        public async Task GetUserAnalyzers_ReturnsOkWithResponse()
        {
            // Arrange
            var analyzers = new List<SourdoughAnalyzer>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Test Analyzer 1",
                    LastSeen = DateTime.UtcNow,
                    UserAnalyzers = new List<UserAnalyzer>
                    {
                        new() { UserId = _testUserId, Nickname = "My Analyzer", IsOwner = true }
                    }
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Test Analyzer 2",
                    LastSeen = DateTime.UtcNow.AddHours(-1),
                    UserAnalyzers = new List<UserAnalyzer>
                    {
                        new() { UserId = _testUserId, Nickname = "Second Analyzer", IsOwner = false }
                    }
                }
            };

            _mockAnalyzerService
                .Setup(x => x.GetUserAnalyzersAsync(_testUserId))
                .ReturnsAsync(analyzers);

            // Act
            var result = await _controller.GetUserAnalyzers();

            // Assert
            result.Result.ShouldBeOfType<OkObjectResult>();
            var okResult = (OkObjectResult)result.Result;
            var response = okResult.Value as IEnumerable<AnalyzerListResponse>;
            response.ShouldNotBeNull();
            var responseList = response.ToList();

            responseList.Count.ShouldBe(2);

            // Verify first analyzer
            responseList[0].Id.ShouldBe(analyzers[0].Id);
            responseList[0].Name.ShouldBe("Test Analyzer 1");
            responseList[0].Nickname.ShouldBe("My Analyzer");
            responseList[0].LastSeen.ShouldBe(analyzers[0].LastSeen);
            responseList[0].IsOwner.ShouldBeTrue();

            // Verify second analyzer
            responseList[1].Id.ShouldBe(analyzers[1].Id);
            responseList[1].Name.ShouldBe("Test Analyzer 2");
            responseList[1].Nickname.ShouldBe("Second Analyzer");
            responseList[1].LastSeen.ShouldBe(analyzers[1].LastSeen);
            responseList[1].IsOwner.ShouldBeFalse();
        }

        [Fact]
        public async Task GetUserAnalyzers_CallsServiceWithCorrectUserId()
        {
            // Arrange
            _mockAnalyzerService
                .Setup(x => x.GetUserAnalyzersAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<SourdoughAnalyzer>());

            // Act
            await _controller.GetUserAnalyzers();

            // Assert
            _mockAnalyzerService.Verify(x => x.GetUserAnalyzersAsync(_testUserId), Times.Once);
        }
    }

    public class GetAllAnalyzers : AnalyzerControllerTests
    {
        public GetAllAnalyzers()
        {
            SetupHttpContext(true);
        }

        [Fact]
        public async Task GetAllAnalyzers_AsAdmin_ReturnsOkWithResponse()
        {
            // Arrange
            var analyzers = new List<SourdoughAnalyzer>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Test Analyzer 1",
                    MacAddress = "AA:BB:CC:DD:EE:FF",
                    FirmwareVersion = "1.0.0",
                    IsActivated = true,
                    ActivatedAt = DateTime.UtcNow.AddDays(-1),
                    LastSeen = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Test Analyzer 2",
                    MacAddress = "11:22:33:44:55:66",
                    FirmwareVersion = "1.1.0",
                    IsActivated = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                }
            };

            _mockAnalyzerService
                .Setup(x => x.GetAllAnalyzersAsync())
                .ReturnsAsync(analyzers);

            // Act
            var result = await _controller.GetAllAnalyzers();

            // Assert
            result.Result.ShouldBeOfType<OkObjectResult>();
            var okResult = (OkObjectResult)result.Result;
            var response = okResult.Value as IEnumerable<AdminAnalyzerListResponse>;
            response.ShouldNotBeNull();
            var responseList = response.ToList();

            responseList.Count.ShouldBe(2);

            // Verify first analyzer
            responseList[0].Id.ShouldBe(analyzers[0].Id);
            responseList[0].Name.ShouldBe("Test Analyzer 1");
            responseList[0].MacAddress.ShouldBe("AA:BB:CC:DD:EE:FF");
            responseList[0].FirmwareVersion.ShouldBe("1.0.0");
            responseList[0].IsActivated.ShouldBeTrue();
            responseList[0].ActivatedAt.ShouldBe(analyzers[0].ActivatedAt);
            responseList[0].LastSeen.ShouldBe(analyzers[0].LastSeen);
            responseList[0].CreatedAt.ShouldBe(analyzers[0].CreatedAt);

            // Verify second analyzer
            responseList[1].Id.ShouldBe(analyzers[1].Id);
            responseList[1].Name.ShouldBe("Test Analyzer 2");
            responseList[1].MacAddress.ShouldBe("11:22:33:44:55:66");
            responseList[1].FirmwareVersion.ShouldBe("1.1.0");
            responseList[1].IsActivated.ShouldBeFalse();
            responseList[1].ActivatedAt.ShouldBeNull();
            responseList[1].LastSeen.ShouldBeNull();
            responseList[1].CreatedAt.ShouldBe(analyzers[1].CreatedAt);
        }
    }

    public class CreateAnalyzer : AnalyzerControllerTests
    {
        public CreateAnalyzer()
        {
            SetupHttpContext(true);
        }

        [Fact]
        public async Task CreateAnalyzer_AsAdmin_ReturnsOkWithResponse()
        {
            // Arrange
            var request = new CreateAnalyzerRequest("AA:BB:CC:DD:EE:FF", "BagNU");
            var analyzer = new SourdoughAnalyzer
            {
                Id = Guid.NewGuid(),
                MacAddress = "AA:BB:CC:DD:EE:FF",
                Name = "BagNU",
                ActivationCode = "ABCD12345678"
            };

            _mockAnalyzerService
                .Setup(x => x.CreateAnalyzerAsync(request.MacAddress, request.Name))
                .ReturnsAsync(analyzer);

            // Act
            var result = await _controller.CreateAnalyzer(request);

            // Assert
            result.Result.ShouldBeOfType<OkObjectResult>();
            var okResult = (OkObjectResult)result.Result;
            var response = okResult.Value.ShouldBeOfType<CreateAnalyzerResponse>();

            response.Id.ShouldBe(analyzer.Id);
            response.MacAddress.ShouldBe(analyzer.MacAddress);
            response.Name.ShouldBe(analyzer.Name);
            response.ActivationCode.ShouldBe("ABCD-1234-5678");
        }

        [Fact]
        public async Task CreateAnalyzer_CallsServiceWithCorrectParameters()
        {
            // Arrange
            var request = new CreateAnalyzerRequest("AA:BB:CC:DD:EE:FF", "Test Analyzer");
            var analyzer = new SourdoughAnalyzer
            {
                Id = Guid.NewGuid(),
                MacAddress = "AA:BB:CC:DD:EE:FF",
                Name = "Test Analyzer",
                ActivationCode = "TEST123456789"
            };

            _mockAnalyzerService
                .Setup(x => x.CreateAnalyzerAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(analyzer);

            // Act
            await _controller.CreateAnalyzer(request);

            // Assert
            _mockAnalyzerService.Verify(x => x.CreateAnalyzerAsync(
                    "AA:BB:CC:DD:EE:FF",
                    "Test Analyzer"),
                Times.Once);
        }
    }
}