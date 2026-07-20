namespace Content.Shared.Goals;

public sealed partial class StellarTargetSelfMindGoalSystem : EntitySystem
{
    [Dependency] private StellarTargetedGoalSystem _targetedGoal = default!;
    [Dependency] private StellarGoalsSystem _goals = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StellarTargetSelfMindGoalComponent, StellarGoalInsertedEvent>(OnInserted);
    }

    private void OnInserted(Entity<StellarTargetSelfMindGoalComponent> ent, ref StellarGoalInsertedEvent args)
    {
        if (!_goals.TryGetIndividualGoalOwner(args.Container.AsNullable(), out var observer))
            return;

        _targetedGoal.SetTarget(ent.Owner, observer);
    }
}
