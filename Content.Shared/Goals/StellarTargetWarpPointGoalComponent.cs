using Content.Shared.Whitelist;

namespace Content.Shared.Goals;

/// <summary>
/// Targets a random warp point
/// </summary>
[RegisterComponent]
public sealed partial class StellarTargetWarpPointGoalComponent : Component
{
    /// <summary>
    /// Warp points matching this blacklist will be excluded
    /// </summary>
    [DataField(required: true)]
    public EntityWhitelist? Blacklist;
}
