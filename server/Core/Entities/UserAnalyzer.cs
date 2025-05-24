using System;
using System.Collections.Generic;

namespace Core.Entities;

public partial class UserAnalyzer
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid AnalyzerId { get; set; }

    public bool? IsOwner { get; set; }

    public string? Nickname { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual SourdoughAnalyzer Analyzer { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
