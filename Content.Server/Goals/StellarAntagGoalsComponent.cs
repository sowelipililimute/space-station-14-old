using Content.Shared.EntityTable.EntitySelectors;

namespace Content.Server.Goals;

[RegisterComponent]
[Access(typeof(StellarAntagGoalsSystem))]
public sealed partial class StellarAntagGoalsComponent : Component
{
    [DataField(required: true)]
    public EntityTableSelector Goals;
}
