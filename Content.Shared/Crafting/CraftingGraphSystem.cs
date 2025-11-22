using Content.Shared.DoAfter;
using Content.Shared.EntityConditions;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared.Crafting;

public sealed partial class CraftingGraphSystem : EntitySystem, ICraftingGraphEventRaiser
{
    [Dependency] private readonly BecomeSystem _become = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedEntityConditionsSystem _entityConditions = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _entityEffects = default!;

    public override void Initialize()
    {
        base.Initialize();

        InitializeEvents();

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
    }

    private readonly Dictionary<object, CraftingGraph> _graphs = new();
    private readonly Dictionary<object, List<CraftingGraphEdgeSelector>> _edges = new();

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs? ev)
    {
        _graphs.Clear();
        _edges.Clear();
    }

    private CraftingGraph Get(CraftingGraphSelector selector)
    {
        if (selector.CacheKey is not null)
        {
            if (_graphs.TryGetValue(selector.CacheKey, out var graph))
                return graph;

            return _graphs[selector.CacheKey] = selector.GetGraph(_prototype, EntityManager.ComponentFactory);
        }

        return selector.GetGraph(_prototype, EntityManager.ComponentFactory);
    }

    private List<CraftingGraphEdgeSelector> Get(CraftingGraphEdgesSelector selector)
    {
        if (selector.CacheKey is not null)
        {
            if (_edges.TryGetValue(selector.CacheKey, out var edges))
                return edges;

            return _edges[selector.CacheKey] = selector.GetEdges(_prototype, EntityManager.ComponentFactory);
        }

        return selector.GetEdges(_prototype, EntityManager.ComponentFactory);
    }

    private CraftingGraphEdge Get(CraftingGraphEdgeSelector selector) =>
        selector.GetEdge(_prototype);

    private CraftingGraphNode Get(CraftingGraphNodeSelector selector) =>
        selector.GetNode(_prototype);

    public bool TryProgress(EntityUid target, Entity<CraftingGraphComponent> graphEnt, string nodeID, int edgeIndex)
    {
        if (graphEnt.Comp.CurrentNode != nodeID)
            return false;

        var graph = Get(graphEnt.Comp.Graph);

        if (!graph.Nodes.TryGetValue(nodeID, out var nodeSelector))
            return false;

        var node = Get(nodeSelector);
        var edges = Get(node.Edges);

        if (edgeIndex >= edges.Count)
            return false;

        var edge = Get(edges[edgeIndex]);

        var stepToComplete = 0;
        if (graphEnt.Comp.CurrentEdgeIndex is { } currentEdgeAndStep)
        {
            var (currentEdge, currentStep) = currentEdgeAndStep;

            if (currentEdge != edgeIndex)
                return false;

            stepToComplete = currentStep;
        }

        var newStepIndex = stepToComplete + 1;

        if (stepToComplete < edge.Steps.Count)
        {
            _entityEffects.ApplyEffects(target, edge.Steps[stepToComplete].CompletedEffects);
        }

        if (newStepIndex < edge.Steps.Count)
        {
            graphEnt.Comp.CurrentEdgeIndex = (edgeIndex, newStepIndex);
            Dirty(graphEnt);

            return true;
        }

        _entityEffects.ApplyEffects(target, edge.CompletedEffects);
        ChangeNode(target, graphEnt, edge.Target);

        return true;
    }

    public void ChangeNode(EntityUid target, Entity<CraftingGraphComponent> graphEnt, string nodeID, bool effects = true)
    {
        var graph = Get(graphEnt.Comp.Graph);

        if (!graph.Nodes.TryGetValue(nodeID, out var nodeSelector))
            return;

        graphEnt.Comp.CurrentNode = nodeID;
        graphEnt.Comp.CurrentEdgeIndex = null;
        Dirty(graphEnt);

        if (!effects)
            return;

        var node = Get(nodeSelector);

        if (node.Result is { } entity)
            ChangeEntity(target, graphEnt, entity);
    }

    private void ChangeEntity(EntityUid target, Entity<CraftingGraphComponent> graphEnt, CraftingGraphResult selector)
    {
        if (selector.RaiseEvent(target, this) is not { } entityPrototype)
            return;

        _become.Become(graphEnt, entityPrototype, graphEnt.Comp.BecomeSettings, graphEnt.Comp.CloningSettings);
    }

    public bool RaiseCouldHandleEvent<TStep, TEvent>(EntityUid target, TStep step, ref TEvent args, EntityUid? user, Entity<CraftingGraphComponent> graph) where TStep : CraftingGraphStepBase<TStep>
    {
        var evt = new CraftingGraphCouldStepEvent<TStep, TEvent>(step, args, user, graph);
        RaiseLocalEvent(target, ref evt);

        return evt.Result;
    }

    public CraftingGraphStepResult RaiseHandleEvent<TStep, TEvent>(EntityUid target, TStep step, ref TEvent args, EntityUid? user, CraftingGraphLocation location, Entity<CraftingGraphComponent> graph) where TStep : CraftingGraphStepBase<TStep>
    {
        var evt = new CraftingGraphStepEvent<TStep, TEvent>(step, args, location, user, graph);
        RaiseLocalEvent(target, ref evt);

        args = evt.Event;

        return evt.Result;
    }

    public EntProtoId? RaiseResultEvent<TEntity>(EntityUid target, TEntity entity) where TEntity : CraftingGraphResultBase<TEntity>
    {
        var evt = new CraftingGraphResultEvent<TEntity>(entity);
        RaiseLocalEvent(target, ref evt);

        return evt.Prototype;
    }

    public void RaiseDoAfterEvent<TStep>(EntityUid target, TStep step, ref CraftingDoAfterEvent args, EntityUid? user, Entity<CraftingGraphComponent> graph) where TStep : CraftingGraphStepBase<TStep>
    {
        var evt = new CraftingGraphStepDoAfterEvent<TStep>(step, args, user, graph);
        RaiseLocalEvent(target, ref evt);
    }

    public void RaiseAttemptDoAfterEvent<TStep>(EntityUid target, TStep step, ref DoAfterAttemptEvent<CraftingDoAfterEvent> args, EntityUid? user, Entity<CraftingGraphComponent> graph) where TStep : CraftingGraphStepBase<TStep>
    {
        var evt = new CraftingGraphStepAttemptDoAfterEvent<TStep>(step, args, user, graph);
        RaiseLocalEvent(target, ref evt);
    }
}
