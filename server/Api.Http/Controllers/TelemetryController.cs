using Application.Interfaces.Data.Repositories; 
using Application.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Api.Http.Controllers
{
    [AllowAnonymous] 
    [Route("api/telemetry")]
    [ApiController]
    public class TelemetryController : ControllerBase
    {
        private readonly ITelemetryRepository _telemetryRepository; 

        public TelemetryController(ITelemetryRepository telemetryRepository) 
        {
            _telemetryRepository = telemetryRepository;
        }
        
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TelemetryReading>>> GetAllAsync()
        {
            return Ok(await _telemetryRepository.GetAllReadingsAsync());
        }

        [HttpGet("{deviceId}")]
        public async Task<ActionResult<IEnumerable<TelemetryReading>>> GetByDeviceAsync(string deviceId)
        {
            return Ok(await _telemetryRepository.GetReadingsByDeviceAsync(deviceId)); 
        }
        
    }
}