using System;
using System.Collections.Generic;
using Dig.Domain.World;

namespace Dig.Domain.Generation
{

internal sealed class GeneratedZonePlan
{
    public GeneratedZonePlan(
        int index,
        CellId center,
        WorldGenerationBiomeDefinition biome,
        int layerIndex)
    {
        Index = index;
        Center = center;
        Biome = biome ?? throw new ArgumentNullException(nameof(biome));
        LayerIndex = layerIndex;
    }

    public int Index { get; }

    public CellId Center { get; }

    public WorldGenerationBiomeDefinition Biome { get; }

    public int LayerIndex { get; }
}

internal sealed class GenerationCellBuffer
{
    private readonly CellState[] _cells;

    public GenerationCellBuffer(WorldSize size, CellState[] cells)
    {
        Size = size;
        _cells = cells ?? throw new ArgumentNullException(nameof(cells));
    }

    public WorldSize Size { get; }

    public IReadOnlyList<CellState> Cells => _cells;

    public CellState Get(CellId cellId)
    {
        return _cells[GetIndex(cellId)];
    }

    public void Set(CellId cellId, CellState state)
    {
        _cells[GetIndex(cellId)] = state;
    }

    public bool Contains(CellId cellId)
    {
        return Size.Contains(cellId);
    }

    private int GetIndex(CellId cellId)
    {
        if (!Size.Contains(cellId))
        {
            throw new ArgumentOutOfRangeException(nameof(cellId));
        }

        return checked((cellId.Y * Size.Width) + cellId.X);
    }
}

}
