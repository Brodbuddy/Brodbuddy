using Microsoft.Playwright;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace PlaywrightTests
{
    public class TestBase
    {
        protected IPlaywright Playwright { get; private set; }
        protected IBrowser Browser { get; private set; }
        protected IPage Page { get; private set; }
        protected string BaseUrl { get; private set; } = "http://localhost:5173"; 

		// Vi benytter SetUp for at sikre os, at metoden bliver kørt inden hver test
        [SetUp]
        public async Task SetUp()
        {
            try
            {
                Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
                Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Headless = false,
                    SlowMo = 50,
                });
                
                var context = await Browser.NewContextAsync();
                Page = await context.NewPageAsync();
                
                await Page.GotoAsync(BaseUrl);
            }
            catch (Exception)
            {
                if (Browser != null) await Browser.DisposeAsync();
                if (Playwright != null) Playwright.Dispose();
                throw;
            }
        }

		// Vi benytter TearDown for at sikre os, at metoden bliver kørt efter hver test
        [TearDown]
        public async Task TearDown()
        {
            if (Page != null) await Page.CloseAsync();
            if (Browser != null) await Browser.DisposeAsync();
            if (Playwright != null) Playwright.Dispose();
        }
    }
}