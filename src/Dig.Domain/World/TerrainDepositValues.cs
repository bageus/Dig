using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Agents;
using Dig.Domain.Content;
using Dig.Domain.Inventory;

namespace Dig.Domain.World
{

public sealed class TerrainDepositDefinition
{
    private readonly IReadOnlyList<MaterialId> _allowedHostMaterialIds;
    private readonly HashSet<MaterialId> _allowedHostMaterials;

    public TerrainDepositDefinition(
        string id,
        string displayName,
        ItemId outputItemId,
        int maximumYield,
        int generationWeight,
        SkillGrantProfile? skillGrantProfile = null,
        int version = 1,
        int workEffortPermille = 1_000,
        IEnumerable<MaterialId>? allowedHostMaterialIds = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("A stable deposit id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("A display name is required.", nameof(displayName));
        }

        if (outputItemId.IsEmpty)
        {
            throw new ArgumentException("A deposit output item id is required.", nameof(outputItemId));
        }

        if (maximumYield <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumYield));
        }

        if (generationWeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(generationWeight));
        }

        if (version <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version));
        }

        if (workEffortPermille < 1_000)
        {
            throw new ArgumentOutOfRangeException(
                nameof(workEffortPermille),
                "Deposit extraction cannot be easier than ordinary host terrain.");
        }

        MaterialId[] hosts = (allowedHostMaterialIds ?? Array.Empty<MaterialId>())
            .OrderBy(value => value)
            .ToArray();
        if (hosts.Any(value => value.IsEmpty)
            || hosts.Distinct().Count() != hosts.Length)
        {
            throw new ArgumentException(
                "Allowed host material ids must be non-empty and unique.",
                nameof(allowedHostMaterialIds));
        }

        Id = id.Trim();
        DisplayName = displayName.Trim();
        OutputItemId = outputItemId;
        MaximumYield = maximumYield;
        GenerationWeight = generationWeight;
        Version = version;
        WorkEffortPermille = workEffortPermille;
        SkillGrantProfile = skillGrantProfile
            ?? DefaultSkillProgressionContent.Catalog.GetProfile(
                DefaultSkillGrantProfileIds.StoneExtraction);
        _allowedHostMaterialIds = new ReadOnlyCollection<MaterialId>(hosts);
        _allowedHostMaterials = new HashSet<MaterialId>(hosts);
    }

    public string Id { get; }

    public string DisplayName { get; }

    public ItemId OutputItemId { get; }

    public int MaximumYield { get; }

    public int GenerationWeight { get; }

    public int Version { get; }

    public int WorkEffortPermille { get; }

    public SkillGrantProfile SkillGrantProfile { get; }

    public IReadOnlyList<MaterialId> AllowedHostMaterialIds => _allowedHostMaterialIds;

    public bool CanOccupy(MaterialDefinition host)
    {
        if (host is null)
        {
            throw new ArgumentNullException(nameof(host));
        }

        return host.IsSolid
            && host.IsMineable
            && (_allowedHostMaterials.Count == 0
                || _allowedHostMaterials.Contains(host.Id));
    }
}

public sealed class TerrainDepositCatalog
{
    private readonly Dictionary<string, TerrainDepositDefinition> _definitions;
    private readonly IReadOnlyList<TerrainDepositDefinition> _orderedDefinitions;

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

        TerrainDepositDefinition[] ordered = values
            .OrderBy(value => value.Id, StringComparer.Ordinal)
            .ToArray();
        _definitions = ordered.ToDictionary(
            value => value.Id,
            StringComparer.Ordinal);
        _orderedDefinitions = new ReadOnlyCollection<TerrainDepositDefinition>(ordered);
    }

    public IReadOnlyList<TerrainDepositDefinition> Definitions => _orderedDefinitions;

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

        InstanceId = instanceId.Trim();
        Cell = cell;
        IsRevealed = isRevealed;
        RemainingYield = remainingYield;
        Version = version;
    }

    public string InstanceId { get; }

    public CellId Cell { get; }

    public TerrainDepositDefinition Definition { get; }

    public int DefinitionVersion => Definition.Version;

    public bool IsRevealed { get; }

    public int RemainingYield { get; }

    public long Version { get; }

    public bool IsDepleted => RemainingYield == 0;

    public TerrainDepositInstance Reveal(long version)
    {
        if (version < Version)
        {
            throw new ArgumentOutOfRangeException(nameof(version));
        }

        if (IsRevealed)
        {
            return this;
        }

        return new TerrainDepositInstance(
            InstanceId,
            Cell,
            Definition,
            isRevealed: true,
            remainingYield: RemainingYield,
            version: version);
    }

    public TerrainDepositInstance Deplete(long version)
    {
        if (version < Version)
        {
            throw new ArgumentOutOfRangeException(nameof(version));
        }

        if (IsDepleted)
        {
            return this;
        }

        return new TerrainDepositInstance(
            InstanceId,
            Cell,
            Definition,
            isRevealed: IsRevealed,
            remainingYield: 0,
            version: version);
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
