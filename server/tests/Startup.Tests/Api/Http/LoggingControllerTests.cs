using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Api.Http.Models;
using Application.Models;
using SharedTestDependencies.Constants;
using Shouldly;
using Startup.Tests.Infrastructure.Bases;
using Startup.Tests.Infrastructure.Extensions;
using Startup.Tests.Infrastructure.Fixtures;
using Xunit.Abstractions;

namespace Startup.Tests.Api.Http;

[Collection(TestCollections.Startup)]
public class LoggingControllerTests(StartupTestFixture fixture, ITestOutputHelper output) : ApiTestBase(fixture, output)
{
    private const string BaseUrl = "/api/logging";
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public class GetCurrentLogLevel(StartupTestFixture fixture, ITestOutputHelper output) : LoggingControllerTests(fixture, output)
    {
        [Fact]
        public async Task GetCurrentLogLevel_WithoutAuth_Returns401()
        {
            // Arrange
            var client = Factory.CreateClient();

            // Act
            var response = await client.GetAsync($"{BaseUrl}/level");

            // Assert
            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetCurrentLogLevel_WithAuth_Returns200()
        {
            // Arrange
            var client = Factory.CreateAdminHttpClient();

            // Act
            var response = await client.GetAsync($"{BaseUrl}/level");

            // Assert
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<LogLevelResponse>(JsonOptions);
            content.ShouldNotBeNull();
            content.CurrentLevel.ShouldBeOneOf(
                LoggingLevel.Verbose,
                LoggingLevel.Debug,
                LoggingLevel.Information,
                LoggingLevel.Warning,
                LoggingLevel.Error,
                LoggingLevel.Fatal);
        }
    }

    public class SetLogLevel(StartupTestFixture fixture, ITestOutputHelper output) : LoggingControllerTests(fixture, output)
    {
        [Fact]
        public async Task SetLogLevel_WithoutAuth_Returns401()
        {
            // Arrange
            var client = Factory.CreateClient();
            var request = new LogLevelUpdateRequest(LoggingLevel.Debug);

            // Act
            var response = await client.PutAsJsonAsync($"{BaseUrl}/level", request);

            // Assert
            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task SetLogLevel_WithValidRequest_Returns200()
        {
            // Arrange
            var client = Factory.CreateAdminHttpClient();
            var request = new LogLevelUpdateRequest(LoggingLevel.Debug);

            // Act
            var response = await client.PutAsJsonAsync($"{BaseUrl}/level", request);

            // Assert
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<LogLevelUpdateResponse>(JsonOptions);
            content.ShouldNotBeNull();
            content.Message.ShouldBe("Log level updated");
            content.CurrentLevel.ShouldBe(LoggingLevel.Debug);
        }

        [Fact]
        public async Task SetLogLevel_PersistsAcrossRequests()
        {
            // Arrange
            var client = Factory.CreateAdminHttpClient();

            var getResponse = await client.GetAsync($"{BaseUrl}/level");
            var originalLevel = await getResponse.Content.ReadFromJsonAsync<LogLevelResponse>(JsonOptions);

            // Act  
            var newLevel = originalLevel!.CurrentLevel == LoggingLevel.Debug
                ? LoggingLevel.Warning
                : LoggingLevel.Debug;
            var updateRequest = new LogLevelUpdateRequest(newLevel);
            await client.PutAsJsonAsync($"{BaseUrl}/level", updateRequest);

            // Assert
            getResponse = await client.GetAsync($"{BaseUrl}/level");
            var currentLevel = await getResponse.Content.ReadFromJsonAsync<LogLevelResponse>(JsonOptions);
            currentLevel!.CurrentLevel.ShouldBe(newLevel);

            // Cleanup - sæt til originale level
            await client.PutAsJsonAsync($"{BaseUrl}/level", new LogLevelUpdateRequest(originalLevel.CurrentLevel));
        }

        [Theory]
        [InlineData(LoggingLevel.Verbose)]
        [InlineData(LoggingLevel.Debug)]
        [InlineData(LoggingLevel.Information)]
        [InlineData(LoggingLevel.Warning)]
        [InlineData(LoggingLevel.Error)]
        [InlineData(LoggingLevel.Fatal)]
        public async Task SetLogLevel_SupportsAllLevels(LoggingLevel level)
        {
            // Arrange
            var client = Factory.CreateAdminHttpClient();
            var request = new LogLevelUpdateRequest(level);

            // Act
            var response = await client.PutAsJsonAsync($"{BaseUrl}/level", request);

            // Assert
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<LogLevelUpdateResponse>(JsonOptions);
            content!.CurrentLevel.ShouldBe(level);
        }
    }
}