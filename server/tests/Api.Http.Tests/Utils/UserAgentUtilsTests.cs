using Api.Http.Utils;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Api.Http.Tests.Utils;


public class UserAgentUtilsTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public UserAgentUtilsTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    public class GetBrowser(ITestOutputHelper testOutputHelper) : UserAgentUtilsTests(testOutputHelper)
    {
        [Fact]
        public void GetBrowser_WithNullUserAgent_ReturnsUnknown()
        {
            // Act
            string? nullString = null;
            var result = UserAgentUtils.GetBrowser(nullString!);

            // Assert
            result.ShouldBe("Unknown");
        }

        [Fact]
        public void GetBrowser_WithEmptyUserAgent_ReturnsUnknown()
        {
            // Act
            var result = UserAgentUtils.GetBrowser(string.Empty);

            // Assert
            result.ShouldBe("Unknown");
        }

        [Theory]
        [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/134.0.0.0 Safari/537.36", "Chrome")]
        [InlineData("Mozilla/5.0 (iPhone; CPU iPhone OS 16_5 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/16.5 Mobile/15E148 Safari/604.1", "Mobile Safari")]
        [InlineData("Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)", "Googlebot")]
        public void GetBrowser_WithUserAgent_ReturnsExpectedBrowser(string userAgent, string expectedBrowser)
        {
            // Act
            var result = UserAgentUtils.GetBrowser(userAgent);

            // Assert
            result.ShouldBe(expectedBrowser);
        }
    }

    public class GetOperatingSystem(ITestOutputHelper testOutputHelper) : UserAgentUtilsTests(testOutputHelper)
    {
        [Fact]
        public void GetOperatingSystem_WithNullUserAgent_ReturnsUnknown()
        {
            // Act
            string? nullString = null;
            var result = UserAgentUtils.GetOperatingSystem(nullString!);

            // Assert
            result.ShouldBe("Unknown");
        }

        [Fact]
        public void GetOperatingSystem_WithEmptyUserAgent_ReturnsUnknown()
        {
            // Act
            var result = UserAgentUtils.GetOperatingSystem(string.Empty);

            // Assert
            result.ShouldBe("Unknown");
        }

        [Theory]
        [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/134.0.0.0 Safari/537.36", "Windows")]
        [InlineData("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.3.1 Safari/605.1.15", "Mac OS X")]
        [InlineData("Mozilla/5.0 (Linux; Android 14; SM-S908B) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/112.0.0.0 Mobile Safari/537.36", "Android")]
        public void GetOperatingSystem_WithUserAgent_ReturnsExpectedOS(string userAgent, string expectedOs)
        {
            // Act
            var result = UserAgentUtils.GetOperatingSystem(userAgent);

            // Assert
            result.ShouldBe(expectedOs);
        }
    }
}