using System;

namespace SharedTestDependencies;

public class TimeProviderAdapter(TimeProvider timeProvider)
{
    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    public DateTimeOffset GetNow()
    {
        return _timeProvider.GetUtcNow();
    }
}