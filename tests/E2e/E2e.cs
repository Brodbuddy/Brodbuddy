using System;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;
using System.Text.RegularExpressions;
using Xunit;

namespace PlaywrightTests;

public class E2e : PageTest
{
    [Fact]
    public async Task WebSocketChatRoomShouldLoad()
    {
        string baseUrl = System.Environment.GetEnvironmentVariable("PLAYWRIGHT_TEST_BASE_URL") 
                         ?? throw new InvalidOperationException("PLAYWRIGHT_TEST_BASE_URL environment variable is not set");
        
        if (!baseUrl.Contains("api."))
        {
            baseUrl = baseUrl.Replace("http://", "http://api.");
        }
        
        Console.WriteLine($"Navigating to: {baseUrl}");
        await Page.GotoAsync(baseUrl);
        
        await Expect(Page.GetByText("Hej, nu med multi API :)")).ToBeVisibleAsync(
            new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
    }
}