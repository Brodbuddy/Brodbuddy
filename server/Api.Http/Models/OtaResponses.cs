namespace Api.Http.Models;

public record UploadFirmwareResponse(
    Guid Id,
    string Version,
    long Size,
    uint Crc32
);

public record FirmwareVersionResponse(
    Guid Id,
    string Version,
    string Description,
    long FileSize,
    long Crc32,
    string? ReleaseNotes,
    bool IsStable,
    DateTime CreatedAt,
    Guid? CreatedBy
);