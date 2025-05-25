using System.ComponentModel;
using Xunit;
using Microsoft.Playwright.Xunit;
using Shouldly;

namespace PlaywrightTests;

public class LoginTests : PageTest
{
    private static string GetBaseUrl()
    {
        return Environment.GetEnvironmentVariable("PLAYWRIGHT_TEST_BASE_URL")
               ?? throw new InvalidOperationException("PLAYWRIGHT_TEST_BASE_URL environment variable is not set");
    }

    [Fact]
    public async Task LoginPage_ShouldDisplayEmailVerificationText()
    {
        var baseUrl = GetBaseUrl();
        await Page.GotoAsync($"{baseUrl}/login");

        // En simpel test til at se om, der vises "Email Verification" på login siden
        var content = await Page.TextContentAsync("body");
        content.ShouldNotBeNull();
        content!.ShouldContain("Email Verification");
    }

    [Fact]
    public async Task SendVerificationCode_ShouldRedirectToVerify()
    {
        var baseUrl = GetBaseUrl();
        await Page.GotoAsync($"{baseUrl}/login");

        // Tjekker om knappen eksistere
        var button = await Page.QuerySelectorAsync("button[type=submit]");
        button.ShouldNotBeNull();

        // Indtaster en valid test email
        var emailInput = await Page.QuerySelectorAsync("input[type=email]");
        emailInput.ShouldNotBeNull();
        await emailInput!.FillAsync("test@example.com");

        await button!.ClickAsync();

        // Venter på URL skifter til /login/verify
        await Page.WaitForURLAsync($"{baseUrl}/login/verify");
        Page.Url.ShouldBe($"{baseUrl}/login/verify");
    }
}