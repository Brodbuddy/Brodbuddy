using Xunit;
using Microsoft.Playwright.Xunit;
using Shouldly;
using PlaywrightTests.Helpers;

namespace PlaywrightTests;

public class LoginTests : PageTest
{
    [Fact]
    public async Task LoginPage_ShouldDisplayEmailVerificationText()
    {
        await Page.GotoAsync($"{TestConfiguration.BaseUrl}/login");

        var content = await Page.TextContentAsync("body");
        content.ShouldNotBeNull();
        content!.ShouldContain("Email Verification");
    }

    [Fact]
    public async Task SendVerificationCode_ShouldRedirectToVerifyPage()
    {
        await Page.GotoAsync($"{TestConfiguration.BaseUrl}/login");
        await Page.LoginWithEmailAsync(TestConfiguration.GenerateTestEmail());
        
        await Page.WaitForURLAsync($"{TestConfiguration.BaseUrl}/login/verify");
        Page.Url.ShouldBe($"{TestConfiguration.BaseUrl}/login/verify");
    }

    [Fact]
    public async Task FullLoginFlow_ShouldAuthenticateUserSuccessfully()
    {
        var testEmail = TestConfiguration.GenerateTestEmail();
        using var mailhogClient = new MailHogClient(TestConfiguration.BaseUrl);
        
        await Page.GotoAsync($"{TestConfiguration.BaseUrl}/login");
        await Page.LoginWithEmailAsync(testEmail);
        await Page.WaitForURLAsync($"{TestConfiguration.BaseUrl}/login/verify");
        
        var verificationCode = await mailhogClient.GetLatestVerificationCodeAsync(testEmail);
        verificationCode.ShouldNotBeNull();
        
        await Page.EnterVerificationCodeAsync(verificationCode!);
        await Page.WaitForLoadStateAsync();
        await Task.Delay(2000);
        
        var errorMessage = await Page.GetErrorMessageAsync();
        if (errorMessage != null)
            throw new Exception($"Login failed with error: {errorMessage}");
        
        Page.Url.ShouldBe($"{TestConfiguration.BaseUrl}/");
        var dashboardTitle = await Page.TextContentAsync("h1");
        dashboardTitle.ShouldBe("My Sourdough");
    }
}