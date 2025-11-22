using Content.Shared.Coordinates.Helpers;

namespace Content.Shared.EntityEffects.Effects.Transform;

/// <summary>
/// Snaps this entity to the grid
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class SnapToGridEntityEffectSystem : EntityEffectSystem<TransformComponent, SnapToGrid>
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    protected override void Effect(Entity<TransformComponent> ent, ref EntityEffectEvent<SnapToGrid> args)
    {
        if (!ent.Comp.Anchored)
            _transform.SetCoordinates(ent, ent.Comp.Coordinates.SnapToGrid(EntityManager));

        if (args.Effect.Rotation is { } rotation)
            _transform.SetLocalRotation(ent, Angle.FromDegrees(rotation), ent);
    }
}

public sealed partial class SnapToGrid : EntityEffectBase<SnapToGrid>
{
    [DataField]
    public double? Rotation = 0;
}
