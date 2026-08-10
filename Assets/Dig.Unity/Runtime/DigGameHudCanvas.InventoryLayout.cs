using System;
using Dig.Domain.Inventory;
using Dig.Presentation.Inventory;
using UnityEngine;
using UnityEngine.UI;

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

    internal static void ConfigureInventoryGrid(
        GridLayoutGroup grid,
        int columns,
        float cellWidth)
    {
        if (grid == null)
        {
            throw new ArgumentNullException(nameof(grid));
        }

        if (columns <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(columns));
        }

        grid.padding = new RectOffset(0, 0, 0, 0);
        grid.cellSize = new Vector2(cellWidth, InventoryCellHeight);
        grid.spacing = new Vector2(InventoryCellSpacing, InventoryCellSpacing);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Vertical;
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint = GridLayoutGroup.Constraint.FixedRowCount;
        grid.constraintCount = InventoryRows;
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
