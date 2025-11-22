using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Temperature;

namespace Content.Shared.Crafting;

public sealed partial class CraftingGraphSystem
{
    private void InitializeEvents()
    {
        SubscribeLocalEvent<CraftingGraphComponent, OnTemperatureChangeEvent>(OnEvent);
        SubscribeLocalEvent<CraftingGraphComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<CraftingGraphComponent, CraftingDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<CraftingGraphComponent, DoAfterAttemptEvent<CraftingDoAfterEvent>>(OnDoAfterAttempt);
    }

    private void OnEvent<TEvent>(Entity<CraftingGraphComponent> ent, ref TEvent args)
    {
        HandleEvent(ent, ent, ref args, null);
    }

    private void OnInteractUsing(Entity<CraftingGraphComponent> ent, ref InteractUsingEvent args)
    {
        HandleEvent(ent, ent, ref args, args.User);
    }

    public void HandleEvent<TEvent>(EntityUid target, Entity<CraftingGraphComponent> graphEnt, ref TEvent args, EntityUid? user)
    {
        var graph = Get(graphEnt.Comp.Graph);
        var node = Get(graph.Nodes[graphEnt.Comp.CurrentNode]);
        var edges = Get(node.Edges);

        if (graphEnt.Comp.CurrentEdgeIndex is { } currentEdgeAndStep)
        {
            var (currentEdge, currentStep) = currentEdgeAndStep;

            var step = Get(edges[currentEdge]).Steps[currentStep];
            if (!step.RaiseCouldHandleEvent(target, this, ref args, user, graphEnt))
                return;

            var location = new CraftingGraphLocation(graphEnt.Comp.CurrentNode, currentEdge, currentStep);
            var result = step.RaiseHandleEvent(target, this, ref args, user, location, graphEnt);
            if (result.Progress)
                TryProgress(target, graphEnt, graphEnt.Comp.CurrentNode, currentEdge);

            return;
        }

        for (var edgeIndex = 0; edgeIndex < edges.Count; edgeIndex++)
        {
            var edge = Get(edges[edgeIndex]);

            if (edge.Steps.Count < 1)
                continue;

            if (!_entityConditions.TryConditions(target, edge.Conditions))
                continue;

            var step = edge.Steps[0];
            if (!step.RaiseCouldHandleEvent(target, this, ref args, user, graphEnt))
                continue;

            var location = new CraftingGraphLocation(graphEnt.Comp.CurrentNode, edgeIndex, 0);
            var result = step.RaiseHandleEvent(target, this, ref args, user, location, graphEnt);

            if (result.Progress)
            {
                TryProgress(target, graphEnt, graphEnt.Comp.CurrentNode, edgeIndex);
                return;
            }
            else if (result.Handled)
            {
                return;
            }
        }
    }

    private void OnDoAfter(Entity<CraftingGraphComponent> graphEnt, ref CraftingDoAfterEvent args)
    {
        if (args.Cancelled || args.Target is not { } target)
            return;

        if (args.Location.Node != graphEnt.Comp.CurrentNode)
            return;

        var graph = Get(graphEnt.Comp.Graph);
        var node = Get(graph.Nodes[graphEnt.Comp.CurrentNode]);
        var edges = Get(node.Edges);

        if (graphEnt.Comp.CurrentEdgeIndex is { } currentEdgeAndStep)
        {
            if (currentEdgeAndStep.Edge != args.Location.Edge || currentEdgeAndStep.Step != args.Location.Step)
                return;
        }
        else
        {
            if (args.Location.Step != 0)
                return;

            if (args.Location.Edge >= edges.Count)
                return;
        }

        var edge = Get(edges[args.Location.Edge]);
        var step = edge.Steps[args.Location.Step];

        step.RaiseDoAfterEvent(target, this, ref args, args.User, graphEnt);
        TryProgress(target, graphEnt, args.Location.Node, args.Location.Edge);
    }

    private void OnDoAfterAttempt(Entity<CraftingGraphComponent> graphEnt, ref DoAfterAttemptEvent<CraftingDoAfterEvent> args)
    {
        if (args.Event.Target is not { } target)
        {
            args.Cancel();
            return;
        }

        if (args.Event.Location.Node != graphEnt.Comp.CurrentNode)
        {
            args.Cancel();
            return;
        }

        if (graphEnt.Comp.CurrentEdgeIndex is { } currentEdgeAndStep)
        {
            if (currentEdgeAndStep.Edge != args.Event.Location.Edge || currentEdgeAndStep.Step != args.Event.Location.Step)
            {
                args.Cancel();
                return;
            }

            var graph = Get(graphEnt.Comp.Graph);
            var node = Get(graph.Nodes[graphEnt.Comp.CurrentNode]);
            var edges = Get(node.Edges);
            var edge = Get(edges[currentEdgeAndStep.Edge]);
            var step = edge.Steps[currentEdgeAndStep.Step];

            step.RaiseAttemptDoAfterEvent(target, this, ref args, args.Event.User, graphEnt);
        }
        else
        {
            if (args.Event.Location.Step != 0)
            {
                args.Cancel();
                return;
            }

            var graph = Get(graphEnt.Comp.Graph);
            var node = Get(graph.Nodes[graphEnt.Comp.CurrentNode]);
            var edges = Get(node.Edges);

            if (args.Event.Location.Edge >= edges.Count)
            {
                args.Cancel();
                return;
            }

            var edge = Get(edges[args.Event.Location.Edge]);
            var step = edge.Steps[args.Event.Location.Step];

            step.RaiseAttemptDoAfterEvent(target, this, ref args, args.Event.User, graphEnt);
        }
    }
}
