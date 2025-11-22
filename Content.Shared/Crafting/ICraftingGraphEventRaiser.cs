using Content.Shared.DoAfter;
using Robust.Shared.Prototypes;

namespace Content.Shared.Crafting;

public interface ICraftingGraphEventRaiser
{
    bool RaiseCouldHandleEvent<TStep, TEvent>(EntityUid target, TStep step, ref TEvent args, EntityUid? user, Entity<CraftingGraphComponent> graph) where TStep : CraftingGraphStepBase<TStep>;
    CraftingGraphStepResult RaiseHandleEvent<TStep, TEvent>(EntityUid target, TStep step, ref TEvent args, EntityUid? user, CraftingGraphLocation location, Entity<CraftingGraphComponent> graph) where TStep : CraftingGraphStepBase<TStep>;
    EntProtoId? RaiseResultEvent<TEntity>(EntityUid target, TEntity entity) where TEntity : CraftingGraphResultBase<TEntity>;
    void RaiseDoAfterEvent<TStep>(EntityUid target, TStep step, ref CraftingDoAfterEvent args, EntityUid? user, Entity<CraftingGraphComponent> graph) where TStep : CraftingGraphStepBase<TStep>;
    void RaiseAttemptDoAfterEvent<TStep>(EntityUid targer, TStep step, ref DoAfterAttemptEvent<CraftingDoAfterEvent> args, EntityUid? user, Entity<CraftingGraphComponent> graph) where TStep : CraftingGraphStepBase<TStep>;
}
