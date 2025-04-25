namespace Core.Exceptions;

/// <summary>
/// Entity cannot be found in the database.
/// 404 Not Found can be returned.
/// </summary>
public sealed class EntityNotFoundException : Exception
{
    public string EntityName { get; }
    public object? EntityId { get; }

    public EntityNotFoundException(string entityName, object? entityId = null)
        : base($"Entity {entityName}{(entityId != null ? $" with ID {entityId}" : "")} was not found.")
    {
        EntityName = entityName;
        EntityId = entityId;
    }
    
    public EntityNotFoundException(string message, string entityName, object? entityId = null, Exception? innerException = null)
        : base(message, innerException)
    {
        EntityName = entityName;
        EntityId = entityId;
    }
}