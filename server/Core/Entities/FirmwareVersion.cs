namespace Core.Entities;

public partial class FirmwareVersion
{
    public Guid Id { get; set; }

    public string Version { get; set; } = null!;

    public string Description { get; set; } = null!;

    public long FileSize { get; set; }

    public long Crc32 { get; set; }

    public string? FileUrl { get; set; }

    public string? ReleaseNotes { get; set; }

    public bool IsStable { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public virtual User? CreatedByNavigation { get; set; }

    public virtual ICollection<FirmwareUpdate> FirmwareUpdates { get; set; } = new List<FirmwareUpdate>();
}
