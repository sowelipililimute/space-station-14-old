using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.EntityTable;
using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.Mind;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Goals;

public sealed partial class StellarGoalsSystem : EntitySystem
{
    [Dependency] private EntityTableSystem _entityTable = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedViewSubscriberSystem _viewSubscriber = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StellarGoalContainerObserverComponent, MindRelayedEvent<PlayerAttachedEvent>>(OnMindPlayerAttached);
        SubscribeLocalEvent<StellarGoalContainerObserverComponent, MindRelayedEvent<PlayerDetachedEvent>>(OnMindPlayerDetached);
        SubscribeLocalEvent<StellarGoalComponent, ComponentShutdown>(OnGoalShutdown);
    }

    private void OnMindPlayerAttached(Entity<StellarGoalContainerObserverComponent> ent,
        ref MindRelayedEvent<PlayerAttachedEvent> args)
    {
        foreach (var observed in ent.Comp.Observed)
        {
            _viewSubscriber.AddViewSubscriber(observed, args.Args.Player);
        }
    }

    private void OnMindPlayerDetached(Entity<StellarGoalContainerObserverComponent> ent,
        ref MindRelayedEvent<PlayerDetachedEvent> args)
    {
        foreach (var observed in ent.Comp.Observed)
        {
            _viewSubscriber.RemoveViewSubscriber(observed, args.Args.Player);
        }
    }

    private void OnGoalShutdown(Entity<StellarGoalComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<StellarGoalContainerComponent>(ent.Comp.Container, out var goalContainer))
            return;

        goalContainer.Goals.Remove(ent);
        Dirty(ent.Comp.Container, goalContainer);
    }

    /// <summary>
    /// Causes this mind to begin observing the goals within a container
    /// </summary>
    /// <param name="ent">The mind to add to the set of observers</param>
    /// <param name="container">The container to observe</param>
    [PublicAPI]
    public void ObserveContainer(Entity<StellarGoalContainerObserverComponent?> ent,
        Entity<StellarGoalContainerComponent?> container)
    {
        if (!Resolve(ent, ref ent.Comp) || !Resolve(container, ref container.Comp))
            return;

        ent.Comp.Observed.Add(container);
        Dirty(ent);

        if (TryComp<MindComponent>(ent, out var mind) && _player.TryGetSessionById(mind.UserId, out var session))
            _viewSubscriber.AddViewSubscriber(container, session);
    }

    /// <summary>
    /// Causes the mind to stop observing the goals within a container
    /// </summary>
    /// <param name="ent">The mind to remove from the set of observers</param>
    /// <param name="container">The container to stop observing</param>
    [PublicAPI]
    public void UnobserveContainer(Entity<StellarGoalContainerObserverComponent?> ent,
        Entity<StellarGoalContainerComponent?> container)
    {
        if (!Resolve(ent, ref ent.Comp) || !Resolve(container, ref container.Comp))
            return;

        ent.Comp.Observed.Remove(container);
        Dirty(ent);

        if (TryComp<MindComponent>(ent, out var mind) && _player.TryGetSessionById(mind.UserId, out var session))
            _viewSubscriber.RemoveViewSubscriber(container, session);
    }

    /// <summary>
    /// Tests whether a mind is currently observing the goals within a container
    /// </summary>
    /// <param name="ent">The mind to check for in the container's observers</param>
    /// <param name="container">The container to check for the presence of the mind in</param>
    /// <returns>Whether or not the mind is currently observing the container</returns>
    [PublicAPI]
    public bool IsObservingContainer(Entity<StellarGoalContainerObserverComponent?> ent,
        Entity<StellarGoalContainerComponent?> container)
    {
        if (!Resolve(ent, ref ent.Comp) || !Resolve(container, ref container.Comp))
            return false;

        return ent.Comp.Observed.Contains(container.Owner);
    }

    /// <summary>
    /// Spawns a new goal container
    /// </summary>
    /// <param name="name">The name of the container, used only for debugging purposes</param>
    /// <returns>The newly spawned goal container</returns>
    [PublicAPI]
    public Entity<StellarGoalContainerComponent> SpawnContainer(string name)
    {
        var containerMap = _map.CreateMap(out var mapId);
        _metaData.SetEntityName(containerMap, $"Goals map - {name}");

        var container = Spawn(null, new MapCoordinates(0, 0, mapId));
        var comp = AddComp<StellarGoalContainerComponent>(container);
        _metaData.SetEntityName(container, $"Goals container - {name}");

        return (container, comp);
    }

    /// <summary>
    /// Attempts to spawn a goal into the given container
    /// </summary>
    /// <param name="ent">The goal container to add the goal to</param>
    /// <param name="protoId">The entity prototype ID of the goal to spawn in the container</param>
    /// <param name="goal">The newly spawned goal</param>
    /// <returns>Whether the goal could be spawned</returns>
    [PublicAPI]
    public bool TryAddGoal(
        Entity<StellarGoalContainerComponent?> ent,
        EntProtoId protoId,
        [NotNullWhen(true)] out Entity<StellarGoalComponent>? goal)
    {
        goal = null;
        if (!Resolve(ent, ref ent.Comp))
            return false;

        var uid = PredictedSpawnNextToOrDrop(protoId, ent);
        var comp = Comp<StellarGoalComponent>(uid);
        goal = (uid, comp);

        ent.Comp.Goals.Add(goal.Value);
        Dirty(ent);

        goal.Value.Comp.Container = ent;
        Dirty(goal.Value);

        var evt = new StellarGoalInsertedEvent((ent, ent.Comp));
        RaiseLocalEvent(goal.Value, ref evt);

        return true;
    }

    /// <summary>
    /// Attempts to add goals in bulk to a goal container
    /// </summary>
    /// <param name="ent">The container to add goals to</param>
    /// <param name="table">The selector containing all the goals to add</param>
    /// <returns>Whether all goals could be spawned</returns>
    [PublicAPI]
    public bool TryAddGoals(Entity<StellarGoalContainerComponent?> ent, EntityTableSelector table)
    {
        var spawns = _entityTable.GetSpawns(table);
        return spawns.All(spawn => TryAddGoal(ent, spawn, out _));
    }

    /// <inheritdoc cref="TryAddGoals(Entity{StellarGoalContainerComponent?}, EntityTableSelector)" />
    [PublicAPI]
    public bool TryAddGoals(Entity<StellarGoalContainerComponent?> ent, ProtoId<EntityTablePrototype> table)
    {
        return TryAddGoals(ent, _prototype.Index(table).Table);
    }

    /// <summary>
    /// Gets the goal container for a mind representing its individual goals
    /// </summary>
    /// <param name="ent">The mind to get the individual goal container of</param>
    /// <returns>The goal container representing goals held by this entity individually</returns>
    [PublicAPI]
    public EntityUid GetIndividualGoalContainer(Entity<StellarGoalContainerObserverComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
        {
            DebugTools.Assert($"Invalid mind {ToPrettyString(ent):ent} passed into {nameof(GetIndividualGoalContainer)}");
            return EntityUid.Invalid;
        }

        if (ent.Comp.OwnedContainer is not { } owned)
        {
            var newContainer = SpawnContainer(ToPrettyString(ent));
            newContainer.Comp.IndividualOwner = ent;
            Dirty(newContainer);

            owned = newContainer;
            ent.Comp.OwnedContainer = owned;
            ObserveContainer(ent, owned);
        }

        return owned;
    }

    /// <summary>
    /// Gets the observer that individually owns this container, if it has one
    /// </summary>
    /// <param name="ent">The container to look up the individual owner of</param>
    /// <param name="owner">The observer that individually owns this container</param>
    /// <returns>Whether or not the container has an owning observer</returns>
    [PublicAPI]
    public bool TryGetIndividualGoalOwner(Entity<StellarGoalContainerComponent?> ent, [NotNullWhen(true)] out EntityUid? owner)
    {
        if (!Resolve(ent, ref ent.Comp))
        {
            owner = null;
            return false;
        }

        owner = ent.Comp.IndividualOwner;
        return owner is not null;
    }

    /// <summary>
    /// Lists all goals being observed by this mind
    /// </summary>
    /// <param name="ent">The mind to enumerate goals of</param>
    /// <returns>All goals that this mind is observing</returns>
    [PublicAPI]
    public List<Entity<StellarGoalComponent>> GetGoals(Entity<StellarGoalContainerObserverComponent?> ent)
    {
        return GetGoals<StellarGoalComponent>(ent).ToList();
    }

    /// <summary>
    /// Lists all goals being observed by this mind with the given component
    /// </summary>
    /// <param name="ent">The mind to enumerate goals of</param>
    /// <typeparam name="T">The component type to test for</typeparam>
    /// <returns>All goals that this mind is observing with the given component</returns>
    [PublicAPI]
    public IEnumerable<Entity<T>> GetGoals<T>(Entity<StellarGoalContainerObserverComponent?> ent) where T : IComponent
    {
        if (!Resolve(ent, ref ent.Comp))
            yield break;

        foreach (var container in ent.Comp.Observed)
        {
            if (!TryComp<StellarGoalContainerComponent>(container, out var containerComp))
                continue;

            foreach (var goal in containerComp.Goals)
            {
                if (TryComp<T>(goal, out var goalComp))
                    yield return (goal, goalComp);
            }
        }
    }

    /// <summary>
    /// Refreshes the progress of a goal
    /// </summary>
    /// <param name="ent">The goal to refresh the progress of</param>
    [PublicAPI]
    public void RefreshProgress(Entity<StellarGoalComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        var progress = new StellarGetGoalProgressEvent();
        RaiseLocalEvent(ent, ref progress);

        var clamped = Math.Clamp(progress.Progress, 0d, 1d);
        if (MathHelper.CloseTo(clamped, ent.Comp.Progress))
            return;

        var oldProgress = ent.Comp.Progress;
        ent.Comp.Progress = clamped;
        Dirty(ent);

        var notification = new StellarGoalProgressChangedEvent(oldProgress, clamped);
        RaiseLocalEvent(ref notification);
    }
}
