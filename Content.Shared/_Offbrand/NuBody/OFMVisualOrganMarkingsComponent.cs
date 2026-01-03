using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Offbrand.NuBody;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
[Access(typeof(SharedOFMVisualBodySystem))]
public sealed partial class OFMVisualOrganMarkingsComponent : Component
{
    /// <summary>
    /// The layers on the entity that this can contain markings for
    /// </summary>
    [DataField(required: true)]
    public HashSet<HumanoidVisualLayers> Layers;

    /// <summary>
    /// The list of markings to apply to the entity
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<Marking> Markings = new();

    /// <summary>
    /// The type of organ this is for markings
    /// </summary>
    [DataField(required: true)]
    public ProtoId<MarkingsGroupPrototype> Group;

    /// <summary>
    /// Client only - the last markings applied by this component
    /// </summary>
    public List<Marking> AppliedMarkings = new();
}
