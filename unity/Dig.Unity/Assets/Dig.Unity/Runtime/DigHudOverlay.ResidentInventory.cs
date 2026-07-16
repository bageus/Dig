using System;
using Dig.Presentation.Inventory;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigHudOverlay
    {
        private ResidentInventoryViewModel? _residentInventory;
        private DigWorldInteraction? _residentInventoryInteraction;

        internal void SetResidentInventoryControls(DigWorldInteraction interaction)
        {
            _residentInventoryInteraction = interaction
                ?? throw new ArgumentNullException(nameof(interaction));
        }

        internal void SetResidentInventory(ResidentInventoryViewModel? inventory)
        {
            if (inventory != null
                && _selectedAgent != null
                && !string.Equals(
                    inventory.ResidentId,
                    _selectedAgent.Id,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Resident inventory does not match the selected resident.",
                    nameof(inventory));
            }

            _residentInventory = inventory;
        }

        private void DrawResidentInventory()
        {
            if (_selectedAgent == null)
            {
                return;
            }

            DrawResidentWorkRates();
            GUILayout.Space(6f);
            GUILayout.Label("INVENTORY");
            if (_residentInventory == null
                || !string.Equals(
                    _residentInventory.ResidentId,
                    _selectedAgent.Id,
                    StringComparison.Ordinal))
            {
                GUILayout.Label("Inventory snapshot unavailable.");
                return;
            }

            GUILayout.Label($"Version: {_residentInventory.InventoryVersion}");
            if (_residentInventory.Slots.Count == 0)
            {
                GUILayout.Label("Empty");
                return;
            }

            foreach (ResidentInventorySlotViewModel slot in _residentInventory.Slots)
            {
                GUILayout.BeginHorizontal();
                string reservation = slot.ReservedQuantity == 0
                    ? "free"
                    : $"reserved {slot.ReservedQuantity}";
                string location = slot.IsEquipped ? "equipped" : "carried";
                GUILayout.Label(
                    $"{slot.ItemId} ×{slot.Quantity} | {location} | "
                    + $"available {slot.AvailableQuantity} | {reservation}",
                    GUILayout.Width(330f));

                bool previousEnabled = GUI.enabled;
                if (slot.IsBuildingBox)
                {
                    GUI.enabled = previousEnabled && slot.CanStartPlacement;
                    if (GUILayout.Button("Place", GUILayout.Width(60f)))
                    {
                        _residentInventoryInteraction?.ActivateResidentInventorySlot(slot);
                    }
                }

                if (slot.IsTool && !slot.IsEquipped)
                {
                    GUI.enabled = previousEnabled && slot.CanUse;
                    if (GUILayout.Button("Use", GUILayout.Width(60f)))
                    {
                        _residentInventoryInteraction?.UseResidentInventorySlot(slot);
                    }
                }

                GUI.enabled = previousEnabled && slot.CanDrop;
                if (GUILayout.Button("Put down", GUILayout.Width(78f)))
                {
                    _residentInventoryInteraction?.DropResidentInventorySlot(slot);
                }

                GUI.enabled = previousEnabled;
                GUILayout.EndHorizontal();
            }
        }
    }
}