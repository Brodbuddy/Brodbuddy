namespace Core.Extensions;

public static class TimeProviderExtensions
{
    public static DateTime Now(this TimeProvider timeProvider)
    {
        return timeProvider.GetUtcNow().UtcDateTime;
    }
}