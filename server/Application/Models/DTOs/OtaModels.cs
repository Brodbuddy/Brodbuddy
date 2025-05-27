namespace Application.Models.DTOs;

public record OtaStartCommand(
    string AnalyzerId,
    string Version,
    long Size,
    uint Crc32
);

public record OtaChunkCommand(
    string AnalyzerId,
    byte[] ChunkData,
    uint ChunkIndex,
    uint ChunkSize
);

public record FirmwareMetadata(
    Guid FirmwareId,
    string Version,
    long Size,
    uint Crc32
);