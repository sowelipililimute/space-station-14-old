using Content.Shared.Goals;

namespace Content.Client.Goals;

public sealed class StellarClientGoalsSystem : EntitySystem
{
    public event Action<Entity<StellarGoalComponent>>? OnGoalChanged;
    public event Action<Entity<StellarGoalContainerComponent>>? OnContainerChanged;
    public event Action<Entity<StellarGoalContainerObserverComponent>>? OnObserverChanged;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StellarGoalComponent, AfterAutoHandleStateEvent>(OnGoalState);
        SubscribeLocalEvent<StellarGoalContainerComponent, AfterAutoHandleStateEvent>(OnContainerState);
        SubscribeLocalEvent<StellarGoalContainerObserverComponent, AfterAutoHandleStateEvent>(OnObserverState);
    }

    private void OnGoalState(Entity<StellarGoalComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        OnGoalChanged?.Invoke(ent);
    }

    private void OnContainerState(Entity<StellarGoalContainerComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        OnContainerChanged?.Invoke(ent);
    }

    private void OnObserverState(Entity<StellarGoalContainerObserverComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        OnObserverChanged?.Invoke(ent);
    }
}
