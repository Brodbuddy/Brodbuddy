using Api.Http.Extensions;
using Api.Http.Models;
using Application.Models.DTOs;
using Application.Services.Sourdough;
using Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Http.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalyzerController : ControllerBase
{
    private readonly ISourdoughAnalyzerService _analyzerService;

    public AnalyzerController(ISourdoughAnalyzerService analyzerService)
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
        var analyzersWithStatus = await _analyzerService.GetUserAnalyzersAsync(userId);
        
        var response = analyzersWithStatus.Select(aws => 
        {
            var userAnalyzer = aws.Analyzer.UserAnalyzers.FirstOrDefault(ua => ua.UserId == userId);
            return new AnalyzerListResponse(
                aws.Analyzer.Id,
                aws.Analyzer.Name ?? string.Empty,
                userAnalyzer?.Nickname,
                aws.Analyzer.LastSeen,
                userAnalyzer?.IsOwner ?? false,
                aws.Analyzer.FirmwareVersion,
                aws.HasUpdate
            );
        });
        
        return Ok(response);
    }
    
    [HttpGet("admin/all")]
    [Authorize(Roles = Role.Admin)]
    public async Task<ActionResult<IEnumerable<AdminAnalyzerListResponse>>> GetAllAnalyzers()
    {
        var analyzers = await _analyzerService.GetAllAnalyzersAsync();
        
        var response = analyzers.Select(a => new AdminAnalyzerListResponse(
            a.Id,
            a.Name ?? string.Empty,
            a.MacAddress,
            a.FirmwareVersion,
            a.IsActivated,
            a.ActivatedAt,
            a.LastSeen,
            a.CreatedAt
        ));
        
        return Ok(response);
    }
    
    [HttpPost("admin/create")]
    [Authorize(Roles = Role.Admin)]
    public async Task<ActionResult<CreateAnalyzerResponse>> CreateAnalyzer(CreateAnalyzerRequest request)
    {
        var analyzer = await _analyzerService.CreateAnalyzerAsync(request.MacAddress, request.Name);
        
        var formattedActivationCode = SourdoughAnalyzerService.FormatActivationCodeForDisplay(analyzer.ActivationCode ?? string.Empty);
        
        return Ok(new CreateAnalyzerResponse(
            analyzer.Id,
            analyzer.MacAddress,
            analyzer.Name ?? string.Empty,
            formattedActivationCode
        ));
    }
}