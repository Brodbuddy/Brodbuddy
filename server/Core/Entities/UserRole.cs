using System;

namespace Core.Entities;

public partial class UserRole
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid RoleId { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public virtual Role Role { get; set; } = null!;

    public virtual User User { get; set; } = null!;

    public virtual User? CreatedByUser { get; set; }
}