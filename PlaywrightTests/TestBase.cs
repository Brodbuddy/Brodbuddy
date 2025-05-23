using System;
using Microsoft.Playwright.Xunit;

namespace PlaywrightTests
{
    public class TestBase : PageTest
    {
        protected static string GetBaseUrl()
        {
            return Environment.GetEnvironmentVariable("PLAYWRIGHT_TEST_BASE_URL") 
                   ?? throw new InvalidOperationException("PLAYWRIGHT_TEST_BASE_URL environment variable is not set");
        }
    }
}