using System;
using System.Collections.Generic;

namespace Core.Entities;

public partial class SourdoughAnalyzer
{
    public Guid Id { get; set; }

    public string MacAddress { get; set; } = null!;

    public string? Name { get; set; }

    public string? FirmwareVersion { get; set; }

    public string? ActivationCode { get; set; }

    public bool IsActivated { get; set; }

    public DateTime? ActivatedAt { get; set; }

    public DateTime? LastSeen { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<AnalyzerReading> AnalyzerReadings { get; set; } = new List<AnalyzerReading>();

    public virtual ICollection<FirmwareUpdate> FirmwareUpdates { get; set; } = new List<FirmwareUpdate>();

    public virtual ICollection<UserAnalyzer> UserAnalyzers { get; set; } = new List<UserAnalyzer>();
}
