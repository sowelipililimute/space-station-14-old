using Robust.Shared.GameStates;

namespace Content.Shared.Goals;

/// <summary>
/// Represents an entity that can contain <see cref="StellarGoalComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(StellarGoalsSystem), Other = AccessPermissions.None)]
public sealed partial class StellarGoalContainerComponent : Component
{
    /// <summary>
    /// The current set of contained goals
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<EntityUid> Goals = new();

    /// <summary>
    /// If this goal container is an individual one, this entity is its owner
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? IndividualOwner;
}
