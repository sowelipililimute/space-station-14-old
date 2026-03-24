using System.Linq;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using Content.Shared.Rejuvenate;
using Robust.Shared.Prototypes;

namespace Content.Shared.Damage.Systems;

public sealed partial class InjurableSystem : EntitySystem
{
    private EntityQuery<InjurableComponent> _injurableQuery;
    private EntityQuery<AppearanceComponent> _appearanceQuery;

    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    private readonly Dictionary<ProtoId<InjuryContainerPrototype>, Dictionary<ProtoId<DamageTypePrototype>, ProtoId<DamageTypePrototype>>> _supportedTypesByContainer = new();
    private readonly Dictionary<ProtoId<DamageTypePrototype>, HashSet<ProtoId<DamageGroupPrototype>>> _damageGroupsByTypes = new();

    public override void Initialize()
    {
        base.Initialize();

        _injurableQuery = GetEntityQuery<InjurableComponent>();
        _appearanceQuery = GetEntityQuery<AppearanceComponent>();

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        SubscribeLocalEvent<InjurableComponent, DamageDealtEvent>(OnDamageDealt);
        SubscribeLocalEvent<InjurableComponent, RejuvenateEvent>(OnRejuvenate);

        RebuildPrototypeCache();
    }

    private void OnDamageDealt(Entity<InjurableComponent> ent, ref DamageDealtEvent args)
    {
        DamageSpecifier? oldInjuries = null;

        foreach (var (damageType, value) in args.Damage.DamageDict)
        {
            if (!_supportedTypesByContainer[ent.Comp.InjuryContainer].TryGetValue(damageType, out var injuryType))
                continue;

            var oldValue = ent.Comp.Injuries.DamageDict.GetValueOrDefault(injuryType);
            var newValue = FixedPoint2.Max(FixedPoint2.Zero, oldValue + value);
            if (newValue == oldValue)
                continue;

            oldInjuries ??= ent.Comp.Injuries.Clone();
            ent.Comp.Injuries.DamageDict[injuryType] = newValue;
        }

        if (oldInjuries is null)
            return;

        OnEntityInjuriesChanged(ent, oldInjuries, args.Origin);
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs ev)
    {
        if (!ev.WasModified<InjuryContainerPrototype>() && !ev.WasModified<DamageGroupPrototype>() && !ev.WasModified<DamageTypePrototype>())
            return;

        RebuildPrototypeCache();
    }

    private void RebuildPrototypeCache()
    {
        _supportedTypesByContainer.Clear();

        foreach (var proto in _prototype.EnumeratePrototypes<InjuryContainerPrototype>())
        {
            _supportedTypesByContainer[proto.ID] = proto.SupportedTypes;
        }

        _damageGroupsByTypes.Clear();

        foreach (var proto in _prototype.EnumeratePrototypes<DamageGroupPrototype>())
        {
            foreach (var type in proto.DamageTypes)
            {
                if (!_damageGroupsByTypes.TryGetValue(type, out var set))
                {
                    set = [];
                    _damageGroupsByTypes[type] = set;
                }

                set.Add(proto);
            }
        }
    }

    /// <summary>
    ///     Should be called whenever an entity's injuries are changed.
    /// </summary>
    /// <remarks>
    ///     This updates cached damage information, flags the component as dirty, and raises relevant events.
    /// </remarks>
    private void OnEntityInjuriesChanged(
        Entity<InjurableComponent> ent,
        DamageSpecifier? old,
        EntityUid? origin
    )
    {
        ent.Comp.InjuriesPerGroup.Clear();
        foreach (var (type, amount) in ent.Comp.Injuries.DamageDict)
        {
            if (!_damageGroupsByTypes.TryGetValue(type, out var groups))
                continue;

            foreach (var group in groups)
            {
                ent.Comp.InjuriesPerGroup[group] = ent.Comp.InjuriesPerGroup.GetValueOrDefault(group) + amount;
            }
        }
        ent.Comp.TotalInjuries = ent.Comp.Injuries.GetTotal();

        if (old != null && _appearanceQuery.TryGetComponent(ent, out var appearance))
        {
            _appearance.SetData(
                ent,
                DamageVisualizerKeys.DamageUpdateGroups,
                new DamageVisualizerGroupData(ent.Comp.InjuriesPerGroup.Keys.ToList()),
                appearance
            );
        }

        var evt = new InjuriesChangedEvent(ent, old, ent.Comp.Injuries, origin);
        RaiseLocalEvent(ent, ref evt);
    }

    private void OnRejuvenate(Entity<InjurableComponent> ent, ref RejuvenateEvent args)
    {
        // Do this so that the state changes when we set the damage
        _mobThreshold.SetAllowRevives(ent, true);
        ClearAllInjuries(ent.AsNullable());
        _mobThreshold.SetAllowRevives(ent, false);
    }
}

/// <summary>
/// Raised on an entity when its injuries change.
/// </summary>
/// <param name="Old">The old injuries, if available.</param>
/// <param name="New">The new injuries.</param>
/// <param name="Origin">The originating entity that caused this change.</param>
[ByRefEvent]
public readonly record struct InjuriesChangedEvent(Entity<InjurableComponent> Injurable, DamageSpecifier? Old, DamageSpecifier New, EntityUid? Origin);
