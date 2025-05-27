using System;
using System.Collections.Generic;

namespace Core.Entities;

public partial class User
{
    public Guid Id { get; set; }

    public string Email { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<AnalyzerReading> AnalyzerReadings { get; set; } = new List<AnalyzerReading>();

    public virtual ICollection<DeviceRegistry> DeviceRegistries { get; set; } = new List<DeviceRegistry>();

    public virtual ICollection<FeatureUser> FeatureUsers { get; set; } = new List<FeatureUser>();

    public virtual ICollection<FirmwareVersion> FirmwareVersions { get; set; } = new List<FirmwareVersion>();

    public virtual ICollection<TokenContext> TokenContexts { get; set; } = new List<TokenContext>();

    public virtual ICollection<UserAnalyzer> UserAnalyzers { get; set; } = new List<UserAnalyzer>();

    public virtual ICollection<UserRole> UserRoleCreatedByNavigations { get; set; } = new List<UserRole>();

    public virtual ICollection<UserRole> UserRoleUsers { get; set; } = new List<UserRole>();

    public virtual ICollection<VerificationContext> VerificationContexts { get; set; } = new List<VerificationContext>();
}
