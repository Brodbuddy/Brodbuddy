using System.Net;
using System.Net.Http.Json;
using Api.Http.Models;
using Api.Http.Utils;
using SharedTestDependencies.Constants;
using Shouldly;
using Startup.Tests.Infrastructure.Bases;
using Startup.Tests.Infrastructure.Extensions;
using Startup.Tests.Infrastructure.Fixtures;
using Xunit.Abstractions;

namespace Startup.Tests.Api.Http;

[Collection(TestCollections.Startup)]
public class FeatureToggleControllerTests(StartupTestFixture fixture, ITestOutputHelper output) : ApiTestBase(fixture, output)
{
    private const string BaseUrl = "/api/features";

    public class GetAllFeatures(StartupTestFixture fixture, ITestOutputHelper output) : FeatureToggleControllerTests(fixture, output)
    {
        [Fact]
        public async Task GetAllFeatures_WithoutAuth_Returns401()
        {
            // Arrange
            var client = Factory.CreateClient();

            // Act
            var response = await client.GetAsync(BaseUrl);

            // Assert
            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetAllFeatures_WithAuth_Returns200()
        {
            // Arrange
            var client = Factory.CreateAdminHttpClient();
            var featureName = $"test_feature_{Guid.NewGuid():N}";

            await client.PutAsJsonAsync($"{BaseUrl}/{featureName}", new FeatureToggleUpdateRequest(true));
            await client.PutAsJsonAsync($"{BaseUrl}/{featureName}/rollout", new FeatureToggleRolloutRequest(75));

            // Act
            var response = await client.GetAsync(BaseUrl);

            // Assert
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<FeatureToggleListResponse>();
            content.ShouldNotBeNull();
            content.Features.ShouldNotBeNull();

            var createdFeature = content.Features.FirstOrDefault(f => f.Name == featureName);
            createdFeature.ShouldNotBeNull();
            createdFeature.RolloutPercentage.ShouldBe(75);
        }
    }

    public class SetFeatureEnabled(StartupTestFixture fixture, ITestOutputHelper output) : FeatureToggleControllerTests(fixture, output)
    {
        [Fact]
        public async Task SetFeatureEnabled_WithValidRequest_Returns200()
        {
            // Arrange
            var client = Factory.CreateAdminHttpClient();
            var featureName = "test_feature";
            var request = new FeatureToggleUpdateRequest(true);

            // Act
            var response = await client.PutAsJsonAsync($"{BaseUrl}/{featureName}", request);

            // Assert
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task SetFeatureEnabled_WithoutAuth_Returns401()
        {
            // Arrange
            var client = Factory.CreateClient();
            var featureName = "test_feature";
            var request = new FeatureToggleUpdateRequest(true);

            // Act
            var response = await client.PutAsJsonAsync($"{BaseUrl}/{featureName}", request);

            // Assert
            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }
    }

    public class AddUserToFeature(StartupTestFixture fixture, ITestOutputHelper output) : FeatureToggleControllerTests(fixture, output)
    {
        [Fact]
        public async Task AddUserToFeature_WithNonExistentUser_Returns500()
        {
            // Arrange
            var client = Factory.CreateAdminHttpClient();
            var featureName = $"test_feature_{Guid.NewGuid():N}";
            var nonExistentUserId = Guid.NewGuid();

            // Opret feature først
            await client.PutAsJsonAsync($"{BaseUrl}/{featureName}", new FeatureToggleUpdateRequest(true));

            // Act  
            var response = await client.PostAsync($"{BaseUrl}/{featureName}/users/{nonExistentUserId}", null);

            // Assert - Forventer fejl da bruger ikke eksisterer  
            response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        }

        [Fact]
        public async Task AddUserToFeature_WithNonExistentFeature_Returns404()
        {
            // Arrange
            var client = Factory.CreateAdminHttpClient();
            var featureName = "non_existent_feature";
            var userId = Guid.NewGuid();

            // Act
            var response = await client.PostAsync($"{BaseUrl}/{featureName}/users/{userId}", null);

            // Assert
            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }
        
        [Fact]
        public async Task AddUserToFeature_WithActualUser_EnablesFeatureForUser()
        {
            // Arrange
            var adminClient = Factory.CreateAdminHttpClient();
            var featureName = $"user_feature_{Guid.NewGuid():N}";
        
            await adminClient.PutAsJsonAsync($"{BaseUrl}/{featureName}", new FeatureToggleUpdateRequest(false));
        
            var getResponse = await adminClient.GetAsync(BaseUrl);
            var features = await getResponse.Content.ReadFromJsonAsync<FeatureToggleListResponse>();
            features!.Features.First(f => f.Name == featureName).IsEnabled.ShouldBeFalse();
        
            // Act & Assert
            var nonExistentUserId = Guid.NewGuid();
            var response = await adminClient.PostAsync($"{BaseUrl}/{featureName}/users/{nonExistentUserId}", null);
            response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        
            var user = await SeedUserAsync();
            response = await adminClient.PostAsync($"{BaseUrl}/{featureName}/users/{user.Id}", null);
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
        
            getResponse = await adminClient.GetAsync(BaseUrl);
            features = await getResponse.Content.ReadFromJsonAsync<FeatureToggleListResponse>();
            features!.Features.First(f => f.Name == featureName).IsEnabled.ShouldBeFalse();
        
            response = await adminClient.DeleteAsync($"{BaseUrl}/{featureName}/users/{user.Id}");
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }
    }

