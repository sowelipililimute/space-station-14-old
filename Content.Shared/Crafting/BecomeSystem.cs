using Content.Shared.Cloning;
using Content.Shared.Containers;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared.Crafting;

public sealed class BecomeSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedCloningSystem _cloning = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ContainerManagerComponent, BeforeBecomeEntityEvent>(OnContainerBecome);
        SubscribeLocalEvent<ContainerFillComponent, DuringBecomeEntityEvent>(OnContainerFillBecome);
    }

    public EntityUid Become(EntityUid oldUid, EntProtoId prototype, ProtoId<BecomeSettingsPrototype>? becomeSettings = null, ProtoId<CloningSettingsPrototype>? cloningSettings = null)
    {
        if (prototype.Equals(MetaData(oldUid).EntityPrototype?.ID))
            return oldUid;

        _prototype.Resolve(becomeSettings, out var become);
        var xform = Transform(oldUid);

        var newUid = EntityManager.CreateEntityUninitialized(prototype, xform.Coordinates);
        EntityManager.FlagPredicted(newUid);

        if (cloningSettings is { } cloning)
            _cloning.CloneComponents(oldUid, newUid, cloning);

        var newXform = Transform(newUid);
        _transform.AttachToGridOrMap(newUid, newXform);
        newXform.LocalRotation = xform.LocalRotation;
        newXform.Anchored = xform.Anchored;

        var beforeEvt = new BeforeBecomeEntityEvent(oldUid, newUid, become);
        RaiseLocalEvent(oldUid, ref beforeEvt);

        var duringEvt = new DuringBecomeEntityEvent(oldUid, newUid, become);
        RaiseLocalEvent(newUid, ref duringEvt);

        EntityManager.InitializeAndStartEntity(newUid);
        PredictedQueueDel(oldUid);

        var afterEvt = new AfterBecomeEntityEvent(become);
        RaiseLocalEvent(newUid, ref afterEvt);

        return newUid;
    }

    private void OnContainerBecome(Entity<ContainerManagerComponent> oldEnt, ref BeforeBecomeEntityEvent args)
    {
        if (args.Settings?.TransferContainers is not { } containers || containers.Count == 0)
            return;

        var newEnt = new Entity<ContainerManagerComponent>(args.New, EnsureComp<ContainerManagerComponent>(args.New));

        foreach (var container in containers)
        {
            if (!_container.TryGetContainer(oldEnt, container, out var oldContainer, oldEnt))
                continue;

            if (!_container.TryGetContainer(newEnt, container, out var newContainer, newEnt))
            {
                if (oldContainer is Container)
                    newContainer = _container.EnsureContainer<Container>(newEnt, container, newEnt);
                else if (oldContainer is ContainerSlot)
                    newContainer = _container.EnsureContainer<ContainerSlot>(newEnt, container, newEnt);
                else
                    throw new InvalidOperationException($"tried to clone unknown container {container} {oldContainer} on {ToPrettyString(oldEnt):oldEnt}");
            }

            for (var i = oldContainer.ContainedEntities.Count-1; i >= 0; i--)
            {
                var contained = oldContainer.ContainedEntities[i];
                _container.Remove(contained, oldContainer, reparent: false, force: true);
                _container.Insert(contained, newContainer);
            }
        }
    }

    private void OnContainerFillBecome(Entity<ContainerFillComponent> newEnt, ref DuringBecomeEntityEvent args)
    {
        if (args.Settings?.TransferContainers is not { } containers || !newEnt.Comp.IgnoreConstructionSpawn)
            return;

        foreach (var container in containers)
        {
            newEnt.Comp.Containers.Remove(container);
        }
    }
}

/// <summary>
/// Raised on the old entity during the process of an entity becoming a new one, after the new one has been created but not yet initialized
/// </summary>
[ByRefEvent]
public record struct BeforeBecomeEntityEvent(EntityUid Old, EntityUid New, BecomeSettingsPrototype? Settings);

/// <summary>
/// Raised on the new entity during the process of an entity becoming a new one, after the new one has been created but not yet initialized.
/// </summary>
[ByRefEvent]
public record struct DuringBecomeEntityEvent(EntityUid Old, EntityUid New, BecomeSettingsPrototype? Settings);

/// <summary>
/// Raised on the new entity during the process of an entity becoming a new one, after the new entity is initialized.
/// </summary>
[ByRefEvent]
public record struct AfterBecomeEntityEvent(BecomeSettingsPrototype? Settings);

