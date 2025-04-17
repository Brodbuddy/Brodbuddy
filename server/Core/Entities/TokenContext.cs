using System;
using System.Collections.Generic;

namespace Core.Entities;

public partial class TokenContext
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid DeviceId { get; set; }

    public Guid RefreshTokenId { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsRevoked { get; set; }

    public virtual Device Device { get; set; } = null!;

    public virtual RefreshToken RefreshToken { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
