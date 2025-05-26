using System;
using System.Collections.Generic;

namespace Core.Entities;

public partial class FirmwareUpdate
{
    public Guid Id { get; set; }

    public Guid AnalyzerId { get; set; }

    public Guid FirmwareVersionId { get; set; }

    public string Status { get; set; } = null!;

    public int Progress { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public virtual SourdoughAnalyzer Analyzer { get; set; } = null!;

    public virtual FirmwareVersion FirmwareVersion { get; set; } = null!;
}
