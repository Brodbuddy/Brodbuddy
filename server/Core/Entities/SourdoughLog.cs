using System;
using System.Collections.Generic;

namespace Core.Entities;

public partial class SourdoughLog
{
    public Guid Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public string Content { get; set; } = null!;

    public string? Status { get; set; }

    public int? RisingTime { get; set; }
}
