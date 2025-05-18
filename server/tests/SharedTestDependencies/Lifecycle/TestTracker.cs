namespace SharedTestDependencies.Lifecycle;

public static class TestTracker
{
    private static int _activeTestCount;
    private static readonly object Lock = new();

    public static void RegisterActiveTest()
    {
        lock (Lock)
        {
            _activeTestCount++;
            Console.WriteLine($"[TestTracker] Test registered. Active tests: {_activeTestCount}");
        }
    }

    public static void UnregisterActiveTest()
    {
        lock (Lock)
        {
            _activeTestCount--;
            Console.WriteLine($"[TestTracker] Test unregistered. Active tests: {_activeTestCount}");
            
            // Hvis dette er den sidste test, så kan vi stoppe watchdog
            if (_activeTestCount > 0) return;
            Console.WriteLine("[TestTracker] All tests completed. Setting short grace period for final cleanup.");
                
            ProcessWatchdog.StopWatchdog();
                
            // Nu når alle tests er færdige, giver vi 10s til at slukke hele systemet
            // så vi undgår at det hænger
            ProcessWatchdog.StartWatchdog(10); // Ikk juster tak :) 
        }
    }
}