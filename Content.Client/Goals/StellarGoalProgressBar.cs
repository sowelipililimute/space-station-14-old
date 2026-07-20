using Content.Client.UserInterface.Systems;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Goals;

public sealed partial class StellarGoalProgressBar : BoxContainer
{
    [Dependency] private IEntityManager _entity = default!;
    private readonly ProgressColorSystem _progressColor;

    private readonly PanelContainer _filledProgress = new()
    {
        StyleClasses = { "BackgroundPanel" },
        HorizontalExpand = true,
        VerticalExpand = true,
        MinSize = new(6, 6),
    };

    private readonly PanelContainer _unfilledProgress = new()
    {
        HorizontalExpand = true,
        VerticalExpand = true,
        MinSize = new(6, 6),
    };

    public StellarGoalProgressBar()
    {
        IoCManager.InjectDependencies(this);

        AddChild(_filledProgress);
        AddChild(_unfilledProgress);

        _progressColor = _entity.System<ProgressColorSystem>();
    }

    public void SetProgress(float progress)
    {
        if (progress <= 0)
        {
            _filledProgress.Visible = false;
            _unfilledProgress.Visible = true;
        }
        else if (progress >= 1)
        {
            _filledProgress.Visible = true;
            _unfilledProgress.Visible = false;
        }
        else
        {
            _filledProgress.SizeFlagsStretchRatio = progress;
            _unfilledProgress.SizeFlagsStretchRatio = 1f - progress;
        }

        _filledProgress.ModulateSelfOverride = _progressColor.GetProgressColor(progress);
    }
}
