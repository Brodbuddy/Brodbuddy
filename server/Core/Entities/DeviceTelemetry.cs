using System;
using System.Collections.Generic;

namespace Core.Entities;

public partial class DeviceTelemetry
{
    public Guid Id { get; set; }

    public string DeviceId { get; set; } = null!;

    public double Distance { get; set; }

    public double RisePercentage { get; set; }

    public DateTime Timestamp { get; set; }

    public DateTime CreatedAt { get; set; }
}
