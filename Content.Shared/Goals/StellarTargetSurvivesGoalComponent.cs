using Robust.Shared.GameStates;

namespace Content.Shared.Goals;

[RegisterComponent, NetworkedComponent]
[Access(typeof(StellarTargetSurvivesGoalSystem))]
public sealed partial class StellarTargetSurvivesGoalComponent : Component;

[RegisterComponent]
[Access(typeof(StellarTargetSurvivesGoalSystem))]
public sealed partial class StellarTargetedSurvivesComponent : StellarTargetedComponent;
