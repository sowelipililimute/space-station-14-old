using Content.Shared.Teleportation.Components;
using Content.Shared.Warps;
using Content.Shared.Whitelist;
using Robust.Shared.Random;

namespace Content.Shared.Goals;

public sealed partial class StellarTargetWarpPointGoalSystem : EntitySystem
{
    [Dependency] private StellarTargetedGoalSystem _targetedGoal = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private EntityWhitelistSystem _entityWhitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StellarTargetWarpPointGoalComponent, StellarGoalInsertedEvent>(OnInserted);
        SubscribeLocalEvent<StellarTargetWarpPointGoalComponent, StellarTargetNameEvent>(OnName);
    }

    private void OnInserted(Entity<StellarTargetWarpPointGoalComponent> ent, ref StellarGoalInsertedEvent args)
    {
        var warps = new List<EntityUid>();
        var query = EntityQueryEnumerator<WarpPointComponent>();

        if (!TryComp<TeleportLocationsComponent>(args.Container, out var teleportLocations))
            return;

        while (query.MoveNext(out var uid, out var comp))
        {
            if (_entityWhitelist.CheckBoth(uid, teleportLocations.Blacklist, teleportLocations.Whitelist))
                continue;

            if (comp.Location == null)
                continue;

            warps.Add(uid);
        }

        if (warps.Count <= 0)
            return;

        _targetedGoal.SetTarget(ent.Owner, _random.Pick(warps));
    }

    private void OnName(Entity<StellarTargetWarpPointGoalComponent> ent, ref StellarTargetNameEvent args)
    {
        if (!TryComp<WarpPointComponent>(args.Target, out var warpPoint) || warpPoint.Location is not { } name)
            return;

        args.Name = name;
    }
}
