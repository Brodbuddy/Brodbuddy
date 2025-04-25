namespace Infrastructure.Communication.Websocket;

public static class RedisSocketKeys
{
    // --- Key præfikser ---
    private const string SocketHashPrefix = "socket:";
    private const string SocketToClientPrefix = "socket_to_client:"; 
    private const string ClientPrefix = "client:";
    private const string ClientSocketsSetSuffix = ":sockets";
    private const string ClientTopicsSetSuffix = ":topics"; 
    private const string TopicPrefix = "topic:";
    private const string TopicSubscribersSetSuffix = ":subscribers";
    private const string PubSubChannelPrefix = "pubsub:";

    // --- Specifikke keys ---
    public const string ActiveSocketsSetKey = "active_sockets";
    public const string AllTopicsSetKey = "topics";

    // --- Hash felt navne ---
    public const string ClientIdField = "clientId";
    public const string ConnectedAtField = "connectedAt";

    // --- Metoder for at generere Redis-nøgler (keys) ---
    
    /// <summary>
    /// Returnerer Redis-nøglen for hashen, der gemmer socket-metadata.
    /// Format: socket:[socketId]
    /// </summary>
    public static string SocketHash(Guid socketId) => $"{SocketHashPrefix}{socketId}";

    /// <summary>
    ///Returnerer Redis-nøglen for strengen, der mapper et socket-ID til et klient-ID.
    /// Format: socket_to_client:[socketId]
    /// </summary>
    public static string SocketToClientMap(Guid socketId) => $"{SocketToClientPrefix}{socketId}";

    /// <summary>
    /// Returnerer Redis-nøglen for sættet, der indeholder socket-ID'er tilknyttet en klient
    /// Format: client:[clientid]:sockets
    /// </summary>
    public static string ClientSocketsSet(string clientId) => $"{ClientPrefix}{clientId}{ClientSocketsSetSuffix}";
    
    /// <summary>
    /// Returnerer Redis-nøglen for sættet, der indeholder navnene på de topics, en klient er abonneret på.
    /// Format: client:[clientId]:topics
    /// </summary>
    public static string ClientTopicsSet(string clientId) => $"{ClientPrefix}{clientId}{ClientTopicsSetSuffix}";

    /// <summary>
    /// Returnerer Redis-nøglen for sættet, der indeholder klient-ID'er for abonnenter på et topic.
    /// Format: topic:[topic]:subscribers
    /// </summary>
    public static string TopicSubscribersSet(string topic) => $"{TopicPrefix}{topic}{TopicSubscribersSetSuffix}";
    
    /// <summary>
    /// Returnerer Redis Pub/Sub-kanalnavnet for et specifikt topic.
    /// Format: pubsub:topic:[topic]
    /// </summary>
    public static string TopicChannel(string topic) => $"{PubSubChannelPrefix}{TopicPrefix}{topic}";

    /// <summary>
    /// Returnerer Redis Pub/Sub-kanalmønsteret til abonnement på alle topic-udsendelser.
    /// Format: pubsub:topic:*
    /// </summary>
    public static string AllTopicChannelsPattern() => $"{PubSubChannelPrefix}{TopicPrefix}*";
}