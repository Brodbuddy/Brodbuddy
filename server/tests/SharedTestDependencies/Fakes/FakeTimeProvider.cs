namespace SharedTestDependencies.Fakes;


public class FakeTimeProvider(DateTimeOffset startDateTime) : TimeProvider
{
    private DateTimeOffset _currentTime = startDateTime;

    public void SetUtcNow(DateTimeOffset newTime)
    {
        _currentTime = newTime;
    }

    public void Advance(TimeSpan timeSpan)
    {
        _currentTime = _currentTime.Add(timeSpan);
    }

    public override DateTimeOffset GetUtcNow() => _currentTime;
}

