using System;
using Dig.Domain.Inventory;
using UnityEngine;

namespace Dig.Unity
{
    internal static class DigAgentEquipmentVisualExtensions
    {
        internal static void Configure(
            this DigAgentEquipmentVisual visual,
            string itemId,
            Material material)
        {
            if (visual == null)
            {
                throw new ArgumentNullException(nameof(visual));
            }

            EquipmentAppearanceKind appearance = EquipmentAppearanceKind.Generic;
            if (itemId.EndsWith("pickaxe", StringComparison.OrdinalIgnoreCase))
            {
                appearance = EquipmentAppearanceKind.Mining;
            }
            else if (itemId.EndsWith("hammer", StringComparison.OrdinalIgnoreCase))
            {
                appearance = EquipmentAppearanceKind.Construction;
            }

            visual.Configure(itemId, appearance, material);
        }
    }
}
