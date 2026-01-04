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
        SubscribeLocalEvent<OFMVisualOrganComponent, BodyRelayedEvent<ApplyOrganProfileDataEvent>>(OnVisualOrganApplyProfile);
        SubscribeLocalEvent<OFMVisualOrganMarkingsComponent, BodyRelayedEvent<ApplyOrganMarkingsEvent>>(OnMarkingsOrganApplyMarkings);

        InitializeModifiers();
    }

    private List<Marking> ResolveMarkings(List<Marking> markings, Color? skinColor, Color? eyeColor)
    {
        var ret = new List<Marking>();
        var forcedColors = new List<(Marking, MarkingPrototype)>();

        foreach (var marking in markings)
        {
            if (!_marking.TryGetMarking(marking, out var proto))
                continue;

            if (!proto.ForcedColoring)
                ret.Add(marking);
            else
                forcedColors.Add((marking, proto));
        }

        foreach (var (marking, prototype) in forcedColors)
        {
            var colors = MarkingColoring.GetMarkingLayerColors(
                prototype,
                skinColor,
                eyeColor,
                ret);

            var markingWithColor = new Marking(marking.MarkingId, colors)
            {
                Forced = marking.Forced,
            };
            ret.Add(markingWithColor);
        }

        return ret;
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

    public void ApplyProfileTo(Entity<OFMVisualBodyComponent?> ent, HumanoidCharacterProfile profile)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        var profileEvt = new ApplyOrganProfileDataEvent(new()
        {
            Sex = profile.Sex,
            SkinColor = profile.Appearance.SkinColor,
            EyeColor = profile.Appearance.EyeColor,
        });
        RaiseLocalEvent(ent, ref profileEvt);

        var markingsEvt = new ApplyOrganMarkingsEvent(profile.Appearance.Markings);
        RaiseLocalEvent(ent, ref markingsEvt);
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

        if (!other.MarkingData.Layers.SetEquals(ent.Comp.MarkingData.Layers))
            return;

        SetOrganMarkings(ent, other.Markings);
    }

    private void OnVisualOrganApplyProfile(Entity<OFMVisualOrganComponent> ent, ref BodyRelayedEvent<ApplyOrganProfileDataEvent> args)
    {
        ent.Comp.Profile = args.Args.Data;

        if (ent.Comp.Layer.Equals(HumanoidVisualLayers.Eyes))
            SetOrganColor(ent, ent.Comp.Profile.EyeColor);
        else
            SetOrganColor(ent, ent.Comp.Profile.SkinColor);
    }

    private void OnMarkingsOrganApplyMarkings(Entity<OFMVisualOrganMarkingsComponent> ent, ref BodyRelayedEvent<ApplyOrganMarkingsEvent> args)
    {
        if (Comp<OFMOrganComponent>(ent).Category is not { } category)
            return;

        if (!args.Args.Markings.TryGetValue(category, out var markingSet))
            return;

        var organMarkings = new List<Marking>();

        foreach (var layer in ent.Comp.MarkingData.Layers)
        {
            if (!markingSet.TryGetValue(layer, out var markings))
                continue;

            foreach (var marking in markings)
            {
                if (!_marking.TryGetMarking(marking, out var proto))
                    continue;

                organMarkings.Add(marking);
            }
        }

        var profile = Comp<OFMVisualOrganComponent>(ent).Profile;
        var resolved = ResolveMarkings(organMarkings, profile.SkinColor, profile.EyeColor);

        SetOrganMarkings(ent, resolved);
    }
}

/// <summary>
/// Raised on body entity, when an organ is having its appearance copied to it
/// </summary>
[ByRefEvent]
public readonly record struct OrganCopyAppearanceEvent(EntityUid Organ);

/// <summary>
/// Raised on body entity when a profile is being applied to it
/// </summary>
[ByRefEvent]
public readonly record struct ApplyOrganProfileDataEvent(OrganProfileData Data);

/// <summary>
/// Raised on body entity when a profile is being applied to it
/// </summary>
[ByRefEvent]
public readonly record struct ApplyOrganMarkingsEvent(Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> Markings);

