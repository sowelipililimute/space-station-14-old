using System.Linq;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.Damage.Systems;

public sealed partial class InjurableSystem
{
    public DamageSpecifier ChangeInjuries(Entity<InjurableComponent?> ent, DamageSpecifier delta, EntityUid? origin = null)
    {
        if (!_injurableQuery.Resolve(ent, ref ent.Comp))
            return new();

        DamageSpecifier? oldInjuries = null;

        foreach (var (type, value) in delta.DamageDict)
        {
            if (!_supportedTypesByContainer[ent.Comp.InjuryContainer].ContainsValue(type))
                continue;

            var oldValue = ent.Comp.Injuries.DamageDict.GetValueOrDefault(type);
            var newValue = FixedPoint2.Max(FixedPoint2.Zero, oldValue + value);
            if (newValue == oldValue)
                continue;

            oldInjuries ??= ent.Comp.Injuries.Clone();
            ent.Comp.Injuries.DamageDict[type] = newValue;
        }

        if (oldInjuries is null)
            return new();

        OnEntityInjuriesChanged((ent, ent.Comp), oldInjuries, origin);
        return ent.Comp.Injuries - oldInjuries;
    }

    public void SetInjuries(Entity<InjurableComponent?> ent, DamageSpecifier injuries)
    {
        if (!_injurableQuery.Resolve(ent, ref ent.Comp))
            return;

        DamageSpecifier? oldInjuries = null;

        foreach (var item in _supportedTypesByContainer[ent.Comp.InjuryContainer].Values)
        {
            if (!injuries.DamageDict.ContainsKey(item) && ent.Comp.Injuries.DamageDict.ContainsKey(item))
            {
                oldInjuries ??= ent.Comp.Injuries.Clone();
                ent.Comp.Injuries.DamageDict.Remove(item);
            }
            else if (injuries.DamageDict.TryGetValue(item, out var value))
            {
                oldInjuries ??= ent.Comp.Injuries.Clone();
                ent.Comp.Injuries.DamageDict[item] = value;
            }
        }

        if (oldInjuries is null)
            return;

        OnEntityInjuriesChanged((ent, ent.Comp), oldInjuries, null);
    }

    /// <summary>
    /// Goes through an entity injuries and saves them inside a dictionary if the value is higher than 0
    /// </summary>
    public Dictionary<ProtoId<DamageTypePrototype>, FixedPoint2> GetPositiveInjuries(Dictionary<ProtoId<DamageGroupPrototype>, FixedPoint2> damagePerGroup, DamageSpecifier injuries)
    {
        var damageTypes = new Dictionary<ProtoId<DamageTypePrototype>, FixedPoint2>();

        foreach (var (damageGroupId, _) in damagePerGroup)
        {
            var group = _prototype.Index(damageGroupId);
            foreach (var type in group.DamageTypes)
            {
                if (!injuries.DamageDict.TryGetValue(type, out var damageValue) || damageValue == 0)
                    continue;

                damageTypes.Add(type, damageValue);
            }
        }
        return damageTypes;
    }

    /// <summary>
    /// Returns a <see cref="Injuries"/> with all positive injuries of the entity from the group specified
    /// </summary>
    /// <param name="ent">entity with injuries</param>
    /// <param name="group">group of injuries to get values from</param>
    public DamageSpecifier GetPositiveInjuries(Entity<InjurableComponent> ent, ProtoId<DamageGroupPrototype> group)
    {
        // No damage if no group exists...
        if (!_prototype.Resolve(group, out var groupProto))
            return new();

        var damage = new DamageSpecifier();
        damage.DamageDict.EnsureCapacity(groupProto.DamageTypes.Count);

        foreach (var damageId in groupProto.DamageTypes)
        {
            if (!ent.Comp.Injuries.DamageDict.TryGetValue(damageId, out var value))
                continue;
            if (value > FixedPoint2.Zero)
                damage.DamageDict.Add(damageId, value);
        }

        return damage;
    }

