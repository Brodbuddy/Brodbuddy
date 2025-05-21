namespace Application.Models.DTOs;

public enum RolloutStrategy
{
    Disabled,
    Enabled,
    Percentage,
    UserList,
    RoleBased
}

public class FeatureRollout
{
    public required string FeatureName { get; set; }
    public RolloutStrategy Strategy { get; set; }
    public int? Percentage { get; set; }
}