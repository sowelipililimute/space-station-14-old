using Content.Shared.Administration.Managers;
using Content.Shared.Administration;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Offbrand.NuBody;

public abstract partial class SharedOFMVisualBodySystem
{
	[Dependency] private readonly ISharedAdminManager _admin = default!;
	[Dependency] private readonly SharedUserInterfaceSystem _userInterface = default!;

    private void InitializeModifiers()
    {
    	SubscribeLocalEvent<OFMVisualBodyComponent, GetVerbsEvent<Verb>>(OnGetVerbs);

        Subs.BuiEvents<OFMVisualBodyComponent>(HumanoidMarkingModifierKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnModifiersOpened);
            subs.Event<HumanoidMarkingModifierMarkingSetMessage>(OnSetModifiers);
        });
    }

    private void OnGetVerbs(Entity<OFMVisualBodyComponent> ent, ref GetVerbsEvent<Verb> args)
    {
    	if (!_admin.HasAdminFlag(args.User, AdminFlags.Fun))
    		return;

        var user = args.User;
    	args.Verbs.Add(new Verb
    	{
    		Text = "Modify markings",
    		Category = VerbCategory.Tricks,
			Icon = new SpriteSpecifier.Rsi(new("/Textures/Mobs/Customization/reptilian_parts.rsi"), "tail_smooth"),
			Act = () =>
			{
				_userInterface.OpenUi(ent.Owner, HumanoidMarkingModifierKey.Key, user);
			}
    	});
    }

    /// <summary>
    /// Gathers all the markings-relevant data from this entity
    /// </summary>
    /// <param name="filter">If set, only returns data concerning the given layers</param>
    public void GatherMarkingsData(Entity<OFMVisualBodyComponent> ent,
        HashSet<HumanoidVisualLayers>? filter,
        out Dictionary<ProtoId<OrganCategoryPrototype>, OrganProfileData> profiles,
        out Dictionary<ProtoId<OrganCategoryPrototype>, OrganMarkingData> markings,
        out Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> applied)
    {
        profiles = new();
        markings = new();
        applied = new();

        var organContainer = _container.EnsureContainer<Container>(ent, OFMBodyComponent.ContainerID);

        foreach (var organ in organContainer.ContainedEntities)
        {
            if (!TryComp<OFMOrganComponent>(organ, out var organComp) || organComp.Category is not { } category)
                continue;

            if (TryComp<OFMVisualOrganComponent>(organ, out var visualOrgan))
            {
                profiles[category] = visualOrgan.Profile;
            }

            if (TryComp<OFMVisualOrganMarkingsComponent>(organ, out var visualOrganMarkings))
            {
                markings[category] = visualOrganMarkings.MarkingData;

                var dict = new Dictionary<HumanoidVisualLayers, List<Marking>>();
                foreach (var marking in visualOrganMarkings.Markings)
                {
                    if (!_marking.TryGetMarking(marking, out var markingData))
                        continue;

                    dict[markingData.BodyPart] = dict.GetValueOrDefault(markingData.BodyPart) ?? [];
                    dict[markingData.BodyPart].Add(marking);
                }

                applied[category] = dict;
            }
        }
    }

    private void OnModifiersOpened(Entity<OFMVisualBodyComponent> ent, ref BoundUIOpenedEvent args)
    {
        GatherMarkingsData(ent, null, out var profiles, out var markings, out var applied);

        _userInterface.SetUiState(ent.Owner, HumanoidMarkingModifierKey.Key, new HumanoidMarkingModifierState(applied, markings, profiles));
    }

    private void OnSetModifiers(Entity<OFMVisualBodyComponent> ent, ref HumanoidMarkingModifierMarkingSetMessage args)
    {
        var markingsEvt = new ApplyOrganMarkingsEvent(args.Markings);
        RaiseLocalEvent(ent, ref markingsEvt);
    }

    public void ApplyMarkings(EntityUid ent, Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> markings)
    {
        var markingsEvt = new ApplyOrganMarkingsEvent(markings);
        RaiseLocalEvent(ent, ref markingsEvt);
    }
}
