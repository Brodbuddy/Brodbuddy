using Api.Http.Models;
using Application.Services;
using Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Http.Controllers;

[ApiController]
[Route("api/features")]
public class FeatureToggleController : ControllerBase
{
    private readonly IFeatureToggleService _featureToggleService;
    
    public FeatureToggleController(IFeatureToggleService featureToggleService)
    {
        _featureToggleService = featureToggleService;
    }
    
    [HttpGet]
    [Authorize(Roles = Role.Admin)]
    public async Task<ActionResult<FeatureToggleListResponse>> GetAllFeatures()
    {
        var features = await _featureToggleService.GetAllFeaturesAsync();
        var response = features.Select(f => new FeatureToggleResponse(
            f.Id,
            f.Name,
            f.Description,
            f.IsEnabled,
            f.RolloutPercentage,
            f.CreatedAt,
            f.LastModifiedAt
        ));
        return new FeatureToggleListResponse(response);
    }
    
    [HttpPut("{featureName}")]
    [Authorize(Roles = Role.Admin)]
    public async Task<ActionResult> SetFeatureEnabled(string featureName, [FromBody] FeatureToggleUpdateRequest request)
    {
        var success = await _featureToggleService.SetFeatureEnabledAsync(featureName, request.IsEnabled);
        return success ? Ok() : BadRequest();
    }
    
    [HttpPost("{featureName}/users/{userId:guid}")]
    [Authorize(Roles = Role.Admin)]
    public async Task<ActionResult> AddUserToFeature(string featureName, Guid userId)
    {
        var success = await _featureToggleService.AddUserToFeatureAsync(featureName, userId);
        return success ? Ok() : NotFound();
    }
    
    [HttpDelete("{featureName}/users/{userId:guid}")]
    [Authorize(Roles = Role.Admin)]
    public async Task<ActionResult> RemoveUserFromFeature(string featureName, Guid userId)
    {
        var success = await _featureToggleService.RemoveUserFromFeatureAsync(featureName, userId);
        return success ? Ok() : NotFound();
    }
    
    [HttpPut("{featureName}/rollout")]
    [Authorize(Roles = Role.Admin)]
    public async Task<ActionResult> SetRolloutPercentage(string featureName, [FromBody] FeatureToggleRolloutRequest request)
    {
        var success = await _featureToggleService.SetRolloutPercentageAsync(featureName, request.Percentage);
        return success ? Ok() : NotFound();
    }
}