using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.Damage.Systems;

public sealed partial class DamageableSystem
{
    [Dependency] private readonly InjurableSystem _injurable = default!;

    /// <summary>
    /// Gets the damages currently sustained by an entity.
    /// </summary>
    [Obsolete("Prefer to call the InjurableSystem method instead; this alias method is only to ease porting")]
    public DamageSpecifier GetAllDamage(Entity<InjurableComponent?> ent)
    {
        return _injurable.GetAllInjuries(ent);
    }

    [Obsolete("Prefer to call the InjurableSystem method instead; this alias method is only to ease porting")]
    public void SetDamage(Entity<InjurableComponent?> ent, DamageSpecifier damage)
    {
        _injurable.SetInjuries(ent, damage);
    }

    [Obsolete("Prefer to call the InjurableSystem method instead; this alias method is only to ease porting")]
    public void SetAllDamage(Entity<InjurableComponent?> ent, FixedPoint2 damage)
    {
        _injurable.SetAllInjuries(ent, damage);
    }

    [Obsolete("Prefer to call the InjurableSystem method instead; this alias method is only to ease porting")]
    public void ClearAllDamage(Entity<InjurableComponent?> ent)
    {
        _injurable.ClearAllInjuries(ent);
    }

    /// <summary>
    /// Gets the total amount of damage currently sustained by an entity.
    /// </summary>
    [Obsolete("Prefer to call the InjurableSystem method instead; this alias method is only to ease porting")]
    public FixedPoint2 GetTotalDamage(Entity<InjurableComponent?> ent)
    {
        return _injurable.GetTotalInjuries(ent);
    }

    /// <summary>
    /// Gets the total amount of damage currently sustained by an entity, indexed by damage group.
    /// </summary>
    [Obsolete("Prefer to call the InjurableSystem method instead; this alias method is only to ease porting")]
    public Dictionary<ProtoId<DamageGroupPrototype>, FixedPoint2> GetDamagePerGroup(Entity<InjurableComponent?> ent)
    {
        return _injurable.GetInjuriesPerGroup(ent);
    }

    /// <summary>
    /// Returns whether the entity can be damaged by the given type of damage
    /// </summary>
    [Obsolete("Do not rely on the ability to determine if an entity will be able to be damaged by something")]
    public bool CanBeDamagedBy(Entity<InjurableComponent?> ent, ProtoId<DamageTypePrototype> type)
    {
        return _injurable.CanBeDamagedBy(ent, type);
    }
}
