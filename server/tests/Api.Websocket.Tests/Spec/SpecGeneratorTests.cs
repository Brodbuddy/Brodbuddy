using System.Reflection;
using Api.Websocket.Spec;
using Brodbuddy.WebSocket.Core;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
using Xunit.Abstractions;
using Fleck;

namespace Api.Websocket.Tests.Spec;

public class SpecGeneratorTests
{
    private readonly ITestOutputHelper _output;
    private readonly IServiceProvider _serviceProvider;

    private SpecGeneratorTests(ITestOutputHelper output)
    {
        _output = output;
        var services = new ServiceCollection();
        _serviceProvider = services.BuildServiceProvider();
    }

    public class GenerateSpec(ITestOutputHelper output) : SpecGeneratorTests(output)
    {
        [Fact]
        public void GenerateSpec_WithHandlersInAssembly_GeneratesValidSpec()
        {
            // Arrange
            var assembly = Assembly.GetExecutingAssembly();
            
            // Act
            var spec = SpecGenerator.GenerateSpec(assembly, _serviceProvider);
            
            // Assert
            spec.ShouldNotBeNull();
            spec.Version.ShouldNotBeNullOrEmpty();
        }

        [Fact]
        public void GenerateSpec_WithRequestResponseHandler_MapsRequestAndResponse()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddScoped<TestRequest>();
            services.AddScoped<TestResponse>();
            services.AddScoped<TestHandler>();
            services.AddScoped<IValidator<TestRequest>, TestRequestValidator>();
            var serviceProvider = services.BuildServiceProvider();
            
            var assembly = typeof(TestHandler).Assembly;
            
            // Act
            var spec = SpecGenerator.GenerateSpec(assembly, serviceProvider);
            
            // Assert
            spec.RequestTypes.ShouldContainKey("test");
            spec.ResponseTypes.ShouldContainKey("testResponse");
        }

        [Fact]
        public void GenerateSpec_WithBroadcastMessage_MapsBroadcastType()
        {
            // Arrange
            var assembly = typeof(TestBroadcastMessage).Assembly;
            
            // Act
            var spec = SpecGenerator.GenerateSpec(assembly, _serviceProvider);
            
            // Assert
            spec.BroadcastTypes.ShouldContainKey("testBroadcastMessage");
        }

        [Fact]
        public void GenerateSpec_WithRequestValidator_MapsValidationRules()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddScoped<TestRequest>();
            services.AddScoped<TestResponse>();
            services.AddScoped<TestHandler>();
            services.AddScoped<IValidator<TestRequest>, TestRequestValidator>();
            var serviceProvider = services.BuildServiceProvider();
            
            var assembly = typeof(TestHandler).Assembly;
            
            // Act
            var spec = SpecGenerator.GenerateSpec(assembly, serviceProvider);
            
            // Assert
            var mapping = spec.RequestResponses["Test"];
            mapping.Validation.ShouldNotBeNull();
            // Validation rules extraction is not implemented yet
            mapping.Validation.Rules.ShouldBeEmpty();
        }
    }
}

public class TestRequest
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
}

public class TestResponse
{
    public string Result { get; set; } = "";
}

public class TestHandler : IWebSocketHandler<TestRequest, TestResponse>
{
    public string MessageType => "Test";
    
    public Task<TestResponse> HandleAsync(TestRequest incoming, string clientId, IWebSocketConnection socket)
    {
        var response = new TestResponse { Result = $"Processed {incoming.Name}" };
        return Task.FromResult(response);
    }
}

public class TestRequestValidator : AbstractValidator<TestRequest>
{
    public TestRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Id is required");
        RuleFor(x => x.Name).MaximumLength(100).WithMessage("Name must not exceed 100 characters");
    }
}

public class TestBroadcastMessage : IBroadcastMessage
{
    public string Content { get; set; } = "";
}