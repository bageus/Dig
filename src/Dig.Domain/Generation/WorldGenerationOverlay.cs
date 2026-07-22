using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Domain.Generation
{

public readonly struct WorldCellOverride
{
    public WorldCellOverride(CellId cellId, CellState state)
    {
        CellId = cellId;
        State = state;
    }

    public CellId CellId { get; }

    public CellState State { get; }
}

public static class WorldGenerationOverlay
{
    public static Result<IReadOnlyList<WorldCellOverride>> Capture(
        WorldState generatedBase,
        WorldState currentWorld)
    {
        if (generatedBase is null)
        {
            throw new ArgumentNullException(nameof(generatedBase));
        }

        if (currentWorld is null)
        {
            throw new ArgumentNullException(nameof(currentWorld));
        }

        if (!AreCompatible(generatedBase, currentWorld))
        {
            return Result<IReadOnlyList<WorldCellOverride>>.Failure(
                WorldGenerationErrors.IncompatibleOverlayWorld);
        }

        List<WorldCellOverride> overrides = new List<WorldCellOverride>();
        for (int y = 0; y < generatedBase.Size.Height; y++)
        {
            for (int x = 0; x < generatedBase.Size.Width; x++)
            {
                CellId cellId = new CellId(x, y, 0);
                CellState baseState = generatedBase.GetCell(cellId).Value.State;
                CellState currentState = currentWorld.GetCell(cellId).Value.State;
                if (baseState != currentState)
                {
                    overrides.Add(new WorldCellOverride(cellId, currentState));
                }
            }
        }

        return Result<IReadOnlyList<WorldCellOverride>>.Success(
            new ReadOnlyCollection<WorldCellOverride>(overrides));
    }

    public static Result<WorldMutationResult> Apply(
        WorldState generatedBase,
        IEnumerable<WorldCellOverride> overrides,
        long tick)
    {
        if (generatedBase is null)
        {
            throw new ArgumentNullException(nameof(generatedBase));
        }

        if (overrides is null)
        {
            throw new ArgumentNullException(nameof(overrides));
        }

        List<TerrainChange> changes = new List<TerrainChange>();
        foreach (WorldCellOverride item in overrides)
        {
            changes.Add(new TerrainChange(item.CellId, item.State));
        }

        return generatedBase.ApplyTerrainChanges(changes, tick);
    }

    private static bool AreCompatible(WorldState left, WorldState right)
    {
        if (left.Size.Width != right.Size.Width
            || left.Size.Height != right.Size.Height
            || left.Layout.ChunkSize != right.Layout.ChunkSize
            || left.Materials.Definitions.Count != right.Materials.Definitions.Count)
        {
            return false;
        }

        for (int index = 0; index < left.Materials.Definitions.Count; index++)
        {
            MaterialDefinition leftMaterial = left.Materials.Definitions[index];
            MaterialDefinition rightMaterial = right.Materials.Definitions[index];
            if (leftMaterial.Id != rightMaterial.Id
                || leftMaterial.IsSolid != rightMaterial.IsSolid
                || leftMaterial.Hardness != rightMaterial.Hardness)
            {
                return false;
            }
        }

        return true;
    }
}

}
