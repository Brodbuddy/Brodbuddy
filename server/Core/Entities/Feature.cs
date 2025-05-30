﻿using System;
using System.Collections.Generic;

namespace Core.Entities;

public partial class Feature
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsEnabled { get; set; }

    public int? RolloutPercentage { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? LastModifiedAt { get; set; }

    public virtual ICollection<FeatureUser> FeatureUsers { get; set; } = new List<FeatureUser>();
}
