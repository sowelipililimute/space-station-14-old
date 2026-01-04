using Robust.Shared.GameStates;

namespace Content.Shared._Offbrand.NuBody;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedOFMVisualBodySystem))]
public sealed partial class OFMVisualBodyComponent : Component;