    /// <summary>
    /// Returns a <see cref="Injuries"/> with all positive injuries of the entity
    /// </summary>
    /// <param name="ent">entity with injuries</param>
    public DamageSpecifier GetPositiveInjuries(Entity<InjurableComponent?> ent)
    {
        if (!_injurableQuery.Resolve(ent, ref ent.Comp))
            return new();

        var damage = new DamageSpecifier();
        damage.DamageDict.EnsureCapacity(ent.Comp.Injuries.DamageDict.Count);

        foreach (var (damageId, value) in ent.Comp.Injuries.DamageDict)
        {
            if (value > FixedPoint2.Zero)
                damage.DamageDict.Add(damageId, value);
        }

        return damage;
    }

    /// <summary>
    /// Tries to get injuries from an entity with an optional group specifier.
    /// </summary>
    /// <param name="ent">Entity we're checking the injuries on</param>
    /// <param name="amount">Amount we want the injuries to be greater than ideally</param>
    /// <param name="damage">Injuries we're returning with</param>
    /// <param name="group">An optional group, note that if it fails to index it will just use all injuries.</param>
    /// <returns>True if the total injuries are greater than the specified amount</returns>
    public bool TryGetInjuriesGreaterThan(Entity<InjurableComponent> ent,
        FixedPoint2 amount,
        out DamageSpecifier damage,
        ProtoId<DamageGroupPrototype>? group = null)
    {
        // get the damage should be healed (either all or only from one group)
        damage = group == null ? GetPositiveInjuries(ent.AsNullable()) : GetPositiveInjuries(ent, group.Value);

        // If trying to heal more than the total damage of damageEntity just heal everything
        return damage.GetTotal() > amount;
    }

    /// <summary>
    /// Will reduce the injuries on the entity exactly by <see cref="amount"/> as close as equally distributed among all injury types the entity has.
    /// If one of the injury types of the entity is too low. it will heal that completely and distribute the excess healing among the other damage types.
    /// If the <see cref="amount"/> is larger than the total injuries to the entity then it just clears all damage.
    /// </summary>
    /// <param name="ent">entity to be healed</param>
    /// <param name="amount">how much to heal. value has to be negative to heal</param>
    /// <param name="group">from which group to heal. if null, heal from all groups</param>
    public DamageSpecifier HealEvenly(
        Entity<InjurableComponent?> ent,
        FixedPoint2 amount,
        ProtoId<DamageGroupPrototype>? group,
        EntityUid? origin = null)
    {
        if (!_injurableQuery.Resolve(ent, ref ent.Comp, false) || amount >= 0)
            return new();

        // Get our total damage, or heal if we're below a certain amount.
        if (!TryGetInjuriesGreaterThan((ent, ent.Comp), -amount, out var damage, group))
            return ChangeInjuries(ent, -damage, origin);

        // make sure damageChange has the same damage types as damage
        var delta = new DamageSpecifier(damage);
        foreach (var type in damage.DamageDict.Keys)
        {
            delta.DamageDict.Add(type, FixedPoint2.Zero);
        }

        var remaining = -amount;
        var keys = damage.DamageDict.Keys.ToList();

        while (remaining > 0)
        {
            var count = keys.Count;
            // We do this to ensure that we always round up when dividing to avoid excess loops.
            // We already have logic to prevent healing more than we have.
            var maxHeal = count == 1 ? remaining : (remaining + FixedPoint2.Epsilon * (count - 1)) / count;

            // Iterate backwards since we're removing items.
            for (var i = count - 1; i >= 0; i--)
            {
                var type = keys[i];
                // This is the amount we're trying to heal, capped by maxHeal
                var heal = damage.DamageDict[type] + delta.DamageDict[type];

                // Don't go above max, if we don't go above max
                if (heal > maxHeal)
                    heal = maxHeal;
                // If we're not above max, we will heal it fully and don't need to enumerate anymore!
                else
                    keys.RemoveAt(i);

                if (heal >= remaining)
                {
                    // Don't remove more than we can remove. Prevents us from healing more than we'd expect...
                    delta.DamageDict[type] -= remaining;
                    remaining = FixedPoint2.Zero;
                    break;
                }

                remaining -= heal;
                delta.DamageDict[type] -= heal;
            }
        }

        return ChangeInjuries(ent, delta, origin);
    }

