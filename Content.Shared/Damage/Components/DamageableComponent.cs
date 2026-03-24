using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Damage.Components;

/// <summary>
///     Component that allows entities to take damage.
/// </summary>
/// <remarks>
///     The supported damage types are specified using a <see cref="DamageContainerPrototype"/>s. DamageContainers
///     may also have resistances to certain damage types, defined via a <see cref="DamageModifierSetPrototype"/>.
/// </remarks>
[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(DamageableSystem))]
public sealed partial class DamageableComponent : Component
{
    /// <summary>
    ///     This <see cref="DamageModifierSetPrototype"/> will be applied to any damage that is dealt to this container,
    ///     unless the damage explicitly ignores resistances.
    /// </summary>
    /// <remarks>
    ///     Though DamageModifierSets can be deserialized directly, we only want to use the prototype version here
    ///     to reduce duplication.
    /// </remarks>
    [DataField("damageModifierSet"), AutoNetworkedField]
    public ProtoId<DamageModifierSetPrototype>? DamageModifierSetId;

    [DataField("radiationDamageTypes"), AutoNetworkedField]
    // ReSharper disable once UseCollectionExpression - Cannot refactor this as it's a potential sandbox violation.
    public List<ProtoId<DamageTypePrototype>> RadiationDamageTypeIDs = new() { "Radiation" };
}

