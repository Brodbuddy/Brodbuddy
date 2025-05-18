using Api.Websocket.Spec;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Api.Websocket.Tests.Spec;

public class WebSocketSpecTests
{
    private readonly ITestOutputHelper _output;

    private WebSocketSpecTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public class Constructor(ITestOutputHelper output) : WebSocketSpecTests(output)
    {
        [Fact]
        public void Constructor_WithProvidedValues_InitializesAllProperties()
        {
            // Arrange
            var requestTypes = new Dictionary<string, string> { ["GetUser"] = "Gets user information" };
            var responseTypes = new Dictionary<string, string> { ["UserResponse"] = "User information response" };
            var broadcastTypes = new Dictionary<string, string> { ["UserUpdated"] = "User update notification" };
            var errorCodes = new Dictionary<string, string> { ["InvalidRequest"] = "The request is invalid" };
            var subscriptionMethods = new Dictionary<string, string> { ["SubscribeToUser"] = "Subscribe to user updates" };
            var unsubscriptionMethods = new Dictionary<string, string> { ["UnsubscribeFromUser"] = "Unsubscribe from user updates" };
            var types = new Dictionary<string, TypeDefinition>();
            var requestResponses = new Dictionary<string, RequestResponseMapping>();
            var enums = new Dictionary<string, EnumDefinition>();

            // Act
            var spec = new WebSocketSpec(
                Version: "1.0.0",
                RequestTypes: requestTypes,
                ResponseTypes: responseTypes,
                BroadcastTypes: broadcastTypes,
                ErrorCodes: errorCodes,
                SubscriptionMethods: subscriptionMethods,
                UnsubscriptionMethods: unsubscriptionMethods,
                Types: types,
                RequestResponses: requestResponses,
                Enums: enums
            );

            // Assert
            spec.Version.ShouldBe("1.0.0");
            spec.RequestTypes.ShouldBe(requestTypes);
            spec.ResponseTypes.ShouldBe(responseTypes);
            spec.BroadcastTypes.ShouldBe(broadcastTypes);
            spec.ErrorCodes.ShouldBe(errorCodes);
            spec.SubscriptionMethods.ShouldBe(subscriptionMethods);
            spec.UnsubscriptionMethods.ShouldBe(unsubscriptionMethods);
            spec.Types.ShouldBe(types);
            spec.RequestResponses.ShouldBe(requestResponses);
            spec.Enums.ShouldBe(enums);
        }
    }

    public class RequestResponseMappingTests(ITestOutputHelper output) : WebSocketSpecTests(output)
    {
        [Fact]
        public void Constructor_WithProvidedValues_InitializesAllProperties()
        {
            // Arrange
            var validation = new ValidationDefinition(
                Rules: new Dictionary<string, List<ValidationRule>>
                {
                    ["Id"] = [new ValidationRule(Type: "Required", Value: null, Message: "Id is required")]
                }
            );

            // Act
            var mapping = new RequestResponseMapping(
                RequestType: "GetUserRequest",
                ResponseType: "GetUserResponse",
                Validation: validation
            );

            // Assert
            mapping.RequestType.ShouldBe("GetUserRequest");
            mapping.ResponseType.ShouldBe("GetUserResponse");
            mapping.Validation.ShouldBe(validation);
        }
    }

    public class TypeDefinitionTests(ITestOutputHelper output) : WebSocketSpecTests(output)
    {
        [Fact]
        public void Constructor_WithProvidedValues_InitializesAllProperties()
        {
            // Arrange
            var properties = new Dictionary<string, PropertyDefinition>
            {
                ["id"] = new(Type: "string", IsRequired: true),
                ["name"] = new(Type: "string", IsRequired: false)
            };

            // Act
            var typeDef = new TypeDefinition(
                Kind: "Object",
                Properties: properties
            );

            // Assert
            typeDef.Kind.ShouldBe("Object");
            typeDef.Properties.ShouldBe(properties);
        }
    }

    public class PropertyDefinitionTests(ITestOutputHelper output) : WebSocketSpecTests(output)
    {
        [Fact]
        public void Constructor_WithProvidedValues_InitializesAllProperties()
        {
            // Arrange & Act
            var propDef = new PropertyDefinition(
                Type: "string",
                IsRequired: true
            );

            // Assert
            propDef.Type.ShouldBe("string");
            propDef.IsRequired.ShouldBeTrue();
        }
    }

    public class ValidationDefinitionTests(ITestOutputHelper output) : WebSocketSpecTests(output)
    {
        [Fact]
        public void Constructor_WithProvidedValues_InitializesAllProperties()
        {
            // Arrange
            var rules = new Dictionary<string, List<ValidationRule>>
            {
                ["Email"] =
                [
                    new ValidationRule(Type: "Email", Value: null, Message: "Invalid email format"),
                    new ValidationRule(Type: "Required", Value: null, Message: "Email is required")
                ]
            };

            // Act
            var validationDef = new ValidationDefinition(Rules: rules);

            // Assert
            validationDef.Rules.ShouldBe(rules);
        }
    }

    public class ValidationRuleTests(ITestOutputHelper output) : WebSocketSpecTests(output)
    {
        [Fact]
        public void Constructor_WithProvidedValues_InitializesAllProperties()
        {
            // Arrange & Act
            var rule = new ValidationRule(
                Type: "MaxLength",
                Value: 100,
                Message: "Maximum length is 100 characters"
            );

            // Assert
            rule.Type.ShouldBe("MaxLength");
            rule.Value.ShouldBe(100);
            rule.Message.ShouldBe("Maximum length is 100 characters");
        }
    }

    public class EnumDefinitionTests(ITestOutputHelper output) : WebSocketSpecTests(output)
    {
        [Fact]
        public void Constructor_WithProvidedValues_InitializesAllProperties()
        {
            // Arrange
            var values = new Dictionary<string, object>
            {
                ["Success"] = 0,
                ["Error"] = 1,
                ["Warning"] = 2
            };

            // Act
            var enumDef = new EnumDefinition(Values: values);

            // Assert
            enumDef.Values.ShouldBe(values);
        }
    }
}