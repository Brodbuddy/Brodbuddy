using System.Net;
using SharedTestDependencies.Constants;
using SharedTestDependencies.Fixtures;
using Shouldly;
using Startup.Tests.Infrastructure.Fixtures;
using Startup.Tests.Infrastructure.Bases;
using Startup.Tests.Infrastructure.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace Startup.Tests.Api.Http;

[Collection(TestCollections.Startup)]
public class AnalyzerReadingsControllerTests(StartupTestFixture fixture, ITestOutputHelper output) : ApiTestBase(fixture, output)
{
    [Fact]
    public async Task GetLatestReading_WithValidAnalyzerId_ReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var client = Factory.CreateMemberHttpClient(userId);
        var analyzerId = Guid.NewGuid();
        
        // Act
        var response = await client.GetAsync($"/api/analyzerreadings/{analyzerId}/latest");
        
        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}