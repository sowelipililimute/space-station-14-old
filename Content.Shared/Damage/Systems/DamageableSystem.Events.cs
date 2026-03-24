using System.Linq;
using Content.Shared.CCVar;
using Content.Shared.Damage.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.Radiation.Events;

namespace Content.Shared.Damage.Systems;

public sealed partial class DamageableSystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<DamageableComponent, OnIrradiatedEvent>(OnIrradiated);

        _damageableQuery = GetEntityQuery<DamageableComponent>();

        // Damage modifier CVars are updated and stored here to be queried in other systems.
        // Note that certain modifiers requires reloading the guidebook.
        Subs.CVar(
            _config,
            CCVars.PlaytestAllDamageModifier,
            value =>
            {
                UniversalAllDamageModifier = value;
                _chemistryGuideData.ReloadAllReagentPrototypes();
                _explosion.ReloadMap();
            },
            true
        );
        Subs.CVar(
            _config,
            CCVars.PlaytestAllHealModifier,
            value =>
            {
                UniversalAllHealModifier = value;
                _chemistryGuideData.ReloadAllReagentPrototypes();
            },
            true
        );
        Subs.CVar(
            _config,
            CCVars.PlaytestProjectileDamageModifier,
            value => UniversalProjectileDamageModifier = value,
            true
        );
        Subs.CVar(
            _config,
            CCVars.PlaytestMeleeDamageModifier,
            value => UniversalMeleeDamageModifier = value,
            true
        );
        Subs.CVar(
            _config,
            CCVars.PlaytestProjectileDamageModifier,
            value => UniversalProjectileDamageModifier = value,
            true
        );
        Subs.CVar(
            _config,
            CCVars.PlaytestHitscanDamageModifier,
            value => UniversalHitscanDamageModifier = value,
            true
        );
        Subs.CVar(
            _config,
            CCVars.PlaytestReagentDamageModifier,
            value =>
            {
                UniversalReagentDamageModifier = value;
                _chemistryGuideData.ReloadAllReagentPrototypes();
            },
            true
        );
        Subs.CVar(
            _config,
            CCVars.PlaytestReagentHealModifier,
            value =>
            {
                UniversalReagentHealModifier = value;
                _chemistryGuideData.ReloadAllReagentPrototypes();
            },
            true
        );
        Subs.CVar(
            _config,
            CCVars.PlaytestExplosionDamageModifier,
            value =>
            {
                UniversalExplosionDamageModifier = value;
                _explosion.ReloadMap();
            },
            true
        );
        Subs.CVar(
            _config,
            CCVars.PlaytestThrownDamageModifier,
            value => UniversalThrownDamageModifier = value,
            true
        );
        Subs.CVar(
            _config,
            CCVars.PlaytestTopicalsHealModifier,
            value => UniversalTopicalsHealModifier = value,
            true
        );
        Subs.CVar(
            _config,
            CCVars.PlaytestMobDamageModifier,
            value => UniversalMobDamageModifier = value,
            true
        );
    }

    private void OnIrradiated(Entity<DamageableComponent> ent, ref OnIrradiatedEvent args)
    {
        var damageValue = FixedPoint2.New(args.TotalRads);

        // Radiation should really just be a damage group instead of a list of types.
        DamageSpecifier damage = new();
        foreach (var typeId in ent.Comp.RadiationDamageTypeIDs)
        {
            damage.DamageDict.Add(typeId, damageValue);
        }

        ChangeDamage(ent.Owner, damage, interruptsDoAfters: false, origin: args.Origin);
    }
}

/// <summary>
///     Raised before damage is done, so stuff can cancel it if necessary.
/// </summary>
[ByRefEvent]
public record struct BeforeDamageChangedEvent(DamageSpecifier Damage, EntityUid? Origin = null, bool Cancelled = false);

/// <summary>
///     Raised on an entity when damage is about to be dealt,
///     in case anything else needs to modify it other than the base
///     damageable component.
///
///     For example, armor.
/// </summary>
public sealed class DamageModifyEvent(DamageSpecifier damage, EntityUid? origin = null)
    : EntityEventArgs, IInventoryRelayEvent
{
    /// <inheritdoc/>
    /// <remarks>
    ///     Whenever locational damage is a thing, this should just check only that bit of armor.
    /// </remarks>
    public SlotFlags TargetSlots => ~SlotFlags.POCKET;

    /// <summary>
    ///     Contains the original damage, prior to any modifers.
    /// </summary>
    public readonly DamageSpecifier OriginalDamage = damage;

    /// <summary>
    ///     Contains the damage after modifiers have been applied.
    ///     This is the damage that will be inflicted.
    /// </summary>
    public DamageSpecifier Damage = damage;

    /// <summary>
    ///     Contains the entity which caused the damage, if any was responsible.
    /// </summary>
    public readonly EntityUid? Origin = origin;
}

/// <summary>
/// Event raised when an entity with <see cref="DamageableComponent" /> has taken some amount of damage.
/// </summary>
/// <param name="Damage">The amount of damage the entity is being subject to.</param>
/// <param name="Origin">The originator of the damage</param>
/// <param name="InterruptsDoAfters">If the damage being dealt will interrupt do-afters</param>
[ByRefEvent]
public readonly record struct DamageDealtEvent(DamageSpecifier Damage, EntityUid? Origin, bool InterruptsDoAfters)
{
    [Obsolete("You should not be dealing negative damage")]
    public readonly bool DamageIncreased = Damage.DamageDict.Values.Any(change => change > FixedPoint2.Zero);
}
