using Api.Websocket.Auth;
using Api.Websocket.Spec;
using Brodbuddy.WebSocket.Auth;
using Brodbuddy.WebSocket.Core;
using Brodbuddy.WebSocket.State;
using Fleck;
using FluentValidation;

namespace Api.Websocket.EventHandlers;

public enum UserRole
{
    Guest = 0,
    Member = 1,
    Moderator = 2,
    Admin = 3
}

public enum RoomStatus
{
    Active,
    Inactive,
    Maintenance
}

// Broadcast messages with various types
public record BroadcastTest(string RoomId, string Status) : IBroadcastMessage;

public record UserStatusBroadcast(
    string RoomId,
    Guid UserId,
    bool IsOnline,
    DateTime LastSeen,
    UserRole Role,
    double Score,
    List<string> Achievements,
    Dictionary<string, int> Statistics
) : IBroadcastMessage;

public record RoomStatsUpdate(
    string RoomId,
    int ActiveUsers,
    int? MaxUsers,
    decimal AverageScore,
    RoomStatus Status,
    TimeSpan Uptime,
    DateTime CreatedAt,
    byte[] MetadataHash,
    List<Dictionary<string, object>> RecentActivity
) : IBroadcastMessage;

// Request/Response pairs
public record JoinRoom(string RoomId, string Username);

public record UserJoined(string RoomId, string Username, Guid ConnectionId);

public record CreateRoom(
    string Name,
    string? Description,
    int MaxUsers,
    bool IsPrivate,
    List<string> Tags,
    Dictionary<string, string> Settings,
    UserRole RequiredRole,
    DateTime? ExpiresAt
);

public record RoomCreated(
    Guid RoomId,
    string Name,
    DateTime CreatedAt,
    Guid CreatedBy,
    bool Success,
    string? ErrorMessage,
    List<UserRole> AllowedRoles,
    Dictionary<string, object> Configuration
);

public record UpdateUserProfile(
    string DisplayName,
    char? AvatarLetter,
    UserRole Role,
    List<int> PreferredRooms,
    Dictionary<string, bool> Preferences,
    byte[]? Avatar,
    float Score,
    long ExperiencePoints,
    DateTime? LastLoginAt
);

public record UserProfileUpdated(
    Guid UserId,
    bool Success,
    List<string> ChangedFields,
    decimal NewScore,
    sbyte Level,
    Dictionary<string, object> UpdatedData,
    DateTimeOffset Timestamp
);

public record GetRoomHistory(
    string RoomId,
    int Limit,
    DateTime? FromDate,
    DateTime? ToDate,
    List<string>? MessageTypes
);

public record RoomHistoryResponse(
    string RoomId,
    List<Dictionary<string, object>> Messages,
    bool HasMore,
    int TotalCount,
    TimeSpan QueryDuration,
    DateTime GeneratedAt,
    Dictionary<string, int> MessageTypeCounts,
    List<Guid> ParticipantIds
);

// Validators
public class JoinRoomValidator : AbstractValidator<JoinRoom> 
{
    public JoinRoomValidator()
    {
        RuleFor(x => x.RoomId).NotEmpty();
        RuleFor(x => x.Username).NotEmpty()
            .MinimumLength(2)
            .MaximumLength(50)
            .Must(username => !string.IsNullOrWhiteSpace(username))
            .WithMessage("Username cannot be whitespace only");
    }
}

public class CreateRoomValidator : AbstractValidator<CreateRoom>
{
    public CreateRoomValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.MaxUsers).GreaterThan(0).LessThanOrEqualTo(500);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Tags).Must(tags => tags.Count <= 10);
    }
}

public class UpdateUserProfileValidator : AbstractValidator<UpdateUserProfile>
{
    public UpdateUserProfileValidator()
    {
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Score).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ExperiencePoints).GreaterThanOrEqualTo(0);
    }
}

// Handlers
[AllowAnonymous]
public class JoinRoomHandler(ISocketManager manager) : ISubscriptionHandler<JoinRoom, UserJoined>
{
    public string GetTopicKey(JoinRoom request, string clientId) => $"room:{request.RoomId}";
    
    public async Task<UserJoined> HandleAsync(JoinRoom incoming, string clientId, IWebSocketConnection socket)
    {
        var topic = GetTopicKey(incoming, clientId); 
        
        var existingTopics = await manager.GetTopicsAsync(clientId);
        var alreadySubscribed = existingTopics.Contains(topic);
        
        await manager.SubscribeAsync(clientId, topic);
        
        if (!alreadySubscribed) 
            await manager.BroadcastAsync(topic, new BroadcastTest(incoming.RoomId, "User joined"));

        return new UserJoined(incoming.RoomId, incoming.Username, socket.ConnectionInfo.Id);
    }
}

[Authorize(Roles = "user")]
public class CreateRoomHandler : IWebSocketHandler<CreateRoom, RoomCreated>
{
    public async Task<RoomCreated> HandleAsync(CreateRoom incoming, string clientId, IWebSocketConnection socket)
    {
        await Task.Delay(1); // Placeholder
        
        return new RoomCreated(
            RoomId: Guid.NewGuid(),
            Name: incoming.Name,
            CreatedAt: DateTime.UtcNow,
            CreatedBy: socket.ConnectionInfo.Id,
            Success: true,
            ErrorMessage: null,
            AllowedRoles: [UserRole.Member, UserRole.Moderator, UserRole.Admin],
            Configuration: new Dictionary<string, object> { { "maxUsers", incoming.MaxUsers } }
        );
    }
}

[Authorize(Roles = "user")]
public class UpdateUserProfileHandler : IWebSocketHandler<UpdateUserProfile, UserProfileUpdated>
{
    public async Task<UserProfileUpdated> HandleAsync(UpdateUserProfile incoming, string clientId, IWebSocketConnection socket)
    {
        await Task.Delay(1); // Placeholder
        
        return new UserProfileUpdated(
            UserId: socket.ConnectionInfo.Id,
            Success: true,
            ChangedFields: ["DisplayName", "Role"],
            NewScore: (decimal)incoming.Score,
            Level: 5,
            UpdatedData: new Dictionary<string, object> 
            { 
                { "role", incoming.Role },
                { "lastUpdate", DateTimeOffset.UtcNow }
            },
            Timestamp: DateTimeOffset.UtcNow
        );
    }
}

[AllowAnonymous]
public class GetRoomHistoryHandler : IWebSocketHandler<GetRoomHistory, RoomHistoryResponse>
{
    public async Task<RoomHistoryResponse> HandleAsync(GetRoomHistory incoming, string clientId, IWebSocketConnection socket)
    {
        await Task.Delay(1); // Placeholder
        
        return new RoomHistoryResponse(
            RoomId: incoming.RoomId,
            Messages: new List<Dictionary<string, object>>(),
            HasMore: false,
            TotalCount: 0,
            QueryDuration: TimeSpan.FromMilliseconds(15),
            GeneratedAt: DateTime.UtcNow,
            MessageTypeCounts: new Dictionary<string, int> { { "chat", 150 }, { "system", 23 } },
            ParticipantIds: new List<Guid>()
        );
    }
}