using JetBrains.Annotations;
using Robust.Shared.Random;

namespace Content.Shared.Goals;

public sealed partial class StellarNumericGoalSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private StellarGoalsSystem _goals = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StellarNumericGoalComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<StellarNumericGoalComponent, StellarGetGoalProgressEvent>(OnGetProgress);
    }

    private void OnMapInit(Entity<StellarNumericGoalComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.TargetRange is not { } range)
        {
            UpdateMetaData(ent);
            return;
        }

        var span = range.Max - range.Min;
        var rangeElement = (int)Math.Ceiling(span / ent.Comp.TargetRangeResolution);
        var amount = _random.Next(0, rangeElement + 1);

        ent.Comp.Target = Math.Clamp(range.Min + amount * ent.Comp.TargetRangeResolution, range.Min, range.Max);
        Dirty(ent);

        UpdateMetaData(ent);
    }

    private void OnGetProgress(Entity<StellarNumericGoalComponent> ent, ref StellarGetGoalProgressEvent args)
    {
        args.Progress = ent.Comp.Current / ent.Comp.Target;
    }

    private void UpdateMetaData(Entity<StellarNumericGoalComponent> ent)
    {
        if (ent.Comp.Title is { } title)
            _metaData.SetEntityName(ent, Loc.GetString(title, ("target", ent.Comp.Target)));

        if (ent.Comp.Description is { } description)
            _metaData.SetEntityDescription(ent, Loc.GetString(description, ("target", ent.Comp.Target)));
    }

    /// <summary>
    /// Sets the current value of the goal, and recomputes progress
    /// </summary>
    /// <param name="ent">The goal to change the current value of</param>
    /// <param name="current">The new current value</param>
    [PublicAPI]
    public void SetCurrent(Entity<StellarNumericGoalComponent?> ent, double current)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        var clamped = Math.Max(current, 0d);
        if (MathHelper.CloseTo(clamped, ent.Comp.Current))
            return;

        ent.Comp.Current = clamped;
        Dirty(ent);

        _goals.RefreshProgress(ent.Owner);
    }

    /// <summary>
    /// Changes the current value of the goal by adding <see cref="delta"/> to it, and recomputes progress
    /// </summary>
    /// <param name="ent">The goal to change the current value of</param>
    /// <param name="delta">The amount to change the current value by</param>
    [PublicAPI]
    public void ChangeCurrent(Entity<StellarNumericGoalComponent?> ent, double delta)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        SetCurrent(ent, ent.Comp.Current + delta);
    }
}
