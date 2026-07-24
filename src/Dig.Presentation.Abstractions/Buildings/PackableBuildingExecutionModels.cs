using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Application.Buildings;
using Dig.Domain.Buildings;

namespace Dig.Presentation.Buildings
{

public sealed class PackableBuildingVisualProfile
{
    public PackableBuildingVisualProfile(
        BuildingDefinitionId definitionId,
        string activeBuildingVisualId,
        string worldBoxVisualId,
        string inventoryBoxVisualId,
        string plannedSiteVisualId,
        string partialUnpackVisualId,
        string partialPackVisualId,
        string iterationEffectId)
    {
        if (definitionId.IsEmpty)
        {
            throw new ArgumentException("Building definition id is required.", nameof(definitionId));
        }

        DefinitionId = definitionId;
        ActiveBuildingVisualId = Normalize(activeBuildingVisualId, nameof(activeBuildingVisualId));
        WorldBoxVisualId = Normalize(worldBoxVisualId, nameof(worldBoxVisualId));
        InventoryBoxVisualId = Normalize(inventoryBoxVisualId, nameof(inventoryBoxVisualId));
        PlannedSiteVisualId = Normalize(plannedSiteVisualId, nameof(plannedSiteVisualId));
        PartialUnpackVisualId = Normalize(partialUnpackVisualId, nameof(partialUnpackVisualId));
        PartialPackVisualId = Normalize(partialPackVisualId, nameof(partialPackVisualId));
        IterationEffectId = Normalize(iterationEffectId, nameof(iterationEffectId));
    }

    public BuildingDefinitionId DefinitionId { get; }
    public string ActiveBuildingVisualId { get; }
    public string WorldBoxVisualId { get; }
    public string InventoryBoxVisualId { get; }
    public string PlannedSiteVisualId { get; }
    public string PartialUnpackVisualId { get; }
    public string PartialPackVisualId { get; }
    public string IterationEffectId { get; }

    private static string Normalize(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Presentation catalog ids are required.", parameterName);
        }

        return value.Trim();
    }
}

public sealed class PackableBuildingVisualCatalog
{
    private readonly IReadOnlyDictionary<BuildingDefinitionId, PackableBuildingVisualProfile>
        _profiles;

    public PackableBuildingVisualCatalog(
        IEnumerable<PackableBuildingVisualProfile> profiles)
    {
        if (profiles is null)
        {
            throw new ArgumentNullException(nameof(profiles));
        }

        PackableBuildingVisualProfile[] values = profiles
            .OrderBy(value => value.DefinitionId)
            .ToArray();
        if (values.Length == 0
            || values.Select(value => value.DefinitionId).Distinct().Count() != values.Length)
        {
            throw new ArgumentException(
                "Packable building visual profiles must have unique definition ids.",
                nameof(profiles));
        }

        _profiles = new ReadOnlyDictionary<BuildingDefinitionId, PackableBuildingVisualProfile>(
            values.ToDictionary(value => value.DefinitionId));
    }

    public PackableBuildingVisualProfile Get(BuildingDefinitionId definitionId)
    {
        return _profiles.TryGetValue(definitionId, out PackableBuildingVisualProfile? profile)
            ? profile
            : throw new KeyNotFoundException(
                $"Unknown packable building presentation profile '{definitionId}'.");
    }
}

public sealed class PackableBuildingExecutionViewModel
{
    public PackableBuildingExecutionViewModel(
        string operationId,
        string packageId,
        string definitionId,
        string boxItemId,
        PackableBuildingOperationKind operation,
        PackableBuildingExecutionStatus status,
        int completedIterations,
        int totalIterations,
        string? activeWorkerId,
        int elapsedIterationSeconds,
        int iterationDurationSeconds,
        string visualId,
        string iterationEffectId,
        string statusLabelKey)
    {
        if (string.IsNullOrWhiteSpace(operationId)
            || string.IsNullOrWhiteSpace(packageId)
            || string.IsNullOrWhiteSpace(definitionId)
            || string.IsNullOrWhiteSpace(boxItemId)
            || string.IsNullOrWhiteSpace(visualId)
            || string.IsNullOrWhiteSpace(iterationEffectId)
            || string.IsNullOrWhiteSpace(statusLabelKey))
        {
            throw new ArgumentException("Packable building presentation values are required.");
        }

        if (completedIterations < 0
            || totalIterations <= 0
            || completedIterations > totalIterations
            || elapsedIterationSeconds < 0
            || iterationDurationSeconds < 0
            || elapsedIterationSeconds > iterationDurationSeconds)
        {
            throw new ArgumentOutOfRangeException(nameof(completedIterations));
        }

        OperationId = operationId.Trim();
        PackageId = packageId.Trim();
        DefinitionId = definitionId.Trim();
        BoxItemId = boxItemId.Trim();
        Operation = operation;
        Status = status;
        CompletedIterations = completedIterations;
        TotalIterations = totalIterations;
        ActiveWorkerId = string.IsNullOrWhiteSpace(activeWorkerId)
            ? null
            : activeWorkerId.Trim();
        ElapsedIterationSeconds = elapsedIterationSeconds;
        IterationDurationSeconds = iterationDurationSeconds;
        VisualId = visualId.Trim();
        IterationEffectId = iterationEffectId.Trim();
        StatusLabelKey = statusLabelKey.Trim();
    }

    public string OperationId { get; }
    public string PackageId { get; }
    public string DefinitionId { get; }
    public string BoxItemId { get; }
    public PackableBuildingOperationKind Operation { get; }
    public PackableBuildingExecutionStatus Status { get; }
    public int CompletedIterations { get; }
    public int TotalIterations { get; }
    public int CurrentIteration => Math.Min(TotalIterations, CompletedIterations + 1);
    public string? ActiveWorkerId { get; }
    public int ElapsedIterationSeconds { get; }
    public int IterationDurationSeconds { get; }
    public int IterationProgressBasisPoints => IterationDurationSeconds == 0
        ? 0
        : (int)((long)ElapsedIterationSeconds * 10_000 / IterationDurationSeconds);
    public string VisualId { get; }
    public string IterationEffectId { get; }
    public string StatusLabelKey { get; }
    public bool IsIterationActive => IterationDurationSeconds > 0;
    public bool IsInterrupted => Status == PackableBuildingExecutionStatus.Interrupted;
    public bool IsTerminal => Status == PackableBuildingExecutionStatus.Completed
        || Status == PackableBuildingExecutionStatus.Cancelled;
}

}