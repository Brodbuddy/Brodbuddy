namespace SharedTestDependencies;


public class FakeTimeProvider(DateTimeOffset startDateTime) : TimeProvider
{
    private DateTimeOffset _currentTime = startDateTime;

    public void Advance(TimeSpan timeSpan)
    {
        _currentTime = _currentTime.Add(timeSpan);
    }

    public override DateTimeOffset GetUtcNow() => _currentTime;
}

