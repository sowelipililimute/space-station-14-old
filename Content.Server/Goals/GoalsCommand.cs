using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Goals;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;

namespace Content.Server.Goals;

[ToolshedCommand, AdminCommand(AdminFlags.Debug)]
public sealed class GoalsCommand : ToolshedCommand
{
    [CommandImplementation("get")]
    public IEnumerable<EntityUid> Get([PipedArgument] EntityUid target)
    {
        foreach (var goal in Sys<StellarGoalsSystem>().GetGoals(target))
        {
            yield return goal;
        }
    }

    [CommandImplementation("progress")]
    public double Progress([PipedArgument] EntityUid goal)
    {
        if (!EntityManager.TryGetComponent<StellarGoalComponent>(goal, out var goalComp))
            return double.NaN;

        return goalComp.Progress;
    }

    [CommandImplementation("individual")]
    public EntityUid? Individual([PipedArgument] EntityUid target)
    {
        return Sys<StellarGoalsSystem>().GetIndividualGoalContainer(target);
    }

    [CommandImplementation("add")]
    public EntityUid? Add([PipedArgument] EntityUid target, EntProtoId protoId)
    {
        Sys<StellarGoalsSystem>().TryAddGoal(target, protoId, out var goal);
        return goal?.Owner;
    }

    [CommandImplementation("create_container")]
    public EntityUid CreateContainer(string name)
    {
        return Sys<StellarGoalsSystem>().SpawnContainer(name);
    }

    [CommandImplementation("observe")]
    public void Observe([PipedArgument] EntityUid observer, EntityUid container)
    {
        Sys<StellarGoalsSystem>().ObserveContainer(observer, container);
    }

    [CommandImplementation("unobserve")]
    public void Unobserve([PipedArgument] EntityUid observer, EntityUid container)
    {
        Sys<StellarGoalsSystem>().UnobserveContainer(observer, container);
    }
}
