using Content.Shared.Damage.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Damage;

/// <summary>
/// Defines an "attack" dealt onto a <see cref="DamageableComponent" /> entity
/// </summary>
/// <param name="Damages">The amount of damage this attack deals</param>
/// <param name="IgnoreResistances">If this attack should ignore resistances</param>
/// <param name="InterruptsDoAfters">If this attack should interrupt do-afters</param>
/// <param name="Origin">The originating entity of this attack</param>
/// <param name="IgnoreGlobalModifiers">If this attack should ignore global modifiers</param>
[DataDefinition, Serializable]
public readonly partial record struct Attack(
    [property: DataField]
    DamageSpecifier Damages,
    [property: DataField]
    bool IgnoreResistances = false,
    [property: DataField]
    bool InterruptsDoAfters = true,
    [property: DataField]
    EntityUid? Origin = null,
    [property: DataField]
    bool IgnoreGlobalModifiers = false)
{
    public Attack() : this(new())
    {
    }
}
