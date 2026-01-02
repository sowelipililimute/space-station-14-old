using System.Linq;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Humanoid;
using Content.Shared.Preferences;
using Robust.Shared.Containers;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared._Offbrand.NuBody;

public abstract partial class SharedOFMVisualBodySystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly MarkingManager _marking = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OFMVisualOrganComponent, BodyRelayedEvent<OrganCopyAppearanceEvent>>(OnVisualOrganCopyAppearance);
        SubscribeLocalEvent<OFMVisualOrganMarkingsComponent, BodyRelayedEvent<OrganCopyAppearanceEvent>>(OnMarkingsOrganCopyAppearance);
    }

    private List<Marking> ResolveMarkings(ProtoId<SpeciesPrototype> species, HumanoidCharacterProfile profile, HumanoidCharacterAppearance appearance)
    {
        var markingsSet = new MarkingSet();

        var speciesProto = _prototype.Index(species);
        var pointsProto = _prototype.Index(speciesProto.MarkingPoints);
        markingsSet.Points = MarkingPoints.CloneMarkingPointDictionary(pointsProto.Points);

        // General markings
        var forcedColorMarkings = new List<(Marking, MarkingPrototype)>();
        foreach (var marking in appearance.Markings)
        {
            if (!_marking.TryGetMarking(marking, out var proto))
                continue;

            // Anything with forced colouring needs to be added after everything else is situated
            if (!proto.ForcedColoring)
            {
                markingsSet.AddBack(proto.MarkingCategory, marking);
            }
            else
            {
                forcedColorMarkings.Add((marking, proto));
            }
        }

        // Hair & facial hair (I hate this)

        var facialHairColor = _marking.MustMatchSkin(species, HumanoidVisualLayers.FacialHair, out var facialHairAlpha, _prototype)
            ? appearance.SkinColor.WithAlpha(facialHairAlpha)
            : appearance.FacialHairColor;

        var facialHair = new Marking(appearance.FacialHairStyleId,
            new[] { facialHairColor });

        if (_marking.TryGetMarking(facialHair, out var facialProto) &&
            _marking.CanBeApplied(species, profile.Sex, facialProto, _prototype))
        {
            markingsSet.AddBack(facialProto.MarkingCategory, facialHair);
        }

        var hairColor = _marking.MustMatchSkin(species, HumanoidVisualLayers.Hair, out var hairAlpha, _prototype)
            ? appearance.SkinColor.WithAlpha(hairAlpha)
            : appearance.HairColor;

        var hair = new Marking(appearance.HairStyleId,
            new[] { hairColor });

        if (_marking.TryGetMarking(hair, out var hairProto) &&
            _marking.CanBeApplied(species, profile.Sex, hairProto, _prototype))
        {
            markingsSet.AddBack(hairProto.MarkingCategory, hair);
        }

        // Ensure the species of this adds up
        markingsSet.EnsureSpecies(species, appearance.SkinColor, _marking, _prototype);

        // Now we go through forced colour markings
        foreach (var (marking, prototype) in forcedColorMarkings)
        {
            var colors = MarkingColoring.GetMarkingLayerColors(
                prototype,
                appearance.SkinColor,
                appearance.EyeColor,
                markingsSet
            );

            var markingWithColor = new Marking(marking.MarkingId, colors);
            markingsSet.AddBack(prototype.MarkingCategory, markingWithColor);
        }

        // Now we ensure defaults
        markingsSet.EnsureDefault(appearance.SkinColor, appearance.EyeColor, _marking);

        return markingsSet.GetForwardEnumerator().ToList();
    }

    protected virtual void SetOrganColor(Entity<OFMVisualOrganComponent> ent, Color color)
    {
        ent.Comp.Data.Color = color;
        Dirty(ent);
    }

    protected virtual void SetOrganAppearance(Entity<OFMVisualOrganComponent> ent, PrototypeLayerData data)
    {
        ent.Comp.Data = data;
        Dirty(ent);
    }

    protected virtual void SetOrganMarkings(Entity<OFMVisualOrganMarkingsComponent> ent, List<Marking> markings)
    {
        ent.Comp.Markings = markings;
        Dirty(ent);
    }

    public void ApplyProfileTo(Entity<OFMBodyComponent?> ent, HumanoidCharacterProfile profile)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        var organContainer = _container.EnsureContainer<Container>(ent, OFMBodyComponent.ContainerID);
        var markings = ResolveMarkings(profile.Species, profile, profile.Appearance);

        var debug = EnsureComp<OFMDebugAppearanceComponent>(ent);
        debug.Appearance = profile.Appearance.Clone();
        Dirty(ent, debug);

        debug.Appearance.Markings = markings;

        foreach (var organ in organContainer.ContainedEntities)
        {
            if (TryComp<OFMVisualOrganComponent>(organ, out var visualOrgan))
            {
                if (visualOrgan.Layer.Equals(HumanoidVisualLayers.Eyes))
                    SetOrganColor((organ, visualOrgan), profile.Appearance.EyeColor);
                else
                    SetOrganColor((organ, visualOrgan), profile.Appearance.SkinColor);
            }

            if (TryComp<OFMVisualOrganMarkingsComponent>(organ, out var visualOrganMarkings))
            {
                var organMarkings = new List<Marking>();

                foreach (var marking in markings)
                {
                    if (!_marking.TryGetMarking(marking, out var proto))
                        continue;

                    if (!visualOrganMarkings.Layers.Contains(proto.BodyPart))
                        continue;

                    if (_marking.CanBeApplied(profile.Species, profile.Sex, proto, _prototype))
                    {
                        organMarkings.Add(marking);
                    }
                }

                SetOrganMarkings((organ, visualOrganMarkings), organMarkings);
            }
        }
    }

    public void CopyAppearanceFrom(Entity<OFMBodyComponent?> source, Entity<OFMBodyComponent?> target)
    {
        if (!Resolve(source, ref source.Comp) || !Resolve(target, ref target.Comp))
            return;

        var sourceOrgans = _container.EnsureContainer<Container>(source, OFMBodyComponent.ContainerID);

        foreach (var sourceOrgan in sourceOrgans.ContainedEntities)
        {
            var evt = new OrganCopyAppearanceEvent(sourceOrgan);
            RaiseLocalEvent(target, ref evt);
        }
    }

    [Dependency] private readonly Content.Shared.Humanoid.HumanoidProfileSystem _humanoidProfile = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    public void SpawnRandomNurist(ProtoId<SpeciesPrototype> species, EntityCoordinates coordinates)
    {
        var speciesProto = _prototype.Index(species);

        var profile = HumanoidCharacterProfile.RandomWithSpecies(species);

        var humanoid = EntityManager.CreateEntityUninitialized(speciesProto.OFMMobPrototype, coordinates);
        EntityManager.InitializeAndStartEntity(humanoid);

        ApplyProfileTo((humanoid, Comp<OFMBodyComponent>(humanoid)), profile);

        _humanoidProfile.ApplyProfileTo(humanoid, profile);
        _metaData.SetEntityName(humanoid, profile.Name);
    }

    private void OnVisualOrganCopyAppearance(Entity<OFMVisualOrganComponent> ent, ref BodyRelayedEvent<OrganCopyAppearanceEvent> args)
    {
        if (!TryComp<OFMVisualOrganComponent>(args.Args.Organ, out var other))
            return;

        if (!other.Layer.Equals(ent.Comp.Layer))
            return;

        SetOrganAppearance(ent, other.Data);
    }

    private void OnMarkingsOrganCopyAppearance(Entity<OFMVisualOrganMarkingsComponent> ent, ref BodyRelayedEvent<OrganCopyAppearanceEvent> args)
    {
        if (!TryComp<OFMVisualOrganMarkingsComponent>(args.Args.Organ, out var other))
            return;

        if (!other.Layers.SetEquals(ent.Comp.Layers))
            return;

        SetOrganMarkings(ent, other.Markings);
    }
}

/// <summary>
/// Raised on body entity, when an organ is having its appearance copied to it
/// </summary>
[ByRefEvent]
public readonly record struct OrganCopyAppearanceEvent(EntityUid Organ);
