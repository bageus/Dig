using System;
using System.Collections.Generic;
using Dig.Domain.Runtime;
using Dig.Domain.World;

namespace Dig.Domain.Generation
{

internal static partial class WorldGenerationLayout
{
    private static void CarveRoom(
        GenerationCellBuffer buffer,
        CellId center,
        int radius,
        MaterialId emptyMaterialId,
        bool explored)
    {
        for (int y = center.Y - radius; y <= center.Y + radius; y++)
        {
            for (int x = center.X - radius; x <= center.X + radius; x++)
            {
                int dx = x - center.X;
                int dy = y - center.Y;
                CellId cellId = new CellId(x, y);
                if ((dx * dx) + (dy * dy) <= radius * radius && buffer.Contains(cellId))
                {
                    CarveCell(buffer, cellId, emptyMaterialId, explored);
                }
            }
        }
    }

    private static void CarveCorridor(
        GenerationCellBuffer buffer,
        CellId from,
        CellId to,
        MaterialId emptyMaterialId,
        int halfWidth,
        bool horizontalFirst)
    {
        CellId corner = horizontalFirst
            ? new CellId(to.X, from.Y)
            : new CellId(from.X, to.Y);
        CarveLine(buffer, from, corner, emptyMaterialId, halfWidth);
        CarveLine(buffer, corner, to, emptyMaterialId, halfWidth);
    }

    private static void CarveLine(
        GenerationCellBuffer buffer,
        CellId from,
        CellId to,
        MaterialId emptyMaterialId,
        int halfWidth)
    {
        int stepX = Math.Sign(to.X - from.X);
        int stepY = Math.Sign(to.Y - from.Y);
        CellId current = from;
        while (true)
        {
            for (int offsetY = -halfWidth; offsetY <= halfWidth; offsetY++)
            {
                for (int offsetX = -halfWidth; offsetX <= halfWidth; offsetX++)
                {
                    CellId cellId = new CellId(current.X + offsetX, current.Y + offsetY);
                    if (buffer.Contains(cellId))
                    {
                        CarveCell(buffer, cellId, emptyMaterialId, explored: false);
                    }
                }
            }

            if (current == to)
            {
                break;
            }

            current = new CellId(current.X + stepX, current.Y + stepY);
        }
    }

    private static void CarveCell(
        GenerationCellBuffer buffer,
        CellId cellId,
        MaterialId emptyMaterialId,
        bool explored)
    {
        CellState current = buffer.Get(cellId);
        buffer.Set(cellId, new CellState(
            emptyMaterialId,
            CellDesignation.None,
            current.IsExplored || explored,
            damage: 0,
            current.Temperature));
    }

    private static List<CellId> CollectSolidRing(
        GenerationCellBuffer buffer,
        CellId center,
        int minimumDistance,
        int maximumDistance,
        MaterialId emptyMaterialId)
    {
        List<CellId> cells = new List<CellId>();
        for (int y = center.Y - maximumDistance; y <= center.Y + maximumDistance; y++)
        {
            for (int x = center.X - maximumDistance; x <= center.X + maximumDistance; x++)
            {
                CellId cellId = new CellId(x, y);
                int distance = ManhattanDistance(cellId, center);
                if (buffer.Contains(cellId)
                    && distance >= minimumDistance
                    && distance <= maximumDistance
                    && buffer.Get(cellId).MaterialId != emptyMaterialId)
                {
                    cells.Add(cellId);
                }
            }
        }

        cells.Sort();
        return cells;
    }

    private static void SetResource(
        GenerationCellBuffer buffer,
        CellId cellId,
        MaterialId resourceMaterialId)
    {
        CellState current = buffer.Get(cellId);
        buffer.Set(cellId, new CellState(
            resourceMaterialId,
            CellDesignation.None,
            current.IsExplored,
            damage: 0,
            current.Temperature));
    }

    private static void Shuffle<T>(List<T> values, DeterministicRandomStream stream)
    {
        for (int index = values.Count - 1; index > 0; index--)
        {
            int swapIndex = stream.NextInt(index + 1);
            T value = values[index];
            values[index] = values[swapIndex];
            values[swapIndex] = value;
        }
    }

    private static int ManhattanDistance(CellId left, CellId right)
    {
        return Math.Abs(left.X - right.X) + Math.Abs(left.Y - right.Y);
    }

    private static int SquaredDistance(CellId left, CellId right)
    {
        int dx = left.X - right.X;
        int dy = left.Y - right.Y;
        return (dx * dx) + (dy * dy);
    }
}

}
