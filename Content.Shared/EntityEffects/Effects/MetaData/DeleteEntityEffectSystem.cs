namespace Content.Shared.EntityEffects.Effects.MetaData;

/// <summary>
/// Queues the given entity for deletion
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class DeleteEntityEffectSystem : EntityEffectSystem<MetaDataComponent, DeleteEntity>
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    protected override void Effect(Entity<MetaDataComponent> ent, ref EntityEffectEvent<DeleteEntity> args)
    {
        PredictedQueueDel(ent.AsNullable());
    }
}

public sealed partial class DeleteEntity : EntityEffectBase<DeleteEntity>;
