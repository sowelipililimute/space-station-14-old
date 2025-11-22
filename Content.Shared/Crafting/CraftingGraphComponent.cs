using Content.Shared.Cloning;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Crafting;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CraftingGraphSystem))]
public sealed partial class CraftingGraphComponent : Component
{
    [DataField(required: true, readOnly: true)]
    public CraftingGraphSelector Graph = default!;

    [DataField(required: true), AutoNetworkedField]
    public string CurrentNode = default!;

    [DataField, AutoNetworkedField]
    public (int Edge, int Step)? CurrentEdgeIndex;

    [DataField]
    public ProtoId<BecomeSettingsPrototype>? BecomeSettings;

    [DataField]
    public ProtoId<CloningSettingsPrototype>? CloningSettings;
}
