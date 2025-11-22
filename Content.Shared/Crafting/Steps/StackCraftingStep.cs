using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Stacks;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Crafting.Steps;

public sealed partial class StackCraftingStep : CraftingGraphStepBase<StackCraftingStep>
{
    [DataField(required: true)]
    public ProtoId<StackPrototype> Stack;

    [DataField]
    public int Amount = 1;

    [DataField]
    public TimeSpan Delay;
}

public sealed class StackCraftingStepSystem : CraftingGraphStepDoAfterSystem<MetaDataComponent, StackCraftingStep, InteractUsingEvent>
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;

    protected override void CouldHandle(Entity<MetaDataComponent> ent, ref CraftingGraphCouldStepEvent<StackCraftingStep, InteractUsingEvent> args)
    {
        if (args.Event.Handled)
            return;

        if (!TryComp<StackComponent>(args.Event.Used, out var stack))
            return;

        args.Result = stack.StackTypeId == args.Step.Stack && stack.Count >= args.Step.Amount;
    }

    protected override void Handle(Entity<MetaDataComponent> ent, ref CraftingGraphStepEvent<StackCraftingStep, InteractUsingEvent> args)
    {
        if (args.Event.Handled)
            return;

        var doAfterEvent = new CraftingDoAfterEvent(args.Location);
        var doAfterArgs = new DoAfterArgs(EntityManager, args.Event.User, args.Step.Delay, doAfterEvent, args.Graph, ent, args.Event.Used)
        {
            BreakOnDamage = false,
            BreakOnMove = true,
            NeedHand = true,
            AttemptFrequency = AttemptFrequency.EveryTick,
        };

        args.Result.Handled = _doAfter.TryStartDoAfter(doAfterArgs);
    }

    protected override void HandleAttemptDoAfter(Entity<MetaDataComponent> ent, ref CraftingGraphStepAttemptDoAfterEvent<StackCraftingStep> args)
    {
        if (!TryComp<StackComponent>(args.Event.Event.Used, out var stack))
        {
            args.Event.Cancel();
            return;
        }

        if (stack.StackTypeId != args.Step.Stack || stack.Count < args.Step.Amount)
            args.Event.Cancel();
    }

    protected override void HandleDoAfter(Entity<MetaDataComponent> ent, ref CraftingGraphStepDoAfterEvent<StackCraftingStep> args)
    {
        if (args.Event.Cancelled)
            return;

        DebugTools.Assert(args.Event.Used is not null);
        if (args.Event.Used is not { } used)
            return;

        var split = _stack.Split(used, args.Step.Amount, Transform(args.Event.User).Coordinates);
        DebugTools.Assert(split is not null || _net.IsClient);
        if (split is not { } splitted)
            return;

        PredictedQueueDel(splitted);
    }
}
