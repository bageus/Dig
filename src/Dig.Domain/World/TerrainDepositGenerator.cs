using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Dig.Domain.World
{

public sealed class TerrainDepositGenerator
{
    private static readonly SpatialCellId[] NeighbourOffsets =
    {
        new SpatialCellId(-1, 0, 0),
        new SpatialCellId(1, 0, 0),
        new SpatialCellId(0, -1, 0),
        new SpatialCellId(0, 1, 0),
        new SpatialCellId(0, 0, -1),
        new SpatialCellId(0, 0, 1),
    };

    public IReadOnlyList<TerrainDepositInstance> Generate(
        int width,
        int height,
        int depth,
        IReadOnlyCollection<SpatialCellId> mineableCells,
        IReadOnlyList<TerrainDepositDefinition> definitions,
        TerrainDepositGenerationSettings settings)
    {
        ValidateDimensions(width, height, depth);
        if (mineableCells == null)
        {
            throw new ArgumentNullException(nameof(mineableCells));
        }

        if (definitions == null || definitions.Count == 0)
        {
            throw new ArgumentException(
                "At least one deposit definition is required.",
                nameof(definitions));
        }

        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        SpatialCellId[] ordered = mineableCells.Distinct().OrderBy(cell => cell).ToArray();
        HashSet<SpatialCellId> candidates = new HashSet<SpatialCellId>(ordered);
        for (int index = 0; index < ordered.Length; index++)
        {
            ValidateCell(ordered[index], width, height, depth);
        }

        int totalWeight = definitions.Sum(value => value.GenerationWeight);
        HashSet<SpatialCellId> assigned = new HashSet<SpatialCellId>();
        List<TerrainDepositInstance> result = new List<TerrainDepositInstance>();
        for (int index = 0; index < ordered.Length; index++)
        {
            SpatialCellId origin = ordered[index];
            if (assigned.Contains(origin)
                || Roll(settings, origin, salt: 1) % 1_000UL
                    >= (ulong)settings.DensityPermille)
            {
                continue;
            }

            TerrainDepositDefinition definition = SelectDefinition(
                definitions,
                totalWeight,
                Roll(settings, origin, salt: 2));
            int desiredSize = 1 + (int)(Roll(settings, origin, salt: 3)
                % (ulong)settings.MaximumClusterSize);
            GrowCluster(
                origin,
                desiredSize,
                definition,
                candidates,
                assigned,
                settings,
                result);
        }

        return new ReadOnlyCollection<TerrainDepositInstance>(result);
    }

    private static void GrowCluster(
        SpatialCellId origin,
        int desiredSize,
        TerrainDepositDefinition definition,
        ISet<SpatialCellId> candidates,
        ISet<SpatialCellId> assigned,
        TerrainDepositGenerationSettings settings,
        ICollection<TerrainDepositInstance> result)
    {
        Queue<SpatialCellId> frontier = new Queue<SpatialCellId>();
        frontier.Enqueue(origin);
        while (frontier.Count > 0 && desiredSize > 0)
        {
            SpatialCellId cell = frontier.Dequeue();
            if (!candidates.Contains(cell) || !assigned.Add(cell))
            {
                continue;
            }

            ulong identity = Roll(settings, cell, salt: 4);
            result.Add(new TerrainDepositInstance(
                $"deposit-instance-{identity:x16}",
                cell,
                definition,
                isRevealed: true,
                definition.MaximumYield,
                version: 1));
            desiredSize--;

            int start = (int)(Roll(settings, cell, salt: 5)
                % (ulong)NeighbourOffsets.Length);
            for (int offset = 0; offset < NeighbourOffsets.Length; offset++)
            {
                SpatialCellId delta = NeighbourOffsets[(start + offset)
                    % NeighbourOffsets.Length];
                frontier.Enqueue(new SpatialCellId(
                    cell.X + delta.X,
                    cell.Y + delta.Y,
                    cell.Z + delta.Z));
            }
        }
    }

    private static TerrainDepositDefinition SelectDefinition(
        IReadOnlyList<TerrainDepositDefinition> definitions,
        int totalWeight,
        ulong roll)
    {
        int selected = (int)(roll % (ulong)totalWeight);
        for (int index = 0; index < definitions.Count; index++)
        {
            TerrainDepositDefinition definition = definitions[index];
            if (selected < definition.GenerationWeight)
            {
                return definition;
            }

            selected -= definition.GenerationWeight;
        }

        return definitions[definitions.Count - 1];
    }

    private static ulong Roll(
        TerrainDepositGenerationSettings settings,
        SpatialCellId cell,
        int salt)
    {
        const ulong offset = 1469598103934665603UL;
        const ulong prime = 1099511628211UL;
        ulong hash = offset;
        Mix(ref hash, unchecked((uint)settings.Seed), prime);
        Mix(ref hash, (uint)settings.AlgorithmVersion, prime);
        Mix(ref hash, unchecked((uint)cell.X), prime);
        Mix(ref hash, unchecked((uint)cell.Y), prime);
        Mix(ref hash, unchecked((uint)cell.Z), prime);
        Mix(ref hash, (uint)salt, prime);
        return hash;
    }

    private static void Mix(ref ulong hash, uint value, ulong prime)
    {
        hash ^= value;
        hash *= prime;
    }

    private static void ValidateDimensions(int width, int height, int depth)
    {
        if (width <= 0 || height <= 0 || depth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }
    }

    private static void ValidateCell(
        SpatialCellId cell,
        int width,
        int height,
        int depth)
    {
        if (cell.X < 0 || cell.X >= width
            || cell.Y < 0 || cell.Y >= height
            || cell.Z < 0 || cell.Z >= depth)
        {
            throw new ArgumentOutOfRangeException(
                nameof(cell),
                $"Deposit candidate '{cell}' is outside the generation volume.");
        }
    }
}

}