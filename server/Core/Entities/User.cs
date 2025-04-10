using System;
using System.Collections.Generic;

namespace Core.Entities;

public partial class User
{
    public Guid Id { get; set; }

    public string Email { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<DeviceRegistry> DeviceRegistries { get; set; } = new List<DeviceRegistry>();
}
