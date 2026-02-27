using Content.Shared.Damage.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Damage;

/// <summary>
/// Defines an "attack" dealt onto a <see cref="DamageableComponent" /> entity
/// </summary>
/// <param name="Damages">The amount of damage this attack deals</param>
/// <param name="IgnoreResistances">If this attack should ignore resistances</param>
/// <param name="InterruptsDoAfters">If this attack should interrupt do-afters</param>
/// <param name="IgnoreGlobalModifiers">If this attack should ignore global modifiers</param>
[DataDefinition, Serializable, NetSerializable]
public readonly partial record struct AttackSpecifier(
    [property: DataField]
    DamageSpecifier Damages,
    [property: DataField]
    bool IgnoreResistances = false,
    [property: DataField]
    bool InterruptsDoAfters = true,
    [property: DataField]
    bool IgnoreGlobalModifiers = false)
{
    public AttackSpecifier() : this(new DamageSpecifier())
    {
    }

    [Obsolete("Allow end-users to specify an AttackSpecifier instead of a DamageSpecifier")]
    public static implicit operator AttackSpecifier(DamageSpecifier damage)
    {
        return new AttackSpecifier(damage);
    }
}

/// <summary>
/// Associates an "attack" as specified by a <see cref="AttackSpecifier" /> with data that is unique per instance, e.g.
/// "who is performing this attack?"
/// </summary>
/// <param name="Specifier">The attack specifier of this attack</param>
/// <param name="Origin">The originating entity of this attack</param>
public readonly record struct Attack(AttackSpecifier Specifier, EntityUid? Origin = null)
{
    [Obsolete("Allow end-users to specify an AttackSpecifier instead of a DamageSpecifier")]
    public static implicit operator Attack(DamageSpecifier damage)
    {
        return new Attack(new(damage));
    }

    public static implicit operator Attack(AttackSpecifier specifier)
    {
        return new Attack(specifier);
    }
}
