using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Agents;
using Dig.Domain.Content;
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
        int generationWeight,
        SkillGrantProfile? skillGrantProfile = null)
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
        SkillGrantProfile = skillGrantProfile
            ?? DefaultSkillProgressionContent.Catalog.GetProfile(
                DefaultSkillGrantProfileIds.StoneExtraction);
    }

    public string Id { get; }

    public string DisplayName { get; }

    public ItemId OutputItemId { get; }

    public int MaximumYield { get; }

    public int GenerationWeight { get; }

    public SkillGrantProfile SkillGrantProfile { get; }
}


public sealed class TerrainDepositCatalog
{
    private readonly Dictionary<string, TerrainDepositDefinition> _definitions;

    public TerrainDepositCatalog(IEnumerable<TerrainDepositDefinition> definitions)
    {
        if (definitions is null)
        {
            throw new ArgumentNullException(nameof(definitions));
        }

        TerrainDepositDefinition[] values = definitions.ToArray();
        if (values.Length == 0
            || values.Any(value => value is null)
            || values.Select(value => value.Id)
                .Distinct(StringComparer.Ordinal)
                .Count() != values.Length)
        {
            throw new ArgumentException(
                "Deposit definitions must be non-empty and unique.",
                nameof(definitions));
        }

        _definitions = values.ToDictionary(
            value => value.Id,
            StringComparer.Ordinal);
    }

    public TerrainDepositDefinition? Get(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        return _definitions.TryGetValue(id, out TerrainDepositDefinition? value)
            ? value
            : null;
    }
}

public sealed class TerrainDepositInstance
{
    public TerrainDepositInstance(
        string instanceId,
        CellId cell,
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

    public CellId Cell { get; }

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
