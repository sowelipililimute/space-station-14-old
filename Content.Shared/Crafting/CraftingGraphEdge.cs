using Content.Shared.EntityConditions;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared.Crafting;

[ImplicitDataDefinitionForInheritors]
public abstract partial class CraftingGraphEdgeSelector
{
    public abstract CraftingGraphEdge GetEdge(IPrototypeManager prototype);
}

public sealed partial class CraftingGraphEdge : CraftingGraphEdgeSelector
{
    [DataField(required: true)]
    public string Target = default!;

    [DataField]
    public List<CraftingGraphStep> Steps = new();

    [DataField]
    public EntityEffect[] CompletedEffects = Array.Empty<EntityEffect>();

    [DataField]
    public EntityCondition[] Conditions = Array.Empty<EntityCondition>();

    public override CraftingGraphEdge GetEdge(IPrototypeManager prototype) =>
        this;
}

public sealed partial class CraftingGraphEdgeReference : CraftingGraphEdgeSelector, IReferenceSelector<CraftingGraphEdgeReference, ProtoId<CraftingGraphEdgePrototype>>
{
    [DataField(required: true)]
    public ProtoId<CraftingGraphEdgePrototype> Edge;

    public override CraftingGraphEdge GetEdge(IPrototypeManager prototype) =>
        prototype.Index(Edge).Edge.GetEdge(prototype);

    public static CraftingGraphEdgeReference Make(ProtoId<CraftingGraphEdgePrototype> id) =>
        new() { Edge = id };
}

[Prototype]
public sealed partial class CraftingGraphEdgePrototype : IPrototype, IInheritingPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<CraftingGraphEdgePrototype>))]
    public string[]? Parents { get; private set; }

    [AbstractDataField]
    [NeverPushInheritance]
    public bool Abstract { get; private set; }

    [DataField(required: true)]
    public CraftingGraphEdgeSelector Edge = default!;
}
