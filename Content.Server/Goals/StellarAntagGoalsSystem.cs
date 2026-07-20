using Content.Server.Antag;
using Content.Shared.Mind;
using Content.Shared.Goals;

namespace Content.Server.Goals;

public sealed partial class StellarAntagGoalsSystem : EntitySystem
{
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private StellarGoalsSystem _goals = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StellarAntagGoalsComponent, AfterAntagEntitySelectedEvent>(OnAntagSelected);
    }

    private void OnAntagSelected(Entity<StellarAntagGoalsComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        if (!_mind.TryGetMind(args.Session, out var mindId, out _))
        {
            Log.Error($"Antag {ToPrettyString(args.EntityUid):player} was selected by {ToPrettyString(ent):rule} but had no mind attached!");
            return;
        }

        var container = _goals.GetIndividualGoalContainer(mindId);
        _goals.TryAddGoals(container, ent.Comp.Goals);
    }
}
