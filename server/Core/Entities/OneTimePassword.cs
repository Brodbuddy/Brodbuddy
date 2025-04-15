using System;
using System.Collections.Generic;

namespace Core.Entities;

public partial class OneTimePassword
{
    public Guid Id { get; set; }

    public int Code { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public bool IsUsed { get; set; }

    public virtual ICollection<VerificationContext> VerificationContexts { get; set; } = new List<VerificationContext>();
}
