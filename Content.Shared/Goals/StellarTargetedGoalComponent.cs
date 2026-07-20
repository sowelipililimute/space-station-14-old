using Robust.Shared.GameStates;

namespace Content.Shared.Goals;

/// <summary>
/// Goal that focuses on a target.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(StellarTargetedGoalSystem))]
public sealed partial class StellarTargetedGoalComponent : Component
{
    /// <summary>
    /// The target entity of the goal
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Target;

    /// <summary>
    /// The type of the marker component for the target
    /// </summary>
    [DataField, AutoNetworkedField]
    public Type? TargetType;

    /// <summary>
    /// The title of the goal. Will have <see cref="Target"/> passed into it as "target" and a string passed as "targetName"
    /// </summary>
    [DataField]
    public LocId? Title;

    /// <summary>
    /// The description of the goal. Will have <see cref="Target"/> passed into it as "target" and a string passed as "targetName"
    /// </summary>
    [DataField]
    public LocId? Description;
}

/// <summary>
/// The supertype for marker components applied to the target of a goal (TODO: relations?)
/// </summary>
public abstract partial class StellarTargetedComponent : Component
{
    /// <summary>
    /// Goals that are observing this entity.
    /// </summary>
    [DataField]
    public List<EntityUid> Observers = [];
}

/// <summary>
/// Raised on a goal entity when its target changes
/// </summary>
/// <param name="Old">The old target of the goal</param>
/// <param name="New">The new target of the goal</param>
/// <param name="NewType">The marker type of the new target</param>
[ByRefEvent]
public readonly record struct StellarGoalTargetChangedEvent(EntityUid? Old, EntityUid? New, Type? NewType);

/// <summary>
/// Raised on a goal entity when determining the name of a target
/// </summary>
/// <param name="Target">The target to name</param>
/// <param name="Name">The name of the target</param>
[ByRefEvent]
public record struct StellarTargetNameEvent(EntityUid Target, string Name);
