using Dig.Presentation.Agents;

namespace Dig.Unity
{

public sealed partial class DigAgentVisual
{
    private ResidentMovementModeViewModel? _movementMode;

    internal ResidentMovementModeViewModel? MovementMode => _movementMode;

    internal void ApplyMovementMode(
        ResidentMovementModeViewModel? movementMode,
        bool movementStarted)
    {
        _movementMode = movementMode;
        if (!movementStarted || movementMode == null)
        {
            return;
        }

        _duration *= (float)movementMode.TransitionDurationMultiplier;
        _rig?.ApplyAction(_visualPresenter.PresentAction(
            Model,
            isMoving: true,
            isCarrying: movementMode.IsCarrying));
    }
}

}
