namespace Application.Models.DTOs;

public record FirmwareUpload(
    byte[] Data,
    string Version,
    string Description,
    string? ReleaseNotes,
    bool IsStable,
    Guid? CreatedBy
);