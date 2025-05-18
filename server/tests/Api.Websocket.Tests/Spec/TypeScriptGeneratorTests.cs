using Api.Websocket.Spec;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Api.Websocket.Tests.Spec;

public class TypeScriptGeneratorTests
{
    private readonly ITestOutputHelper _output;
    private readonly string _testTemplatesDirectory;
    private readonly string _testOutputDirectory;

    private TypeScriptGeneratorTests(ITestOutputHelper output)
    {
        _output = output;
        _testTemplatesDirectory = Path.Combine(Directory.GetCurrentDirectory(), "TestTemplates");
        _testOutputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "TestOutput");
        
        if (!Directory.Exists(_testTemplatesDirectory)) Directory.CreateDirectory(_testTemplatesDirectory);
        if (!Directory.Exists(_testOutputDirectory)) Directory.CreateDirectory(_testOutputDirectory);
    }

    public class Generate(ITestOutputHelper output) : TypeScriptGeneratorTests(output)
    {
        [Fact]
        public void Generate_WithValidSpec_CreatesOutputFile()
        {
            // Arrange
            var spec = CreateTestSpec();
            CreateTestTemplate(_testTemplatesDirectory);

            // Act
            TypeScriptGenerator.Generate(spec, _testTemplatesDirectory, _testOutputDirectory);

            // Assert
            var outputFile = Path.Combine(_testOutputDirectory, "websocket-client.ts");
            File.Exists(outputFile).ShouldBeTrue();
        }

        [Fact]
        public void Generate_WithEnums_GeneratesEnumDefinitions()
        {
            // Arrange
            var spec = CreateSpecWithEnums();
            CreateTestTemplate(_testTemplatesDirectory);

            // Act
            TypeScriptGenerator.Generate(spec, _testTemplatesDirectory, _testOutputDirectory);

            // Assert
            var outputFile = Path.Combine(_testOutputDirectory, "websocket-client.ts");
            var content = File.ReadAllText(outputFile);
            
            content.ShouldContain("export enum ErrorCode {");
            content.ShouldContain("InvalidRequest = \"1\",");
            content.ShouldContain("AuthenticationFailed = \"2\"");
        }

        [Fact]
        public void Generate_WithTypes_GeneratesTypeDefinitions()
        {
            // Arrange
            var spec = CreateSpecWithTypes();
            CreateTestTemplate(_testTemplatesDirectory);

            // Act
            TypeScriptGenerator.Generate(spec, _testTemplatesDirectory, _testOutputDirectory);

            // Assert
            var outputFile = Path.Combine(_testOutputDirectory, "websocket-client.ts");
            var content = File.ReadAllText(outputFile);
            
            content.ShouldContain("export interface UserRequest extends BaseBroadcast {");
            content.ShouldContain("id: string;");
            content.ShouldContain("name?: string;");
        }

        [Fact]
        public void Generate_WithRequestResponseMappings_GeneratesClientMethods()
        {
            // Arrange
            var spec = CreateSpecWithRequestResponseMappings();
            CreateTestTemplate(_testTemplatesDirectory);

            // Act
            TypeScriptGenerator.Generate(spec, _testTemplatesDirectory, _testOutputDirectory);

            // Assert
            var outputFile = Path.Combine(_testOutputDirectory, "websocket-client.ts");
            var content = File.ReadAllText(outputFile);
            
            content.ShouldContain("GetUser: (payload: Omit<GetUserRequest, 'requestId'>): Promise<GetUserResponse> =>");
        }

        private static WebSocketSpec CreateTestSpec()
        {
        return new WebSocketSpec(
            Version: "1.0.0",
            RequestTypes: new Dictionary<string, string>(),
            ResponseTypes: new Dictionary<string, string>(),
            BroadcastTypes: new Dictionary<string, string>(),
            ErrorCodes: new Dictionary<string, string>(),
            SubscriptionMethods: new Dictionary<string, string>(),
            UnsubscriptionMethods: new Dictionary<string, string>(),
            Types: new Dictionary<string, TypeDefinition>(),
            RequestResponses: new Dictionary<string, RequestResponseMapping>(),
            Enums: new Dictionary<string, EnumDefinition>()
        );
    }

    private static WebSocketSpec CreateSpecWithEnums()
    {
        var enums = new Dictionary<string, EnumDefinition>
        {
            ["ErrorCode"] = new(
                new Dictionary<string, object>
                {
                    ["InvalidRequest"] = 1,
                    ["AuthenticationFailed"] = 2
                }
            )
        };

        return CreateTestSpec() with { Enums = enums };
    }

    private static WebSocketSpec CreateSpecWithTypes()
    {
        var types = new Dictionary<string, TypeDefinition>
        {
            ["UserRequest"] = new(
                Kind: "Object",
                Properties: new Dictionary<string, PropertyDefinition>
                {
                    ["id"] = new(Type: "string", IsRequired: true),
                    ["name"] = new(Type: "string", IsRequired: false)
                }
            )
        };

        return CreateTestSpec() with { Types = types };
    }

    private static WebSocketSpec CreateSpecWithRequestResponseMappings()
    {
        var requestResponses = new Dictionary<string, RequestResponseMapping>
        {
            ["GetUser"] = new(
                RequestType: "GetUserRequest",
                ResponseType: "GetUserResponse",
                Validation: null
            )
        };

        var requestTypes = new Dictionary<string, string>
        {
            ["GetUser"] = "GetUser"  // Key er metode navn, value er message type 
        };

        var types = new Dictionary<string, TypeDefinition>
        {
            ["GetUserRequest"] = new(
                Kind: "Object",
                Properties: new Dictionary<string, PropertyDefinition>
                {
                    ["userId"] = new(Type: "string", IsRequired: true)
                }
            ),
            ["GetUserResponse"] = new(
                Kind: "Object",
                Properties: new Dictionary<string, PropertyDefinition>
                {
                    ["id"] = new(Type: "string", IsRequired: true),
                    ["name"] = new(Type: "string", IsRequired: true)
                }
            )
        };

        return CreateTestSpec() with 
        { 
            RequestResponses = requestResponses,
            RequestTypes = requestTypes,
            Types = types
        };
        }

        private static void CreateTestTemplate(string testTemplatesDirectory)
        {
            var templateContent = @"// WebSocket client template
/* GENERATED_IMPORTS */

export class WebSocketClient {
    /* GENERATED_SEND_METHODS */
}
";
            File.WriteAllText(Path.Combine(testTemplatesDirectory, "websocket-client-template.ts"), templateContent);
        }
    }
}