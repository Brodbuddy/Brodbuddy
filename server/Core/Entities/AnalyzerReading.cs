using System;
using System.Collections.Generic;

namespace Core.Entities;

public partial class AnalyzerReading
{
    public Guid Id { get; set; }

    public Guid AnalyzerId { get; set; }

    public long EpochTime { get; set; }

    public Guid UserId { get; set; }

    public DateTime Timestamp { get; set; }

    public DateTime LocalTime { get; set; }

    public decimal? Temperature { get; set; }

    public decimal? Humidity { get; set; }

    public decimal? Rise { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual SourdoughAnalyzer Analyzer { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
