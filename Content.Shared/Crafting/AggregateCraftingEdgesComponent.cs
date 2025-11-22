using Robust.Shared.Prototypes;

namespace Content.Shared.Crafting;

[RegisterComponent]
public sealed partial class AggregateCraftingEdgesComponent : Component
{
    [DataField(required: true, readOnly: true)]
    public ProtoId<AggregateCraftingGraphEdgesPrototype> AggregateInto;

    [DataField(readOnly: true)]
    [NeverPushInheritance]
    public CraftingGraphEdgesSelector? Edges;
}
