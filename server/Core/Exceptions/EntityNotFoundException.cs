namespace Core.Exceptions;

/// <summary>
/// Thrown when a requested entity cannot be found in the database.
/// More specific than the generic ObjectNotFoundException.
/// Useful for REST APIs where 404 Not Found can be returned.
/// </summary>
public sealed class EntityNotFoundException : ApplicationException
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