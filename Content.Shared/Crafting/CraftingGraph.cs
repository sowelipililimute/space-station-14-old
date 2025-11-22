using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared.Crafting;

[ImplicitDataDefinitionForInheritors]
public abstract partial class CraftingGraphSelector
{
    public virtual object? CacheKey => null;

    public abstract CraftingGraph GetGraph(IPrototypeManager prototype, IComponentFactory componentFactory);
}

public sealed partial class CraftingGraph : CraftingGraphSelector
{
    [DataField(required: true)]
    public Dictionary<string, CraftingGraphNodeSelector> Nodes = new();

    public override CraftingGraph GetGraph(IPrototypeManager prototype, IComponentFactory componentFactory) => this;
}

public sealed partial class CraftingGraphReference : CraftingGraphSelector, IReferenceSelector<CraftingGraphReference, ProtoId<CraftingGraphPrototype>>
{
    [DataField(required: true)]
    public ProtoId<CraftingGraphPrototype> Graph;

    public override CraftingGraph GetGraph(IPrototypeManager prototype, IComponentFactory componentFactory) =>
        prototype.Index(Graph).Graph.GetGraph(prototype, componentFactory);

    public static CraftingGraphReference Make(ProtoId<CraftingGraphPrototype> id) =>
        new() { Graph = id };
}

[Prototype]
public sealed partial class CraftingGraphPrototype : IPrototype, IInheritingPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<CraftingGraphPrototype>))]
    public string[]? Parents { get; private set; }

    [AbstractDataField]
    [NeverPushInheritance]
    public bool Abstract { get; private set; }

    [DataField(required: true)]
    public CraftingGraphSelector Graph = default!;
}

public sealed partial class AggregateCraftingGraphReference : CraftingGraphSelector
{
    public override object CacheKey => Aggregate;

    [DataField(required: true)]
    public ProtoId<AggregateCraftingGraphPrototype> Aggregate;

    private IEnumerable<CraftingGraphSelector> GetSelectors(IPrototypeManager prototype, IComponentFactory componentFactory)
    {
        foreach (var fragment in prototype.EnumeratePrototypes<CraftingGraphFragmentPrototype>())
        {
            if (fragment.AggregateInto != Aggregate)
                continue;

            yield return fragment.Graph;
        }

        foreach (var entity in prototype.EnumeratePrototypes<EntityPrototype>())
        {
            if (entity.Abstract)
                continue;

            if (!entity.TryGetComponent<AggregateCraftingGraphComponent>(out var aggregateComponent, componentFactory))
                continue;

            if (aggregateComponent.AggregateInto != Aggregate)
                continue;

            if (aggregateComponent.Graph is not { } graphSelector)
                continue;

            yield return graphSelector;
        }
    }

    public override CraftingGraph GetGraph(IPrototypeManager prototype, IComponentFactory componentFactory)
    {
        var graph = new CraftingGraph() { Nodes = new() };

        foreach (var fragment in GetSelectors(prototype, componentFactory))
        {
            foreach (var (id, node) in fragment.GetGraph(prototype, componentFactory).Nodes)
            {
                graph.Nodes[id] = node;
            }
        }

        return graph;
    }
}

[Prototype]
public sealed partial class CraftingGraphFragmentPrototype : IPrototype, IInheritingPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<CraftingGraphFragmentPrototype>))]
    public string[]? Parents { get; private set; }

    [AbstractDataField]
    [NeverPushInheritance]
    public bool Abstract { get; private set; }

    [DataField(required: true)]
    public ProtoId<AggregateCraftingGraphPrototype> AggregateInto;

    [DataField(required: true)]
    public CraftingGraphSelector Graph = default!;
}

[Prototype]
public sealed partial class AggregateCraftingGraphPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;
}
