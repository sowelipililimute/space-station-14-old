using System.Linq;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared.Crafting;

[ImplicitDataDefinitionForInheritors]
public abstract partial class CraftingGraphEdgesSelector
{
    public virtual object? CacheKey => null;

    public abstract List<CraftingGraphEdgeSelector> GetEdges(IPrototypeManager prototype, IComponentFactory componentFactory);
}

public sealed partial class CraftingGraphEdges : CraftingGraphEdgesSelector
{
    [DataField(required: true)]
    public List<CraftingGraphEdgeSelector> Edges = new();

    public override List<CraftingGraphEdgeSelector> GetEdges(IPrototypeManager prototype, IComponentFactory componentFactory) => Edges;
}

public sealed partial class AggregateCraftingGraphEdges : CraftingGraphEdgesSelector
{
    public override object CacheKey => Aggregate;

    [DataField(required: true)]
    public ProtoId<AggregateCraftingGraphEdgesPrototype> Aggregate;

    private IEnumerable<CraftingGraphEdgesSelector> GetSelectors(IPrototypeManager prototype, IComponentFactory componentFactory)
    {
        foreach (var fragment in prototype.EnumeratePrototypes<CraftingGraphEdgesFragmentPrototype>())
        {
            if (fragment.AggregateInto != Aggregate)
                continue;

            yield return fragment.Edges;
        }

        foreach (var entity in prototype.EnumeratePrototypes<EntityPrototype>())
        {
            if (entity.Abstract)
                continue;

            if (!entity.TryGetComponent<AggregateCraftingEdgesComponent>(out var aggregateComponent, componentFactory))
                continue;

            if (aggregateComponent.AggregateInto != Aggregate)
                continue;

            if (aggregateComponent.Edges is not { } edgesSelector)
                continue;

            yield return edgesSelector;
        }
    }

    public override List<CraftingGraphEdgeSelector> GetEdges(IPrototypeManager prototype, IComponentFactory componentFactory)
    {
        var edges = new List<CraftingGraphEdgeSelector>();

        foreach (var selector in GetSelectors(prototype, componentFactory))
        {
            edges.AddRange(selector.GetEdges(prototype, componentFactory));
        }

        return edges;
    }
}

[Prototype]
public sealed partial class CraftingGraphEdgesFragmentPrototype : IPrototype, IInheritingPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<CraftingGraphEdgesFragmentPrototype>))]
    public string[]? Parents { get; private set; }

    [AbstractDataField]
    [NeverPushInheritance]
    public bool Abstract { get; private set; }

    [DataField(required: true)]
    public ProtoId<AggregateCraftingGraphEdgesPrototype> AggregateInto;

    [DataField(required: true)]
    public CraftingGraphEdgesSelector Edges = default!;
}

[Prototype]
public sealed partial class AggregateCraftingGraphEdgesPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;
}

