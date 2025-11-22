using Content.Shared.DoAfter;
using Content.Shared.EntityEffects;
using Robust.Shared.Serialization;

namespace Content.Shared.Crafting;

[ImplicitDataDefinitionForInheritors]
[Access(typeof(CraftingGraphSystem))]
public abstract partial class CraftingGraphStep
{
    [DataField]
    public EntityEffect[] CompletedEffects = Array.Empty<EntityEffect>();

    public abstract bool RaiseCouldHandleEvent<TEvent>(EntityUid target, ICraftingGraphEventRaiser raiser, ref TEvent args, EntityUid? user, Entity<CraftingGraphComponent> graph);
    public abstract CraftingGraphStepResult RaiseHandleEvent<TEvent>(EntityUid target, ICraftingGraphEventRaiser raiser, ref TEvent args, EntityUid? user, CraftingGraphLocation location, Entity<CraftingGraphComponent> graph);
    public abstract void RaiseDoAfterEvent(EntityUid target, ICraftingGraphEventRaiser raiser, ref CraftingDoAfterEvent args, EntityUid? user, Entity<CraftingGraphComponent> graph);
    public abstract void RaiseAttemptDoAfterEvent(EntityUid target, ICraftingGraphEventRaiser raiser, ref DoAfterAttemptEvent<CraftingDoAfterEvent> args, EntityUid? user, Entity<CraftingGraphComponent> graph);
}

public abstract partial class CraftingGraphStepBase<TStep> : CraftingGraphStep where TStep : CraftingGraphStepBase<TStep>
{
    public override bool RaiseCouldHandleEvent<TEvent>(EntityUid target, ICraftingGraphEventRaiser raiser, ref TEvent args, EntityUid? user, Entity<CraftingGraphComponent> graph)
    {
        if (this is not TStep step)
            return false;

        return raiser.RaiseCouldHandleEvent(target, step, ref args, user, graph);
    }

    public override CraftingGraphStepResult RaiseHandleEvent<TEvent>(EntityUid target, ICraftingGraphEventRaiser raiser, ref TEvent args, EntityUid? user, CraftingGraphLocation location, Entity<CraftingGraphComponent> graph)
    {
        if (this is not TStep step)
            return default;

        return raiser.RaiseHandleEvent(target, step, ref args, user, location, graph);
    }

    public override void RaiseDoAfterEvent(EntityUid target, ICraftingGraphEventRaiser raiser, ref CraftingDoAfterEvent args, EntityUid? user, Entity<CraftingGraphComponent> graph)
    {
        if (this is not TStep step)
            return;

        raiser.RaiseDoAfterEvent(target, step, ref args, user, graph);
    }

    public override void RaiseAttemptDoAfterEvent(EntityUid target, ICraftingGraphEventRaiser raiser, ref DoAfterAttemptEvent<CraftingDoAfterEvent> args, EntityUid? user, Entity<CraftingGraphComponent> graph)
    {
        if (this is not TStep step)
            return;

        raiser.RaiseAttemptDoAfterEvent(target, step, ref args, user, graph);
    }
}

[ByRefEvent]
public record struct CraftingGraphCouldStepEvent<TStep, TEvent>(TStep Step, TEvent Event, EntityUid? User, Entity<CraftingGraphComponent> Graph) where TStep : CraftingGraphStepBase<TStep>
{
    public bool Result;
}

public struct CraftingGraphStepResult
{
    public bool Progress;

    public bool Handled;
}

[Serializable, NetSerializable]
public record struct CraftingGraphLocation(string Node, int Edge, int Step);

[ByRefEvent]
public record struct CraftingGraphStepEvent<TStep, TEvent>(TStep Step, TEvent Event, CraftingGraphLocation Location, EntityUid? User, Entity<CraftingGraphComponent> Graph) where TStep : CraftingGraphStepBase<TStep>
{
    public CraftingGraphStepResult Result;
}

[ByRefEvent]
public record struct CraftingGraphStepDoAfterEvent<TStep>(TStep Step, CraftingDoAfterEvent Event, EntityUid? User, Entity<CraftingGraphComponent> Graph) where TStep : CraftingGraphStepBase<TStep>;

[ByRefEvent]
public record struct CraftingGraphStepAttemptDoAfterEvent<TStep>(TStep Step, DoAfterAttemptEvent<CraftingDoAfterEvent> Event, EntityUid? User, Entity<CraftingGraphComponent> Graph) where TStep : CraftingGraphStepBase<TStep>;

public abstract partial class CraftingGraphStepSystem<TComp, TStep, TEvent> : EntitySystem where TComp : Component where TStep : CraftingGraphStepBase<TStep>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TComp, CraftingGraphCouldStepEvent<TStep, TEvent>>(CouldHandle);
        SubscribeLocalEvent<TComp, CraftingGraphStepEvent<TStep, TEvent>>(Handle);
    }

    protected virtual void CouldHandle(Entity<TComp> ent, ref CraftingGraphCouldStepEvent<TStep, TEvent> args)
    {
        args.Result = true;
    }

    protected abstract void Handle(Entity<TComp> ent, ref CraftingGraphStepEvent<TStep, TEvent> args);
} 

public abstract partial class CraftingGraphStepDoAfterSystem<TComp, TStep, TEvent> : CraftingGraphStepSystem<TComp, TStep, TEvent> where TComp : Component where TStep : CraftingGraphStepBase<TStep>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TComp, CraftingGraphStepDoAfterEvent<TStep>>(HandleDoAfter);
        SubscribeLocalEvent<TComp, CraftingGraphStepAttemptDoAfterEvent<TStep>>(HandleAttemptDoAfter);
    }

    protected virtual void HandleAttemptDoAfter(Entity<TComp> ent, ref CraftingGraphStepAttemptDoAfterEvent<TStep> args)
    {
    }

    protected virtual void HandleDoAfter(Entity<TComp> ent, ref CraftingGraphStepDoAfterEvent<TStep> args)
    {
    }
}

[Serializable, NetSerializable]
public sealed partial class CraftingDoAfterEvent : DoAfterEvent
{
    public CraftingGraphLocation Location;

    public CraftingDoAfterEvent(CraftingGraphLocation location)
    {
        Location = location;
    }

    public override DoAfterEvent Clone() => this;
}
