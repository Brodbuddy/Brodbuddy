namespace PlaywrightTests.Helpers;

public static class TestConfiguration
{
    public static string BaseUrl => 
        Environment.GetEnvironmentVariable("PLAYWRIGHT_TEST_BASE_URL")
        ?? "https://staging.brodbuddy.com";
    
    public static string GenerateTestEmail() => $"playwright-test-{Guid.NewGuid():N}@example.com";
}