using JetBrains.Annotations;

namespace Content.Shared.Goals;

public sealed partial class StellarTargetedGoalSystem : EntitySystem
{
    [Dependency] private StellarGoalsSystem _goals = default!;
    [Dependency] private MetaDataSystem _metaData = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StellarTargetedGoalComponent, ComponentShutdown>(OnGoalShutdown);
    }

    private void OnGoalShutdown(Entity<StellarTargetedGoalComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.TargetType is not { } targetType)
            return;

        if (!EntityManager.TryGetComponent(ent.Comp.Target, targetType, out var targetComponent))
            return;

        DetachTarget(ent, (ent.Comp.Target.Value, targetComponent));
    }

    internal void DetachTarget(Entity<StellarTargetedGoalComponent> goal, Entity<IComponent> target)
    {
        if (target.Comp is not StellarTargetedComponent targeted)
            return;

        targeted.Observers.Remove(goal);
        if (targeted.Observers.Count <= 0)
            RemComp(target, target.Comp);
    }

    private void UpdateMetaData(Entity<StellarTargetedGoalComponent> ent)
    {
        if (ent.Comp.Target is not { } target)
            return;

        var evt = new StellarTargetNameEvent(target, Name(target));
        RaiseLocalEvent(ent, ref evt);

        if (ent.Comp.Title is { } title)
            _metaData.SetEntityName(ent, Loc.GetString(title, ("target", target), ("targetName", evt.Name)));

        if (ent.Comp.Description is { } description)
            _metaData.SetEntityDescription(ent, Loc.GetString(description, ("target", target), ("targetName", evt.Name)));
    }

    [PublicAPI]
    public void SetTarget(Entity<StellarTargetedGoalComponent?> ent, EntityUid? target)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (ent.Comp.Target == target)
            return;

        var evt = new StellarGoalTargetChangedEvent(ent.Comp.Target, target, null);
        RaiseLocalEvent(ent, ref evt);

        ent.Comp.Target = target;
        ent.Comp.TargetType = evt.NewType;
        Dirty(ent);

        _goals.RefreshProgress(ent.Owner);
        UpdateMetaData((ent, ent.Comp));
    }
}
