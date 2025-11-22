using Robust.Shared.Prototypes;

namespace Content.Shared.Crafting;

[RegisterComponent]
public sealed partial class AggregateCraftingGraphComponent : Component
{
    [DataField(required: true, readOnly: true)]
    public ProtoId<AggregateCraftingGraphPrototype> AggregateInto;

    [DataField(readOnly: true)]
    [NeverPushInheritance]
    public CraftingGraphSelector? Graph;
}
