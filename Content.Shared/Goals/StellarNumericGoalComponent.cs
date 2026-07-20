using Robust.Shared.GameStates;

namespace Content.Shared.Goals;

/// <summary>
/// A goal that sets its progress based on comparing a current value to a target value
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(StellarNumericGoalSystem), Other = AccessPermissions.None)]
public sealed partial class StellarNumericGoalComponent : Component
{
    /// <summary>
    /// The current value of the goal
    /// </summary>
    [DataField, AutoNetworkedField]
    public double Current = 0d;

    /// <summary>
    /// The target value of the goal
    /// </summary>
    [DataField, AutoNetworkedField]
    public double Target = 1d;

    /// <summary>
    /// If set, this will randomize the goal's <see cref="Target"/>
    /// </summary>
    /// <seealso cref="TargetRangeResolution"/>
    [DataField]
    public (double Min, double Max)? TargetRange;

    /// <summary>
    /// The minimum difference between any two values that can be chosen by <see cref="TargetRange"/>
    /// </summary>
    [DataField]
    public double TargetRangeResolution = 1d;

    /// <summary>
    /// The title of the goal. Will have <see cref="Target"/> passed into it as "target"
    /// </summary>
    [DataField]
    public LocId? Title;

    /// <summary>
    /// The description of the goal. Will have <see cref="Target"/> passed into it as "target"
    /// </summary>
    [DataField]
    public LocId? Description;
}