    /// <summary>
    /// Will reduce the injuries on the entity exactly by <see cref="amount"/> distributed by weight among all injury types the entity has.
    /// (the weight is how many injuries of the type there is)
    /// If the <see cref="amount"/> is larger than the total injuries to the entity then it just clears all injuries.
    /// </summary>
    /// <param name="ent">entity to be healed</param>
    /// <param name="amount">how much to heal. value has to be negative to heal</param>
    /// <param name="group">from which group to heal. if null, heal from all groups</param>
    public DamageSpecifier HealDistributed(
        Entity<InjurableComponent?> ent,
        FixedPoint2 amount,
        ProtoId<DamageGroupPrototype>? group = null,
        EntityUid? origin = null)
    {
        if (!_injurableQuery.Resolve(ent, ref ent.Comp, false) || amount >= 0)
            return new();

        // Get our total damage, or heal if we're below a certain amount.
        if (!TryGetInjuriesGreaterThan((ent, ent.Comp), -amount, out var damage, group))
            return ChangeInjuries(ent, -damage);

        // make sure damageChange has the same damage types as damageEntity
        var delta = new DamageSpecifier(new(damage));
        var total = damage.GetTotal();

        // heal weighted by the damage of that type
        foreach (var (type, value) in damage.DamageDict)
        {
            delta.DamageDict.Add(type, value / total * amount);
        }

        return ChangeInjuries(ent, delta, origin);
    }

    /// <summary>
    ///     Sets all injury types supported by a <see cref="InjurableComponent"/> to the specified value.
    /// </summary>
    /// <remarks>
    ///     Does nothing if the given injury value is negative.
    /// </remarks>
    public void SetAllInjuries(Entity<InjurableComponent?> ent, FixedPoint2 newValue)
    {
        if (!_injurableQuery.Resolve(ent, ref ent.Comp))
            return;

        if (newValue < FixedPoint2.Zero)
            return;

        var oldInjuries = ent.Comp.Injuries.Clone();
        foreach (var type in _supportedTypesByContainer[ent.Comp.InjuryContainer].Values)
        {
            ent.Comp.Injuries.DamageDict[type] = newValue;
        }

        OnEntityInjuriesChanged((ent, ent.Comp), oldInjuries, null);
    }

    /// <summary>
    /// Clears all injuries on an entity
    /// </summary>
    /// <param name="ent"></param>
    public void ClearAllInjuries(Entity<InjurableComponent?> ent)
    {
        SetAllInjuries(ent, FixedPoint2.Zero);
    }

    /// <summary>
    /// Gets the injuries sustained by an entity, broken into injury groups
    /// </summary>
    public Dictionary<ProtoId<DamageGroupPrototype>, FixedPoint2> GetInjuriesPerGroup(Entity<InjurableComponent?> ent)
    {
        if (!_injurableQuery.Resolve(ent, ref ent.Comp))
            return new();

        return ent.Comp.InjuriesPerGroup;
    }

    /// <summary>
    /// Gets the total amount of injuries sustained by an entity
    /// </summary>
    public FixedPoint2 GetTotalInjuries(Entity<InjurableComponent?> ent)
    {
        if (!_injurableQuery.Resolve(ent, ref ent.Comp))
            return FixedPoint2.Zero;

        return ent.Comp.TotalInjuries;
    }

    /// <summary>
    /// Gets all injuries sustained by an entity
    /// </summary>
    public DamageSpecifier GetAllInjuries(Entity<InjurableComponent?> ent)
    {
        if (!_injurableQuery.Resolve(ent, ref ent.Comp))
            return new();

        return ent.Comp.Injuries.Clone();
    }

    /// <summary>
    /// Returns whether the entity can be injured by the given type of damage
    /// </summary>
    public bool CanBeDamagedBy(Entity<InjurableComponent?> ent, ProtoId<DamageTypePrototype> damage)
    {
        if (!_injurableQuery.Resolve(ent, ref ent.Comp))
            return false;

        return _supportedTypesByContainer[ent.Comp.InjuryContainer].ContainsKey(damage);
    }
}
