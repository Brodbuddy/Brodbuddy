using System.Diagnostics;

namespace Startup.Tests.WebApplicationFactories;

public static class ProcessWatchdog
{
    private static CancellationTokenSource? _globalWatchdogTokenSource;

    /// <summary>
    /// Starter en watchdog (vagthund) som tvinger den nuværende process til at afslutte hvis tests tager for lang tid
    /// </summary>
    /// <param name="timeoutSeconds">Maks tid i sekunder før en der afsluttes</param>
    public static void StartWatchdog(int timeoutSeconds = 30)
    {
        // Stopper eksisterende watchdogs hvis der er nogen
        StopWatchdog();

        _globalWatchdogTokenSource = new CancellationTokenSource();

        // Her starter vi en baggrunds task som dræber processen hvis den ikke bliver cancelled
        Task.Run(async () =>
        {
            try
            {
                Console.WriteLine($"[WATCHDOG] Process watchdog started - will terminate in {timeoutSeconds} seconds if not cancelled");
                
                // Wait for the timeout or cancellation
                // Venter lidt for timeout eller cancellation
                await Task.Delay(TimeSpan.FromSeconds(timeoutSeconds), _globalWatchdogTokenSource.Token);

                // Hvis vi er her, er token ikke cancelled - vi dræber nu processen
                Console.WriteLine("[WATCHDOG] ⚠️ Watchdog timeout exceeded - forcefully terminating process");

                Process currentProcess = Process.GetCurrentProcess();
                Console.WriteLine($"[WATCHDOG] Terminating process {currentProcess.Id}");

                // Vent et lille stykke tid for logs til at flush
                await Task.Delay(500);

                // Dræb processen med en anormal exit kode
                Environment.Exit(999);
            }
            catch (OperationCanceledException)
            {
                // Normal cancellation, intet at gøre
                Console.WriteLine("[WATCHDOG] Watchdog cancelled normally");
            }
            catch (Exception ex)
            {
                // Hvis noget er galt med watchdog sig selv
                Console.WriteLine($"[WATCHDOG] Error in watchdog: {ex.Message}");
            }
        });
    }

    /// <summary>
    /// Stop watchdog (kald denne når tests færdiggøres normalt)
    /// </summary>
    public static void StopWatchdog()
    {
        if (_globalWatchdogTokenSource == null) return;
        try
        {
            _globalWatchdogTokenSource.Cancel();
            _globalWatchdogTokenSource.Dispose();
            _globalWatchdogTokenSource = null;
        }
        catch
        {
            // Ignore fejl i cleanup
        }
    }
}