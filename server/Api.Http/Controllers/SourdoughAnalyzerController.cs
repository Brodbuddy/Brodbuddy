using Api.Http.Extensions;
using Api.Http.Models;
using Application.Models.DTOs;
using Application.Services.Sourdough;
using Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Http.Controllers;

[ApiController]
[Route("api/analyzers")]
public class SourdoughAnalyzerController : ControllerBase
{
    private readonly ISourdoughAnalyzerService _analyzerService;

    public SourdoughAnalyzerController(ISourdoughAnalyzerService analyzerService)
    {
        _analyzerService = analyzerService;
    }
    
    [HttpPost("register")]
    public async Task<ActionResult<RegisterAnalyzerResponse>> RegisterAnalyzer(RegisterAnalyzerRequest request)
    {
        var userId = HttpContext.User.GetUserId();
        
        var input = new RegisterAnalyzerInput(userId, request.ActivationCode, request.Nickname);
        var result = await _analyzerService.RegisterAnalyzerAsync(input);
        
        var response = new RegisterAnalyzerResponse(
            result.Analyzer.Id,
            result.Analyzer.Name ?? string.Empty,
            result.UserAnalyzer.Nickname,
            result.IsNewAnalyzer,
            result.UserAnalyzer.IsOwner ?? false
        );
        
        return Ok(response);
    }
    
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AnalyzerListResponse>>> GetUserAnalyzers()
    {
        var userId = HttpContext.User.GetUserId();
        var analyzers = await _analyzerService.GetUserAnalyzersAsync(userId);
        
        var response = analyzers.Select(a => 
        {
            var userAnalyzer = a.UserAnalyzers.FirstOrDefault(ua => ua.UserId == userId);
            return new AnalyzerListResponse(
                a.Id,
                a.Name ?? string.Empty,
                userAnalyzer?.Nickname,
                a.LastSeen,
                userAnalyzer?.IsOwner ?? false
            );
        });
        
        return Ok(response);
    }
    
    [HttpPost("admin/create")]
    [Authorize(Roles = Role.Admin)]
    public async Task<ActionResult<CreateAnalyzerResponse>> CreateAnalyzer(CreateAnalyzerRequest request)
    {
        var analyzer = await _analyzerService.CreateAnalyzerAsync(request.MacAddress, request.Name);
        
        return Ok(new CreateAnalyzerResponse(
            analyzer.Id,
            analyzer.MacAddress,
            analyzer.Name ?? string.Empty,
            analyzer.ActivationCode ?? string.Empty
        ));
    }
}