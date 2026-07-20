using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Goals;

/// <summary>
/// Represents a goal that can be contained by a <see cref="StellarGoalContainerComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(StellarGoalsSystem), Other = AccessPermissions.None)]
public sealed partial class StellarGoalComponent : Component
{
    /// <summary>
    /// The container this goal belongs to
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid Container;

    /// <summary>
    /// The current progress of the goal
    /// </summary>
    [DataField, AutoNetworkedField]
    [Access(typeof(StellarGoalsSystem), Other = AccessPermissions.Read)]
    public double Progress = 0f;

    /// <summary>
    /// A sprite used to represent the goal in the UI
    /// </summary>
    [DataField]
    [Access(typeof(StellarGoalsSystem), Other = AccessPermissions.Read)]
    public SpriteSpecifier? Icon;
}

/// <summary>
/// Raised on a goal when it gets inserted into a container
/// </summary>
/// <param name="Container">The container the goal is being inserted into</param>
[ByRefEvent]
public record struct StellarGoalInsertedEvent(Entity<StellarGoalContainerComponent> Container);

/// <summary>
/// Raised on a goal to compute its progress.
/// </summary>
/// <param name="Progress">The computed progress of the goal, from 0 to 1.</param>
[ByRefEvent]
public record struct StellarGetGoalProgressEvent(double Progress);

/// <summary>
/// Raised when a goal changes progress.
/// </summary>
/// <param name="Old">The old progress of the goal.</param>
/// <param name="New">The new progress of the goal.</param>
[ByRefEvent]
public readonly record struct StellarGoalProgressChangedEvent(double Old, double New);
