using System.Linq;
using Content.Shared.Interaction;
using Content.Shared.Tools.Systems;
using Content.Shared.Tools;
using Robust.Shared.Prototypes;

namespace Content.Shared.Crafting.Steps;

public sealed partial class ToolCraftingStep : CraftingGraphStepBase<ToolCraftingStep>
{
    [DataField(required: true)]
    public HashSet<ProtoId<ToolQualityPrototype>> Tools = default!;

    [DataField]
    public float Fuel = 10;

    [DataField]
    public TimeSpan Delay;
}

public sealed class ToolCraftingStepSystem : CraftingGraphStepSystem<MetaDataComponent, ToolCraftingStep, InteractUsingEvent>
{
    [Dependency] private readonly SharedToolSystem _tool = default!;

    protected override void CouldHandle(Entity<MetaDataComponent> ent, ref CraftingGraphCouldStepEvent<ToolCraftingStep, InteractUsingEvent> args)
    {
        if (args.Event.Handled)
            return;

        args.Result = _tool.HasAllQualities(args.Event.Used, args.Step.Tools.Select(it => it.ToString()));
    }

    protected override void Handle(Entity<MetaDataComponent> ent, ref CraftingGraphStepEvent<ToolCraftingStep, InteractUsingEvent> args)
    {
        if (args.Event.Handled)
            return;

        args.Result.Handled = _tool.UseTool(args.Event.Used, args.Event.User, ent, args.Step.Delay, args.Step.Tools.Select(it => it.ToString()), new CraftingDoAfterEvent(args.Location), out _, args.Step.Fuel);
        args.Event.Handled = args.Result.Handled;
    }
}
