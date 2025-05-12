using Api.Websocket.EventHandlers;
using SharedTestDependencies.Constants;
using Startup.Tests.Websocket;
using Xunit.Abstractions;
using Shouldly;
using Startup.Tests.WebSocket;

namespace Startup.Tests.ApiTests;

[Collection(TestCollections.Startup)]
public class JoinRoomTests(StartupTestFixture fixture, ITestOutputHelper output) : ApiTestBase(fixture, output)
{
    [Fact]
    public async Task JoinRoom_ShouldSucceed_WhenValidRequest()
    {
        await using var client = Factory.CreateWebSocketClient(Output, "vanvittigtjo");
    
        var response = await client.SendAndWaitAsync<JoinRoom, UserJoined>("JoinRoom", new JoinRoom("room1", "Alice"));
    
        response.ShouldNotBeNull();
        response.Username.ShouldBe("Alice");
        response.RoomId.ShouldBe("room1");
    }
    
    [Fact]
    public async Task JoinRoom_ShouldSucceed_WithScenario()
    {
        await using var client = Factory.CreateWebSocketClient(Output, "okiiii");

        await client.CreateScenario("Alice joins room")
            .Send("JoinRoom", new JoinRoom("room1", "Alice"))
            .ExpectResponse<UserJoined>("UserJoined", response => 
            {
                response.Username.ShouldBe("Alice");
                response.RoomId.ShouldBe("room1");
            })
            .ExecuteAsync();
    }
    
    [Fact]
    public async Task JoinRoom_ShouldHandleMultipleUsersWithChaining()
    {
        await using var client = Factory.CreateWebSocketClient(Output, "hallo");

        await client.CreateScenario("Multiple users join the same room")
            .SendAndExpect<JoinRoom, UserJoined>("JoinRoom",
                new JoinRoom("room1", "Alice"),
                response => response.Username.ShouldBe("Alice"))
            .SendAndExpect<JoinRoom, UserJoined>("JoinRoom",
                new JoinRoom("room1", "Bob"),
                response => response.Username.ShouldBe("Bob"))
            .SendAndExpect<JoinRoom, UserJoined>("JoinRoom",
                new JoinRoom("room1", "Charlie"),
                response => response.Username.ShouldBe("Charlie"))
            .ExecuteAsync();
    }

    [Fact]
    public async Task JoinRoom_ShouldHandleErrorScenarios()
    {
        await using var client = Factory.CreateWebSocketClient(Output, "hejmeddig");

        await client.CreateScenario("Test validation errors")
            .Send("JoinRoom", new JoinRoom("room1", ""))
            .ExpectError(error => 
            {
                error.Code.ShouldBe("VALIDATION_ERROR");
                error.Message.ShouldContain("Username");
            })
            .ExecuteAsync();
    }

    [Fact]
    public async Task MultipleClients_ShouldJoinRoom_Simultaneously()
    {
        await using var alice = Factory.CreateWebSocketClient(Output, "ostehaps");
        await using var bob = Factory.CreateWebSocketClient(Output, "chokoladestang");
        await using var charlie = Factory.CreateWebSocketClient(Output, "kanelkartoffel");

        var aliceScenario = alice.CreateScenario("Alice joins")
            .SendAndExpect<JoinRoom, UserJoined>("JoinRoom", 
                new JoinRoom("roomLOL", "Alice"),
                r => r.Username.ShouldBe("Alice"));

        var bobScenario = bob.CreateScenario("Bob joins")
            .SendAndExpect<JoinRoom, UserJoined>("JoinRoom", 
                new JoinRoom("roomLOL", "Bob"),
                r => r.Username.ShouldBe("Bob"));

        var charlieScenario = charlie.CreateScenario("Charlie joins")
            .SendAndExpect<JoinRoom, UserJoined>("JoinRoom", 
                new JoinRoom("roomLOL", "Charlie"),
                r => r.Username.ShouldBe("Charlie"));

        await WebSocketTestScenario.InParallel(aliceScenario, bobScenario, charlieScenario).ExecuteAsync();
    }
    
}