using Content.Shared.Temperature.Components;
using Content.Shared.Temperature;

namespace Content.Shared.Crafting.Steps;

public sealed partial class TemperatureCraftingStep : CraftingGraphStepBase<TemperatureCraftingStep>
{
    [DataField]
    public float Min = float.NegativeInfinity;

    [DataField]
    public float Max = float.PositiveInfinity;
}

public sealed class TemperatureCraftingStepSystem : CraftingGraphStepSystem<TemperatureComponent, TemperatureCraftingStep, OnTemperatureChangeEvent>
{
    protected override void Handle(Entity<TemperatureComponent> ent, ref CraftingGraphStepEvent<TemperatureCraftingStep, OnTemperatureChangeEvent> args)
    {
        if (args.Step.Min <= args.Event.CurrentTemperature && args.Step.Max >= args.Event.CurrentTemperature)
            args.Result.Progress = true;
    }
}
