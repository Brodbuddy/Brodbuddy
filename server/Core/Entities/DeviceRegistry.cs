using System;
using System.Collections.Generic;

namespace Core.Entities;

public partial class DeviceRegistry
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid DeviceId { get; set; }

    public string Fingerprint { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Device Device { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
