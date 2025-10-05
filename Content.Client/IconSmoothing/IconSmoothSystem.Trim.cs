using Content.Shared.IconSmoothing;
using Robust.Client.GameObjects;

namespace Content.Client.IconSmoothing;

public sealed partial class IconSmoothSystem
{
    private void InitializeTrim()
    {
        SubscribeLocalEvent<EdgeTrimComponent, ComponentStartup>(OnTrimStartup);
        SubscribeLocalEvent<EdgeTrimComponent, ComponentShutdown>(OnTrimShutdown);
    }

    private void InitializeTrimLayers(Entity<IconSmoothComponent, SpriteComponent?, EdgeTrimComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp2, ref ent.Comp3, false))
            return;

        foreach (var layer in Enum.GetValues<TrimLayer>())
        {
            var state = $"{ent.Comp1.StateBase}trim{(byte)layer}";

            _sprite.LayerMapRemove((ent.Owner, ent.Comp2), layer);
            _sprite.LayerMapSet((ent.Owner, ent.Comp2), layer, _sprite.AddRsiLayer((ent.Owner, ent.Comp2), state));
        }
    }

    private void OnTrimStartup(Entity<EdgeTrimComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<IconSmoothComponent>(ent, out var smooth))
            return;

        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        InitializeTrimLayers((ent.Owner, smooth, sprite, ent.Comp));

        _sprite.LayerSetVisible((ent, sprite), TrimLayer.South, false);
        _sprite.LayerSetVisible((ent, sprite), TrimLayer.East, false);
        _sprite.LayerSetVisible((ent, sprite), TrimLayer.North, false);
        _sprite.LayerSetVisible((ent, sprite), TrimLayer.West, false);
    }

    private void OnTrimShutdown(Entity<EdgeTrimComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        _sprite.LayerMapRemove((ent, sprite), TrimLayer.South);
        _sprite.LayerMapRemove((ent, sprite), TrimLayer.East);
        _sprite.LayerMapRemove((ent, sprite), TrimLayer.North);
        _sprite.LayerMapRemove((ent, sprite), TrimLayer.West);
    }

    private void CalculateTrim(Entity<EdgeTrimComponent?, SpriteComponent?> ent, DirectionFlag directions)
    {
        if (!Resolve(ent, ref ent.Comp1, ref ent.Comp2, false))
            return;

        for (var i = 0; i < 4; i++)
        {
            var dir = (DirectionFlag)(1 << i);
            var trim = GetTrim(dir);

            _sprite.LayerSetVisible((ent.Owner, ent.Comp2), trim, (dir & directions) != 0x0);
        }
    }

    private TrimLayer GetTrim(DirectionFlag direction)
    {
        switch (direction)
        {
            case DirectionFlag.South:
                return TrimLayer.South;
            case DirectionFlag.East:
                return TrimLayer.East;
            case DirectionFlag.North:
                return TrimLayer.North;
            case DirectionFlag.West:
                return TrimLayer.West;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private enum TrimLayer : byte
    {
        South,
        East,
        North,
        West
    }
}
