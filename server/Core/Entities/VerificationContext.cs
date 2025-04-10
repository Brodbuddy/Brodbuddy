using System;
using System.Collections.Generic;

namespace Core.Entities;

public partial class VerificationContext
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid OtpId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual OneTimePassword Otp { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
