namespace Api.Websocket.Spec;


public sealed record WebSocketSpec(
    string Version,
    Dictionary<string, string> MessageTypes,
    Dictionary<string, TypeDefinition> Types,
    Dictionary<string, RequestResponseMapping> RequestResponses
);

public sealed record RequestResponseMapping(
    string RequestType,
    string ResponseType,
    ValidationDefinition? Validation
);

public sealed record TypeDefinition(
    string Kind,
    Dictionary<string, PropertyDefinition> Properties
);

public sealed record PropertyDefinition(
    string Type,
    bool IsRequired
);

public sealed record ValidationDefinition(
    Dictionary<string, List<ValidationRule>> Rules
);

public sealed record ValidationRule(
    string Type,
    object? Value,
    string? Message
); 