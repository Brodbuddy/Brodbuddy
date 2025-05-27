using Application.Services.Sourdough;
using Core.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Api.Http.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalyzerReadingsController : ControllerBase
{
    private readonly SourdoughReadingsService _readingsService;

    public AnalyzerReadingsController(SourdoughReadingsService readingsService)
    {
        _readingsService = readingsService;
    }

    [HttpGet("{analyzerId}/latest")]
    public async Task<ActionResult<AnalyzerReading?>> GetLatestReading(Guid analyzerId)
    {
        var reading = await _readingsService.GetLatestReadingAsync(analyzerId);
        return reading == null ? NotFound() : Ok(reading);
    }

    [HttpGet("{analyzerId}/cache")]
    public async Task<ActionResult<IEnumerable<AnalyzerReading>>> GetReadingsForCache(
        Guid analyzerId,
        [FromQuery] int maxResults = 500)
    {
        var readings = await _readingsService.GetLatestReadingsForCachingAsync(analyzerId, maxResults);
        return Ok(readings);
    }

    [HttpGet("{analyzerId}")]
    public async Task<ActionResult<IEnumerable<AnalyzerReading>>> GetReadings(
        Guid analyzerId,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? toDate = null)
    {
        var readings = await _readingsService.GetReadingsByTimeRangeAsync(analyzerId, from, toDate);
        return Ok(readings);
    }
}