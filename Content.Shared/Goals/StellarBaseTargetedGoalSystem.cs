namespace Content.Shared.Goals;

public abstract partial class StellarBaseTargetedGoalSystem<TGoal, TMarker> : EntitySystem where TMarker : StellarTargetedComponent, new() where TGoal : IComponent
{
    [Dependency] private StellarTargetedGoalSystem _targetedGoal = default!;
    [Dependency] private StellarGoalsSystem _goals = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TMarker, ComponentShutdown>(OnTargetedShutdown);
        SubscribeLocalEvent<TGoal, StellarGoalTargetChangedEvent>(OnTargetChanged);
    }

    protected void RefreshOnEvent<TEvent>() where TEvent : notnull
    {
        SubscribeLocalEvent<TMarker, TEvent>(Refresh);
    }

    private void Refresh<TEvent>(Entity<TMarker> ent, ref TEvent args)
    {
        foreach (var observer in ent.Comp.Observers)
        {
            _goals.RefreshProgress(observer);
        }
    }

    private void OnTargetChanged(Entity<TGoal> ent, ref StellarGoalTargetChangedEvent args)
    {
        if (!TryComp<StellarTargetedGoalComponent>(ent, out var comp))
            return;

        if (TryComp<TMarker>(args.Old, out var oldTargeted))
        {
            _targetedGoal.DetachTarget((ent, comp), (args.Old.Value, oldTargeted));
        }

        if (args.New is { } newTarget)
        {
            var marker = EnsureComp<TMarker>(newTarget);
            marker.Observers.Add(ent);
        }
    }

    private void OnTargetedShutdown(Entity<TMarker> ent, ref ComponentShutdown args)
    {
        foreach (var goal in ent.Comp.Observers)
        {
            if (!TryComp<StellarTargetedGoalComponent>(goal, out var targetedGoal))
                continue;

            _targetedGoal.SetTarget((goal, targetedGoal), null);
        }
    }

    protected Entity<TMarker>? GetTarget(Entity<StellarTargetedGoalComponent?> goal)
    {
        if (!Resolve(goal, ref goal.Comp) || goal.Comp.Target is not { } target)
            return null;

        if (!TryComp<TMarker>(target, out var targetComp))
            return null;

        return (target, targetComp);
    }
}
