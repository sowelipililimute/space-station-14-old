using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions;

public sealed class AnchoredEntityConditionSystem : EntityConditionSystem<TransformComponent, AnchoredCondition>
{
    protected override void Condition(Entity<TransformComponent> ent, ref EntityConditionEvent<AnchoredCondition> args)
    {
        args.Result = ent.Comp.Anchored;
    }
}

public sealed partial class AnchoredCondition : EntityConditionBase<AnchoredCondition>
{
    public override string EntityConditionGuidebookText(IPrototypeManager prototype) =>
        Loc.GetString("entity-condition-anchored", ("inverted", Inverted));
}
