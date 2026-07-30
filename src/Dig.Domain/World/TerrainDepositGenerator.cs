using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Dig.Domain.World
{

public sealed class TerrainDepositGenerationResult
{
    public TerrainDepositGenerationResult(
        int algorithmVersion,
        IEnumerable<TerrainDepositInstance> deposits)
    {
        if (algorithmVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(algorithmVersion));
        }

        if (deposits is null)
        {
            throw new ArgumentNullException(nameof(deposits));
        }

        AlgorithmVersion = algorithmVersion;
        Deposits = new ReadOnlyCollection<TerrainDepositInstance>(
            deposits.OrderBy(value => value.Cell).ToArray());
    }

    public int AlgorithmVersion { get; }

    public IReadOnlyList<TerrainDepositInstance> Deposits { get; }
}

public sealed class TerrainDepositGenerator
{
    private const string OriginStream = "deposits.origin";
    private const string DefinitionStream = "deposits.definition";
    private const string ClusterSizeStream = "deposits.cluster_size";
    private const string InstanceStream = "deposits.instance";
    private const string NeighbourStream = "deposits.neighbour_order";

    private static readonly CellId[] NeighbourOffsets =
    {
        new CellId(-1, 0, 0),
        new CellId(1, 0, 0),
        new CellId(0, -1, 0),
        new CellId(0, 1, 0),
        new CellId(0, 0, -1),
        new CellId(0, 0, 1),
    };

    public TerrainDepositGenerationResult Generate(
        WorldSize size,
        IReadOnlyCollection<TerrainDepositHostCell> hostCells,
        TerrainDepositCatalog catalog,
        TerrainDepositGenerationSettings settings)
    {
        if (hostCells is null)
        {
            throw new ArgumentNullException(nameof(hostCells));
        }

        if (catalog is null)
        {
            throw new ArgumentNullException(nameof(catalog));
        }

        if (settings is null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        Dictionary<CellId, TerrainDepositHostCell> candidates =
            new Dictionary<CellId, TerrainDepositHostCell>();
        foreach (TerrainDepositHostCell candidate in hostCells)
        {
            if (candidate is null)
            {
                throw new ArgumentException(
                    "Deposit host cells cannot contain null values.",
                    nameof(hostCells));
            }

            if (!size.Contains(candidate.Cell))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(hostCells),
                    $"Deposit candidate '{candidate.Cell}' is outside the generation volume.");
            }

            if (!candidate.Material.IsSolid || !candidate.Material.IsMineable)
            {
                throw new ArgumentException(
                    $"Deposit candidate '{candidate.Cell}' is not a mineable solid host.",
                    nameof(hostCells));
            }

            if (!candidates.TryAdd(candidate.Cell, candidate))
            {
                throw new ArgumentException(
                    $"Deposit candidate '{candidate.Cell}' is duplicated.",
                    nameof(hostCells));
            }
        }

        CellId[] ordered = candidates.Keys.OrderBy(cell => cell).ToArray();
        HashSet<CellId> assigned = new HashSet<CellId>();
        HashSet<string> instanceIds = new HashSet<string>(StringComparer.Ordinal);
        List<TerrainDepositInstance> result = new List<TerrainDepositInstance>();
        for (int index = 0; index < ordered.Length; index++)
        {
            CellId origin = ordered[index];
            if (assigned.Contains(origin)
                || Roll(settings, origin, OriginStream) % 1_000UL
                    >= (ulong)settings.DensityPermille)
            {
                continue;
            }

            TerrainDepositDefinition[] compatible = catalog.Definitions
                .Where(value => value.CanOccupy(candidates[origin].Material))
                .ToArray();
            if (compatible.Length == 0)
            {
                continue;
            }

            TerrainDepositDefinition definition = SelectDefinition(
                compatible,
                Roll(settings, origin, DefinitionStream));
            int desiredSize = 1 + (int)(Roll(
                settings,
                origin,
                ClusterSizeStream) % (ulong)settings.MaximumClusterSize);
            GrowCluster(
                origin,
                desiredSize,
                definition,
                candidates,
                assigned,
                instanceIds,
                settings,
                result);
        }

        return new TerrainDepositGenerationResult(settings.AlgorithmVersion, result);
    }

    private static void GrowCluster(
        CellId origin,
        int desiredSize,
        TerrainDepositDefinition definition,
        IReadOnlyDictionary<CellId, TerrainDepositHostCell> candidates,
        ISet<CellId> assigned,
        ISet<string> instanceIds,
        TerrainDepositGenerationSettings settings,
        ICollection<TerrainDepositInstance> result)
    {
        Queue<CellId> frontier = new Queue<CellId>();
        HashSet<CellId> queued = new HashSet<CellId>();
        frontier.Enqueue(origin);
        queued.Add(origin);
        while (frontier.Count > 0 && desiredSize > 0)
        {
            CellId cell = frontier.Dequeue();
            if (!candidates.TryGetValue(cell, out TerrainDepositHostCell? candidate)
                || candidate == null
                || assigned.Contains(cell)
                || !definition.CanOccupy(candidate.Material))
            {
                continue;
            }

            assigned.Add(cell);
            string instanceId = CreateInstanceId(settings, cell);
            if (!instanceIds.Add(instanceId))
            {
                throw new InvalidOperationException(
                    $"Deterministic deposit identity collision at '{cell}'.");
            }

            result.Add(new TerrainDepositInstance(
                instanceId,
                cell,
                definition,
                isRevealed: false,
                definition.MaximumYield,
                version: 1));
            desiredSize--;

            int start = (int)(Roll(settings, cell, NeighbourStream)
                % (ulong)NeighbourOffsets.Length);
            for (int offset = 0; offset < NeighbourOffsets.Length; offset++)
            {
                CellId delta = NeighbourOffsets[(start + offset)
                    % NeighbourOffsets.Length];
                CellId neighbour = new CellId(
                    cell.X + delta.X,
                    cell.Y + delta.Y,
                    cell.Z + delta.Z);
                if (queued.Add(neighbour))
                {
                    frontier.Enqueue(neighbour);
                }
            }
        }
    }

    private static TerrainDepositDefinition SelectDefinition(
        IReadOnlyList<TerrainDepositDefinition> definitions,
        ulong roll)
    {
        int totalWeight = 0;
        for (int index = 0; index < definitions.Count; index++)
        {
            totalWeight = checked(totalWeight + definitions[index].GenerationWeight);
        }

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

    private static string CreateInstanceId(
        TerrainDepositGenerationSettings settings,
        CellId cell)
    {
        ulong identity = Roll(settings, cell, InstanceStream);
        return $"deposit-instance-v{settings.AlgorithmVersion}-{identity:x16}";
    }

    private static ulong Roll(
        TerrainDepositGenerationSettings settings,
        CellId cell,
        string streamName)
    {
        const ulong offset = 1469598103934665603UL;
        const ulong prime = 1099511628211UL;
        ulong hash = offset;
        Mix(ref hash, unchecked((uint)settings.Seed), prime);
        Mix(ref hash, (uint)settings.AlgorithmVersion, prime);
        for (int index = 0; index < streamName.Length; index++)
        {
            Mix(ref hash, streamName[index], prime);
        }

        Mix(ref hash, unchecked((uint)cell.X), prime);
        Mix(ref hash, unchecked((uint)cell.Y), prime);
        Mix(ref hash, unchecked((uint)cell.Z), prime);
        return hash;
    }

    private static void Mix(ref ulong hash, uint value, ulong prime)
    {
        hash ^= value;
        hash *= prime;
    }
}

}
