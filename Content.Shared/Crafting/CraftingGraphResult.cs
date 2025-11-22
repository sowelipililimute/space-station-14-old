using Robust.Shared.Prototypes;

namespace Content.Shared.Crafting;

[ImplicitDataDefinitionForInheritors]
public abstract partial class CraftingGraphResult
{
    public abstract EntProtoId? RaiseEvent(EntityUid target, ICraftingGraphEventRaiser raiser);
}

public abstract partial class CraftingGraphResultBase<TEntity> : CraftingGraphResult where TEntity : CraftingGraphResultBase<TEntity>
{
    public override EntProtoId? RaiseEvent(EntityUid target, ICraftingGraphEventRaiser raiser)
    {
        if (this is not TEntity entity)
            return null;

        return raiser.RaiseResultEvent(target, entity);
    }
}

[ByRefEvent]
public record struct CraftingGraphResultEvent<TEntity>(TEntity Entity) where TEntity : CraftingGraphResultBase<TEntity>
{
    public EntProtoId? Prototype;
}

public abstract partial class CraftingGraphResultSystem<TComp, TEntity> : EntitySystem where TComp : Component where TEntity : CraftingGraphResultBase<TEntity>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TComp, CraftingGraphResultEvent<TEntity>>(Entity);
    }

    protected abstract void Entity(Entity<TComp> ent, ref CraftingGraphResultEvent<TEntity> args);
}

public sealed partial class ConstantCraftingResult : CraftingGraphResultBase<ConstantCraftingResult>, IReferenceSelector<ConstantCraftingResult, EntProtoId>
{
    [DataField(required: true)]
    public EntProtoId Prototype;

    public override EntProtoId? RaiseEvent(EntityUid target, ICraftingGraphEventRaiser raiser) =>
        Prototype;

    public static ConstantCraftingResult Make(EntProtoId id) =>
        new() { Prototype = id };
}
