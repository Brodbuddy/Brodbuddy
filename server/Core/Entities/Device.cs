﻿using System;
using System.Collections.Generic;

namespace Core.Entities;

public partial class Device
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string Browser { get; set; } = null!;

    public string Os { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime LastSeenAt { get; set; }

    public bool IsActive { get; set; }

    public string? CreatedByIp { get; set; }

    public string? UserAgent { get; set; }

    public virtual ICollection<DeviceRegistry> DeviceRegistries { get; set; } = new List<DeviceRegistry>();

    public virtual ICollection<TokenContext> TokenContexts { get; set; } = new List<TokenContext>();
}
