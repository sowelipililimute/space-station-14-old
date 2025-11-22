using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared.Crafting;

[ImplicitDataDefinitionForInheritors]
public abstract partial class CraftingGraphNodeSelector
{
    public abstract CraftingGraphNode GetNode(IPrototypeManager prototype);
}

public sealed partial class CraftingGraphNode : CraftingGraphNodeSelector
{
    [DataField]
    public CraftingGraphEdgesSelector Edges = new CraftingGraphEdges() { Edges = new() };

    [DataField]
    public CraftingGraphResult? Result;

    public override CraftingGraphNode GetNode(IPrototypeManager prototype) => this;
}

public sealed partial class CraftingGraphNodeReference : CraftingGraphNodeSelector, IReferenceSelector<CraftingGraphNodeReference, ProtoId<CraftingGraphNodePrototype>>
{
    [DataField(required: true)]
    public ProtoId<CraftingGraphNodePrototype> Node;

    public override CraftingGraphNode GetNode(IPrototypeManager prototype) =>
        prototype.Index(Node).Node.GetNode(prototype);

    public static CraftingGraphNodeReference Make(ProtoId<CraftingGraphNodePrototype> id) =>
        new() { Node = id };
}

[Prototype]
public sealed partial class CraftingGraphNodePrototype : IPrototype, IInheritingPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<CraftingGraphNodePrototype>))]
    public string[]? Parents { get; private set; }

    [AbstractDataField]
    [NeverPushInheritance]
    public bool Abstract { get; private set; }

    [DataField(required: true)]
    public CraftingGraphNodeSelector Node = default!;
}
