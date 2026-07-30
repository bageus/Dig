using System;
using Dig.Domain.Inventory;
using Dig.Presentation.Inventory;
using UnityEngine;

namespace Dig.Unity
{

public sealed partial class DigGameHudCanvas
{
    internal static Vector2Int ResolveInventoryGrid(int slotCount)
    {
        if (slotCount <= 0 || slotCount % InventoryRows != 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(slotCount),
                "Inventory compartments require paired cells in exactly two rows.");
        }

        return new Vector2Int(slotCount / InventoryRows, InventoryRows);
    }

    private float ResolveInventoryCellWidth(
        ResidentInventoryLayoutViewModel inventory)
    {
        int sectionCount = 0;
        int totalColumns = 0;
        int interColumnGaps = 0;
        foreach (ResidentInventoryCompartment compartment in new[]
        {
            ResidentInventoryCompartment.Weapon,
            ResidentInventoryCompartment.Main,
            ResidentInventoryCompartment.Cargo,
        })
        {
            int count = inventory.GetCompartment(compartment).Count;
            if (count == 0)
            {
                continue;
            }

            int columns = ResolveInventoryGrid(count).x;
            sectionCount++;
            totalColumns += columns;
            interColumnGaps += columns - 1;
        }

        float availableWidth = _bottomContent == null
            ? 0f
            : _bottomContent.rect.width;
        if (availableWidth <= 0f || totalColumns == 0)
        {
            return InventoryCellWidth;
        }

        float reservedWidth = ((sectionCount - 1) * 8f)
            + (sectionCount * 4f)
            + (interColumnGaps * InventoryCellSpacing);
        return Mathf.Clamp(
            (availableWidth - reservedWidth) / totalColumns,
            28f,
            InventoryCellWidth);
    }


}

}
