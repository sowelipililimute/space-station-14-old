using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Offbrand.NuBody;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
[Access(typeof(SharedOFMVisualBodySystem))]
public sealed partial class OFMVisualOrganMarkingsComponent : Component
{
    /// <summary>
    /// What markings this organ can take
    /// </summary>
    [DataField(required: true), AlwaysPushInheritance]
    public OrganMarkingData MarkingData = default!;

    /// <summary>
    /// The list of markings to apply to the entity
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<HumanoidVisualLayers, List<Marking>> Markings = new();

    /// <summary>
    /// Client only - the last markings applied by this component
    /// </summary>
    [ViewVariables]
    public List<Marking> AppliedMarkings = new();
}

/// <summary>
/// Defines the layers and group an organ takes markings for
/// </summary>
[DataDefinition]
[Serializable, NetSerializable]
public partial record struct OrganMarkingData
{
    [DataField(required: true)]
    public HashSet<HumanoidVisualLayers> Layers = default!;

    /// <summary>
    /// The type of organ this is for markings
    /// </summary>
    [DataField(required: true)]
    public ProtoId<MarkingsGroupPrototype> Group = default!;
}
