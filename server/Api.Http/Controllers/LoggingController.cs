using Api.Http.Models;
using Application.Interfaces.Monitoring;
using Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Http.Controllers;

[ApiController]
[Route("api/logging")]
public class LoggingController : ControllerBase
{
    private readonly ILogLevelManager _logLevelManager;
    
    public LoggingController(ILogLevelManager logLevelManager)
    {
        _logLevelManager = logLevelManager;
    }
    
    [HttpGet("level")]
    [AllowAnonymous]
    public ActionResult<LogLevelResponse> GetCurrentLogLevel()
    {
        var currentLevel = _logLevelManager.GetCurrentLevel();
        return new LogLevelResponse(currentLevel);
    }
    
    [HttpPut("level")]
    [AllowAnonymous]
    public ActionResult<LogLevelUpdateResponse> SetLogLevel([FromBody] LogLevelUpdateRequest request)
    {
        _logLevelManager.SetLogLevel(request.LogLevel);
        return new LogLevelUpdateResponse("Log level updated", request.LogLevel);
    }
}