using Content.Shared.Humanoid;

namespace Content.Shared._Offbrand.NuBody;

public sealed partial class OFMBodySystem
{
    private void InitializeRelay()
    {
        SubscribeLocalEvent<OFMBodyComponent, OrganCopyAppearanceEvent>(RefRelayBodyEvent);
        SubscribeLocalEvent<OFMBodyComponent, HumanoidLayerVisibilityChangedEvent>(RefRelayBodyEvent);
    }

    private void RefRelayBodyEvent<T>(EntityUid uid, OFMBodyComponent component, ref T args) where T : struct
    {
        RelayEvent((uid, component), ref args);
    }

    private void RelayBodyEvent<T>(EntityUid uid, OFMBodyComponent component, T args) where T : class
    {
        RelayEvent((uid, component), args);
    }

    public void RelayEvent<T>(Entity<OFMBodyComponent> ent, ref T args) where T : struct
    {
        var ev = new BodyRelayedEvent<T>(ent, args);
        foreach (var organ in ent.Comp.Organs?.ContainedEntities ?? [])
        {
            RaiseLocalEvent(organ, ref ev);
        }
        args = ev.Args;
    }

    public void RelayEvent<T>(Entity<OFMBodyComponent> ent, T args) where T : class
    {
        var ev = new BodyRelayedEvent<T>(ent, args);
        foreach (var organ in ent.Comp.Organs?.ContainedEntities ?? [])
        {
            RaiseLocalEvent(organ, ref ev);
        }
    }
}

/// <summary>
/// Event wrapper for relayed events.
/// </summary>
[ByRefEvent]
public record struct BodyRelayedEvent<TEvent>(Entity<OFMBodyComponent> Body, TEvent Args);
