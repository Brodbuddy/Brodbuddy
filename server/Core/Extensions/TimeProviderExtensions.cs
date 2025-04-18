namespace Core.Extensions;

public static class TimeProviderExtensions
{
    public static DateTime Now(this TimeProvider timeProvider)
    {
        return timeProvider.GetUtcNow().UtcDateTime;
    }

    public static DateTime Tomorrow(this TimeProvider timeProvider)
    {
        return timeProvider.Now().AddDays(1);
    }

    public static DateTime Yesterday(this TimeProvider timeProvider)
    {
        return timeProvider.Now().AddDays(-1);
    }
}