    public class RemoveUserFromFeature(StartupTestFixture fixture, ITestOutputHelper output) : FeatureToggleControllerTests(fixture, output)
    {
        [Fact]
        public async Task RemoveUserFromFeature_WithNonExistentFeature_Returns404()
        {
            // Arrange
            var client = Factory.CreateAdminHttpClient();
            var featureName = "non_existent_feature";
            var userId = Guid.NewGuid();

            // Act
            var response = await client.DeleteAsync($"{BaseUrl}/{featureName}/users/{userId}");

            // Assert
            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task RemoveUserFromFeature_WithNonExistentUser_Returns404()
        {
            // Arrange
            var client = Factory.CreateAdminHttpClient();
            var featureName = "test_feature";
            var userId = Guid.NewGuid();

            // Opret feature først
            await client.PutAsJsonAsync($"{BaseUrl}/{featureName}", new FeatureToggleUpdateRequest(true));

            // Act
            var response = await client.DeleteAsync($"{BaseUrl}/{featureName}/users/{userId}");

            // Assert
            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task RemoveUserFromFeature_WithoutAuth_Returns401()
        {
            // Arrange
            var client = Factory.CreateClient();
            var featureName = "test_feature";
            var userId = Guid.NewGuid();

            // Act
            var response = await client.DeleteAsync($"{BaseUrl}/{featureName}/users/{userId}");

            // Assert
            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }
    }
    
    public class SetRolloutPercentage(StartupTestFixture fixture, ITestOutputHelper output) : FeatureToggleControllerTests(fixture, output)
    {
        [Fact]
        public async Task SetRolloutPercentage_WithValidRequest_Returns200()
        {
            // Arrange
            var client = Factory.CreateAdminHttpClient();
            var featureName = $"rollout_feature_{Guid.NewGuid():N}";
            var request = new FeatureToggleRolloutRequest(50);

            await client.PutAsJsonAsync($"{BaseUrl}/{featureName}", new FeatureToggleUpdateRequest(true));

            // Act
            var response = await client.PutAsJsonAsync($"{BaseUrl}/{featureName}/rollout", request);

            // Assert
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task SetRolloutPercentage_WithInvalidPercentage_Returns400()
        {
            // Arrange
            var client = Factory.CreateAdminHttpClient();
            var featureName = $"rollout_feature_{Guid.NewGuid():N}";

            await client.PutAsJsonAsync($"{BaseUrl}/{featureName}", new FeatureToggleUpdateRequest(true));

            // Act
            var response1 = await client.PutAsJsonAsync($"{BaseUrl}/{featureName}/rollout",
                new FeatureToggleRolloutRequest(-1));
            var response2 = await client.PutAsJsonAsync($"{BaseUrl}/{featureName}/rollout",
                new FeatureToggleRolloutRequest(101));

            // Assert
            response1.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            response2.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task SetRolloutPercentage_WithNonExistentFeature_Returns404()
        {
            // Arrange
            var client = Factory.CreateAdminHttpClient();
            var featureName = $"non_existent_{Guid.NewGuid():N}";
            var request = new FeatureToggleRolloutRequest(50);

            // Act
            var response = await client.PutAsJsonAsync($"{BaseUrl}/{featureName}/rollout", request);

            // Assert
            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task SetRolloutPercentage_WithoutAuth_Returns401()
        {
            // Arrange
            var client = Factory.CreateClient();
            var featureName = "test_feature";
            var request = new FeatureToggleRolloutRequest(50);

            // Act
            var response = await client.PutAsJsonAsync($"{BaseUrl}/{featureName}/rollout", request);

            // Assert
            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }
    }
    
    public class FeatureToggleMiddleware(StartupTestFixture fixture, ITestOutputHelper output) : FeatureToggleControllerTests(fixture, output)
    {
        [Fact]
        public async Task FeatureToggleMiddleware_DisabledEndpoint_Returns404()
        {
            // Arrange
            var featureName = "Api.PasswordlessAuth.InitiateLogin";
            var adminClient = Factory.CreateAdminHttpClient();
            var regularClient = Factory.CreateClient();
            var request = new InitiateLoginRequest("test@example.com");

            // Act & Assert - Disabled endpoint
            await adminClient.PutAsJsonAsync($"{BaseUrl}/{featureName}", new FeatureToggleUpdateRequest(false));

            var response = await regularClient.PostAsJsonAsync(Routes.PasswordlessAuth.Initiate, request);
            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);

            // Act & Assert - Enabled endpoint
            await adminClient.PutAsJsonAsync($"{BaseUrl}/{featureName}", new FeatureToggleUpdateRequest(true));

            response = await regularClient.PostAsJsonAsync(Routes.PasswordlessAuth.Initiate, request);
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }
    }

    public class FullCycle(StartupTestFixture fixture, ITestOutputHelper output) : FeatureToggleControllerTests(fixture, output)
    {
        [Fact]
        public async Task FullCycle_CreateFeatureAndManageUsers_WorksCorrectly()
        {
            // Arrange
            var client = Factory.CreateAdminHttpClient();
            var featureName = $"test_feature_{Guid.NewGuid():N}";

            // Act & Assert
            var createResponse = await client.PutAsJsonAsync($"{BaseUrl}/{featureName}", new FeatureToggleUpdateRequest(true));
            createResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

            var getResponse = await client.GetAsync(BaseUrl);
            getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

            var features = await getResponse.Content.ReadFromJsonAsync<FeatureToggleListResponse>();
            features.ShouldNotBeNull();
            features.Features.Any(f => f.Name == featureName).ShouldBeTrue();

            var disableResponse = await client.PutAsJsonAsync($"{BaseUrl}/{featureName}", new FeatureToggleUpdateRequest(false));
            disableResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        }
    }
}