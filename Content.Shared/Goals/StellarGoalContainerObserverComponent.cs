using Robust.Shared.GameStates;

namespace Content.Shared.Goals;

/// <summary>
/// Represents a mind entity that can observe the goals contained within a <see cref="StellarGoalContainerComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(StellarGoalsSystem), Other = AccessPermissions.None)]
public sealed partial class StellarGoalContainerObserverComponent : Component
{
    /// <summary>
    /// Goal containers that are being observed by this mind.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<EntityUid> Observed = [];

    /// <summary>
    /// The container owned by this mind, if it has personal goals.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? OwnedContainer;
}
