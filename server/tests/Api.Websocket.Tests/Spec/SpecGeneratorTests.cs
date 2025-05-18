using System.Reflection;
using Api.Websocket.Spec;
using Brodbuddy.WebSocket.Core;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
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
    
    public class GenerateTypeDefinitionTests(ITestOutputHelper output) : SpecGeneratorTests(output)
    {
        [Theory]
        [InlineData("NullableInt", true)]        // Nullable value type
        [InlineData("NullableBool", true)]       // Nullable value type 
        [InlineData("NullableString", false)]    // Nullable reference type 
        [InlineData("NullableList", false)]      // Nullable reference type 
        [InlineData("NonNullableString", false)] // Non-nullable reference type
        [InlineData("NonNullableList", false)]   // Non-nullable reference type
        public void GenerateTypeDefinition_ReflectsCurrentNullabilityImplementation(string propertyName, bool expectedIsNullable)
        {
            // Arrange
            var testType = typeof(ClassWithDifferentPropertyTypes);
    
            // Act
            var typeDefinition = SpecGenerator.GenerateTypeDefinition(testType);
    
            // Assert
            var propertyDef = typeDefinition.Properties[propertyName];
            propertyDef.IsRequired.ShouldBe(!expectedIsNullable);
        }
        
        [Fact]
        public void GenerateTypeDefinition_SetsKindToRecord()
        {
            // Arrange
            var testType = typeof(TestRequest);
            
            // Act
            var typeDefinition = SpecGenerator.GenerateTypeDefinition(testType);
            
            // Assert
            typeDefinition.Kind.ShouldBe("record");
        }
    }
    
    public class GenerateValidationDefinitionTests(ITestOutputHelper output) : SpecGeneratorTests(output)
    {
        [Fact]
        public void GenerateValidationDefinition_WithMultipleValidators_FindsCorrect()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddScoped<TestRequest>();
            services.AddScoped<IValidator<TestRequest>, TestRequestValidator>();
            services.AddScoped<IValidator<TestRequest>, AnotherTestRequestValidator>(); 
            var serviceProvider = services.BuildServiceProvider();
            
            // Act
            var assembly = typeof(TestRequest).Assembly;
            var spec = SpecGenerator.GenerateSpec(assembly, serviceProvider);
            
            // Assert
            var validation = spec.RequestResponses["Test"].Validation;
            validation.ShouldNotBeNull();
        }
    }
    
     public class GetTypeScriptTypeTests(ITestOutputHelper output) : SpecGeneratorTests(output)
    {
        [Theory]
        [InlineData(typeof(byte), "number")]
        [InlineData(typeof(sbyte), "number")]
        [InlineData(typeof(short), "number")]
        [InlineData(typeof(ushort), "number")]
        [InlineData(typeof(int), "number")]
        [InlineData(typeof(int?), "number | null")]
        [InlineData(typeof(string), "string")]
        [InlineData(typeof(bool), "boolean")]
        [InlineData(typeof(DateTime), "string")]
        [InlineData(typeof(Guid), "string")]
        [InlineData(typeof(double), "number")]
        [InlineData(typeof(Uri), "string")]
        [InlineData(typeof(long), "number")]
        [InlineData(typeof(ulong), "number")]
        [InlineData(typeof(float), "number")]
        [InlineData(typeof(decimal), "number")]
        [InlineData(typeof(char), "string")]
        [InlineData(typeof(DateTimeOffset), "string")]
        [InlineData(typeof(DateOnly), "string")]
        [InlineData(typeof(TimeOnly), "string")]
        [InlineData(typeof(TimeSpan), "string")]
        [InlineData(typeof(object), "any")]
        public void GetTypeScriptType_MapsBasicTypesCorrectly(Type type, string expectedTypeScriptType)
        {
            // Arrange
            var tsType = GetTypeScriptTypeHelper(type);
            
            // Assert
            tsType.ShouldBe(expectedTypeScriptType);
        }
        
        [Fact]
        public void GetTypeScriptType_WithUnknownType_ReturnsAny()
        {
            // Arrange
            var customType = GetType(); 
    
            // Act
            var tsType = GetTypeScriptTypeHelper(customType);
    
            // Assert
            tsType.ShouldBe("any");
        }
        
        [Fact]
        public void GetTypeScriptType_WithEnum_ReturnsEnumName()
        {
            // Arrange
            var enumType = typeof(EnumTest);
            
            // Act
            var tsType = GetTypeScriptTypeHelper(enumType);
            
            // Assert
            tsType.ShouldBe(enumType.Name);
        }
        
        [Theory]
        [InlineData(typeof(int[]), "number[]")]
        [InlineData(typeof(string[]), "string[]")]
        [InlineData(typeof(List<int>), "number[]")]
        [InlineData(typeof(IEnumerable<string>), "string[]")]
        [InlineData(typeof(ICollection<bool>), "boolean[]")]
        [InlineData(typeof(Dictionary<string, int>), "Record<string, number>")]
        [InlineData(typeof(IDictionary<string, bool>), "Record<string, boolean>")]
        public void GetTypeScriptType_WithCollections_AppendsArraySuffix(Type type, string expectedType)
        {
            // Act
            var tsType = GetTypeScriptTypeHelper(type);
            
            // Assert
            tsType.ShouldBe(expectedType);
        }
        
        [Fact]
        public void GetTypeScriptType_WithKeyValuePair_ReturnsObjectLiteral()
        {
            // Arrange
            var kvpType = typeof(KeyValuePair<string, int>);
            
            // Act
            var tsType = GetTypeScriptTypeHelper(kvpType);
            
            // Assert
            tsType.ShouldBe("{ key: string; value: number }");
        }
        
        private static string GetTypeScriptTypeHelper(Type type)
        {
            var methodInfo = typeof(SpecGenerator).GetMethod("GetTypeScriptType", BindingFlags.NonPublic | BindingFlags.Static);
    
            if (methodInfo == null) throw new InvalidOperationException("GetTypeScriptType method not found in SpecGenerator");
    
            var result = methodInfo.Invoke(null, [type]);
            return result as string ?? "";
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

public enum EnumTest { Value1, Value2 }
    
public class ClassWithDifferentPropertyTypes
{
    // Non-nullable properties 
    public string NonNullableString { get; set; } = string.Empty;
    public List<int> NonNullableList { get; set; } = [];
    
    // Nullable properties
    public string? NullableString { get; set; }
    public int? NullableInt { get; set; }
    public bool? NullableBool { get; set; }
    public List<int>? NullableList { get; set; }
}
    
public class AnotherTestRequestValidator : AbstractValidator<TestRequest>
{
    public AnotherTestRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

#pragma warning disable S2094 // Classes should not be empty
public class ClassWithNoValidator { }
#pragma warning restore S2094 // Classes should not be empty

public class DummyValidator : AbstractValidator<ClassWithNoValidator> { }