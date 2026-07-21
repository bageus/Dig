namespace Dig.Unity
{

public sealed partial class DigWorldInteraction
{
    internal DigExcavationDrawingMode ExcavationDrawingMode => _excavationMode;

    internal bool CanSelectExcavationCells =>
        _excavationMode == DigExcavationDrawingMode.Tunnel
        && CanActivateExcavationDrawing
        && !_caveRoomPreset.HasValue;

    internal void EnsureDefaultExcavationDrawingMode()
    {
        if (_excavationMode != DigExcavationDrawingMode.None
            || _caveRoomPreset.HasValue
            || !CanActivateExcavationDrawing)
        {
            return;
        }

        _excavationMode = DigExcavationDrawingMode.Tunnel;
        SetTunnelDigInteractionActive(active: true);
        ResetExcavationStroke();
        _hud?.SetStatus(
            "Tunnel tool active. Hold LMB and drag horizontally or vertically.");
    }
}

}
