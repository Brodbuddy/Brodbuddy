using Microsoft.Playwright;

namespace PlaywrightTests.Helpers;

public static class PageExtensions
{
    public static async Task LoginWithEmailAsync(this IPage page, string email)
    {
        await page.FillAsync("input[type=email]", email);
        await page.ClickAsync("button[type=submit]");
    }

    public static async Task EnterVerificationCodeAsync(this IPage page, string code)
    {
        await page.FillAsync("input[inputmode='numeric']", code);
        await page.WaitForSelectorAsync("button[type='submit']:not([disabled])");
        await page.ClickAsync("button[type='submit']:has-text('Login')");
    }
    
    public static async Task<string?> GetErrorMessageAsync(this IPage page)
    {
        var errorElement = await page.QuerySelectorAsync(".bg-red-100");
        return errorElement != null ? await errorElement.TextContentAsync() : null;
    }
}