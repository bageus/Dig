namespace Dig.Unity
{
    public sealed partial class DigHudOverlay
    {
        internal void ActivateResidentRosterForSelection()
        {
            _gameHudCanvas?.ActivateResidentRosterForSelection();
        }

        internal void ActivateBuildingRosterForSelection()
        {
            _gameHudCanvas?.ActivateBuildingRosterForSelection();
        }
    }
}
