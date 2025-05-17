using System;
using System.Collections.Generic;

namespace Core.Entities;

public partial class FeatureUser
{
    public Guid Id { get; set; }

    public Guid FeatureId { get; set; }

    public Guid UserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Feature Feature { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
