namespace SharedTestDependencies;


public class FakeTimeProvider(DateTimeOffset startDateTime) : TimeProvider
{
    private DateTimeOffset _currentTime = startDateTime;

    public void Advance(TimeSpan timeSpan)
    {
        _currentTime = _currentTime.Add(timeSpan);
    }
    
    public void SetUtcNow(DateTime utcNow)
    {
        _currentTime = new DateTimeOffset(utcNow);
    }

    public override DateTimeOffset GetUtcNow() => _currentTime;
}

