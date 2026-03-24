using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Damage.Components;

/// <summary>
/// 	Component that allows an entity to have a set of injuries that it tracks.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(InjurableSystem), Other = AccessPermissions.ReadExecute)]
public sealed partial class InjurableComponent : Component
{
    /// <summary>
    ///     This <see cref="InjuryContainerPrototype"/> specifies what injury types are supported by this component.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<InjuryContainerPrototype> InjuryContainer;

    /// <summary>
    ///     Injuries, indexed by <see cref="InjuryGroupPrototype"/> ID keys.
    /// </summary>
    [ViewVariables]
    [Access(typeof(InjurableSystem), Other = AccessPermissions.None)]
    public Dictionary<ProtoId<DamageGroupPrototype>, FixedPoint2> InjuriesPerGroup = new();

    /// <summary>
    ///     The sum of all injuries in the <see cref="Injuries" />
    /// </summary>
    [ViewVariables]
    [Access(typeof(InjurableSystem), Other = AccessPermissions.None)]
    public FixedPoint2 TotalInjuries;


    /// <summary>
    ///     All the damage information is stored in this <see cref="DamageSpecifier"/>.
    /// </summary>
    /// <remarks>
    ///     If this data-field is specified, this allows damageable components to be initialized with non-zero damage.
    /// </remarks>
    [DataField, AutoNetworkedField]
    [Access(typeof(InjurableSystem), Other = AccessPermissions.None)]
    public DamageSpecifier Injuries = new();

    [DataField]
    public Dictionary<MobState, ProtoId<HealthIconPrototype>> HealthIcons = new()
    {
        { MobState.Alive, "HealthIconFine" },
        { MobState.Critical, "HealthIconCritical" },
        { MobState.Dead, "HealthIconDead" },
    };

    [DataField]
    public ProtoId<HealthIconPrototype> RottingIcon = "HealthIconRotting";

    /// <summary>
    ///     Group types that affect the pain overlay.
    /// </summary>
    ///     TODO: Add support for adding damage types specifically rather than whole damage groups
    [DataField]
    // ReSharper disable once UseCollectionExpression - Cannot refactor this as it's a potential sandbox volation.
    public List<ProtoId<DamageGroupPrototype>> PainDamageGroups = new() { "Brute", "Burn" };

    [DataField, AutoNetworkedField]
    public FixedPoint2? HealthBarThreshold;
}
