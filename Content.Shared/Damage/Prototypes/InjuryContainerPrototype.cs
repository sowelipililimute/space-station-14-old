using Content.Shared.Damage.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Damage.Prototypes;

/// <summary>
///     An injury container which can be used to specify support for various injury types.
/// </summary>
[Prototype]
public sealed partial class InjuryContainerPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    ///     Dictionary of damage types to injury types for this container.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<DamageTypePrototype>, ProtoId<DamageTypePrototype>> SupportedTypes = new();
}
