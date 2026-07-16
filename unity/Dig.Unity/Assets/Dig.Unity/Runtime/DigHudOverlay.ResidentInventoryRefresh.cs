using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigHudOverlay
    {
        private void LateUpdate()
        {
            if (_selectedAgent == null)
            {
                _residentInventory = null;
                return;
            }

            if (_buildingSession != null)
            {
                _residentInventory = _buildingSession.LoadResidentInventory(
                    _selectedAgent.Id);
            }

            if (_residentInventoryInteraction == null)
            {
                _residentInventoryInteraction = FindObjectOfType<DigWorldInteraction>();
            }
        }
    }
}
