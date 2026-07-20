using System;
using Dig.Domain.Inventory;

namespace Dig.Domain.World
{

public sealed class TerrainDepositDefinition
{
    public TerrainDepositDefinition(
        string id,
        string displayName,
        ItemId outputItemId,
        int maximumYield,
        int generationWeight)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("A stable deposit id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("A display name is required.", nameof(displayName));
        }

        if (maximumYield <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumYield));
        }

        if (generationWeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(generationWeight));
        }

        Id = id;
        DisplayName = displayName;
        OutputItemId = outputItemId;
        MaximumYield = maximumYield;
        GenerationWeight = generationWeight;
    }

    public string Id { get; }

    public string DisplayName { get; }

    public ItemId OutputItemId { get; }

    public int MaximumYield { get; }

    public int GenerationWeight { get; }
}

public sealed class TerrainDepositInstance
{
    public TerrainDepositInstance(
        string instanceId,
        SpatialCellId cell,
        TerrainDepositDefinition definition,
        bool isRevealed,
        int remainingYield,
        long version)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
        {
            throw new ArgumentException("A stable instance id is required.", nameof(instanceId));
        }

        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        if (remainingYield < 0 || remainingYield > definition.MaximumYield)
        {
            throw new ArgumentOutOfRangeException(nameof(remainingYield));
        }

        if (version < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version));
        }

        InstanceId = instanceId;
        Cell = cell;
        IsRevealed = isRevealed;
        RemainingYield = remainingYield;
        Version = version;
    }

    public string InstanceId { get; }

    public SpatialCellId Cell { get; }

    public TerrainDepositDefinition Definition { get; }

    public bool IsRevealed { get; }

    public int RemainingYield { get; }

    public long Version { get; }

    public bool IsDepleted => RemainingYield == 0;

    public TerrainDepositInstance Deplete(long version)
    {
        if (version < Version)
        {
            throw new ArgumentOutOfRangeException(nameof(version));
        }

        return new TerrainDepositInstance(
            InstanceId,
            Cell,
            Definition,
            IsRevealed,
            remainingYield: 0,
            version);
    }
}

public sealed class TerrainDepositGenerationSettings
{
    public TerrainDepositGenerationSettings(
        int seed,
        int algorithmVersion,
        int densityPermille,
        int maximumClusterSize)
    {
        if (algorithmVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(algorithmVersion));
        }

        if (densityPermille < 0 || densityPermille > 1_000)
        {
            throw new ArgumentOutOfRangeException(nameof(densityPermille));
        }

        if (maximumClusterSize < 1 || maximumClusterSize > 4)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumClusterSize));
        }

        Seed = seed;
        AlgorithmVersion = algorithmVersion;
        DensityPermille = densityPermille;
        MaximumClusterSize = maximumClusterSize;
    }

    public int Seed { get; }

    public int AlgorithmVersion { get; }

    public int DensityPermille { get; }

    public int MaximumClusterSize { get; }
}

}