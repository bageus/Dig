namespace Dig.Unity
{
    public sealed partial class DigGameHudCanvas
    {
        internal void ActivateResidentRosterForSelection()
        {
            SelectRightPanelTab(RightPanelTab.Residents);
        }

        internal void ActivateBuildingRosterForSelection()
        {
            SelectRightPanelTab(RightPanelTab.Buildings);
        }
    }
}
