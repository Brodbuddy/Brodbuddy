using Microsoft.AspNetCore.Http;

namespace Api.Http.Models;

public class UploadFirmwareRequest
{
    public IFormFile File { get; set; } = null!;
    public string Version { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ReleaseNotes { get; set; }
    public bool IsStable { get; set; }
}

public class MakeFirmwareAvailableRequest  
{
    public Guid FirmwareVersionId { get; set; }
}