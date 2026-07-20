using Content.Shared.Mind;
using Content.Shared.Mobs;

namespace Content.Shared.Goals;

public sealed partial class StellarTargetSurvivesGoalSystem : StellarBaseTargetedGoalSystem<StellarTargetSurvivesGoalComponent, StellarTargetedSurvivesComponent>
{
    [Dependency] private SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StellarTargetSurvivesGoalComponent, StellarGetGoalProgressEvent>(OnGetProgress);
        RefreshOnEvent<MindRelayedEvent<MobStateChangedEvent>>();
    }

    private void OnGetProgress(Entity<StellarTargetSurvivesGoalComponent> ent, ref StellarGetGoalProgressEvent args)
    {
        if (GetTarget(ent.Owner) is not { } target || !TryComp<MindComponent>(target, out var mindComp))
            return;

        args.Progress = _mind.IsCharacterDeadIc(mindComp) ? 0f : 1f;
    }
}
