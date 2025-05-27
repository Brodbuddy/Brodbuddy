using Api.Http.Extensions;
using Api.Http.Models;
using Application.Models.DTOs;
using Application.Services;
using Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Http.Controllers;

[ApiController]
[Route("api/ota")]
public class OtaController : ControllerBase
{
    private readonly IFirmwareManagementService _firmwareService;

    public OtaController(IFirmwareManagementService firmwareService)
    {
        _firmwareService = firmwareService;
    }

    [HttpPost("firmware")]
    [Authorize(Roles = Role.Admin)]
    public async Task<ActionResult<UploadFirmwareResponse>> UploadFirmware([FromForm] UploadFirmwareRequest request)
    {
        using var memoryStream = new MemoryStream();
        await request.File.CopyToAsync(memoryStream);
        var firmwareData = memoryStream.ToArray();
        
        var userId = HttpContext.User.GetUserId();
        
        var upload = new FirmwareUpload(
            firmwareData,
            request.Version,
            request.Description,
            request.ReleaseNotes,
            request.IsStable,
            userId
        );
        
        var metadata = await _firmwareService.UploadFirmwareAsync(upload);

        return Ok(new UploadFirmwareResponse(
            metadata.FirmwareId,
            metadata.Version,
            metadata.Size,
            metadata.Crc32
        ));
    }

    [HttpGet("firmware")]
    public async Task<ActionResult<IEnumerable<FirmwareVersionResponse>>> GetFirmwareVersions()
    {
        var versions = await _firmwareService.GetAllFirmwareVersionsAsync();
        
        var response = versions.Select(v => new FirmwareVersionResponse(
            v.Id,
            v.Version,
            v.Description,
            v.FileSize,
            v.Crc32, 
            v.ReleaseNotes,
            v.IsStable,
            v.CreatedAt,
            v.CreatedBy
        ));
        
        return Ok(response);
    }
}