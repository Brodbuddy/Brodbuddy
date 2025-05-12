using System.Net;
using SharedTestDependencies.Constants;
using Shouldly;
using Startup.Tests.Infrastructure.Bases;
using Startup.Tests.Infrastructure.Fixtures;
using Xunit.Abstractions;

namespace Startup.Tests.Api.Http;

[Collection(TestCollections.Startup)]
public class BasicHttpTests(StartupTestFixture fixture, ITestOutputHelper output) : ApiTestBase(fixture, output)
{
    [Fact]
    public async Task RootEndpoint_ShouldReturn200()
    {
        // Act
        var response = await Factory.CreateClient().GetAsync("/");
            
        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        Output.WriteLine($"Response: {content}");
        content.ShouldContain("Hej");
    }
    
    [Fact]
    public async Task RootEndpoint_ShouldReturn1200()
    {
        // Act
        var response = await Factory.CreateClient().GetAsync("/");
            
        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        Output.WriteLine($"Response: {content}");
        content.ShouldContain("nu");
    }
    
    [Fact]
    public async Task RootEndpoint_ShouldRetur200()
    {
        // Act
        var response = await Factory.CreateClient().GetAsync("/");
            
        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        Output.WriteLine($"Response: {content}");
        content.ShouldContain("med");
    } 
}