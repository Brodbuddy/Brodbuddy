using System.Diagnostics;
using Shouldly;

namespace Startup.Tests.Infrastructure.Extensions;

public static class TestExtensions
{
    public static async Task WhenReadyAsync(this object test,
                                            Action assertions,
                                            TimeSpan? timeout = null, 
                                            TimeSpan? checkEvery = null)
    {
        timeout ??= TimeSpan.FromSeconds(10);
        checkEvery ??= TimeSpan.FromMilliseconds(100);

        var stopwatch = Stopwatch.StartNew();
        var currentDelay = TimeSpan.FromMilliseconds(10);

        while (stopwatch.Elapsed < timeout)
        {
            try
            {
                assertions();
                return; // Success :)
            }
            catch (ShouldAssertException)
            {
                // Shouldly assertion failed, bliver ved med at vente...
                await Task.Delay(currentDelay);
                
                // Eksponentiel backoff up til 'checkEvery' limit
                currentDelay = TimeSpan.FromMilliseconds(Math.Min(currentDelay.TotalMilliseconds * 1.5, checkEvery.Value.TotalMilliseconds));
            }
        }

        // Prøver lige en sidste gang
        assertions();
    }
}