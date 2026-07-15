using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Buildings;
using Dig.Domain.Core;

namespace Dig.Presentation.Buildings
{

public enum BuildingFunctionActionKind
{
    Pack = 0,
}

public sealed class BuildingFunctionActionViewModel
{
    public BuildingFunctionActionViewModel(
        BuildingFunctionActionKind kind,
        string labelKey,
        bool isEnabled,
        string? disabledReasonCode)
    {
        if (string.IsNullOrWhiteSpace(labelKey))
        {
            throw new ArgumentException("Action label key is required.", nameof(labelKey));
        }

        if (!isEnabled && string.IsNullOrWhiteSpace(disabledReasonCode))
        {
            throw new ArgumentException(
                "A disabled action requires a reason code.",
                nameof(disabledReasonCode));
        }

        Kind = kind;
        LabelKey = labelKey.Trim();
        IsEnabled = isEnabled;
        DisabledReasonCode = disabledReasonCode;
    }

    public BuildingFunctionActionKind Kind { get; }

    public string LabelKey { get; }

    public bool IsEnabled { get; }

    public string? DisabledReasonCode { get; }
}

public sealed class BuildingFunctionsViewModel
{
    public BuildingFunctionsViewModel(
        EntityId buildingId,
        BuildingDefinitionId definitionId,
        BuildingStatus status,
        int durability,
        int maximumDurability,
        bool isPacking,
        int packingCompletedWork,
        int packingRequiredWork,
        IReadOnlyCollection<BuildingFunctionActionViewModel> actions)
    {
        if (buildingId.IsEmpty || definitionId.IsEmpty)
        {
            throw new ArgumentException("Building and definition ids are required.");
        }

        if (durability < 0
            || maximumDurability <= 0
            || durability > maximumDurability
            || packingCompletedWork < 0
            || packingRequiredWork < 0
            || packingCompletedWork > packingRequiredWork)
        {
            throw new ArgumentOutOfRangeException(nameof(durability));
        }

        if (actions is null)
        {
            throw new ArgumentNullException(nameof(actions));
        }

        BuildingId = buildingId;
        DefinitionId = definitionId;
        Status = status;
        Durability = durability;
        MaximumDurability = maximumDurability;
        IsPacking = isPacking;
        PackingCompletedWork = packingCompletedWork;
        PackingRequiredWork = packingRequiredWork;
        Actions = new ReadOnlyCollection<BuildingFunctionActionViewModel>(
            actions.OrderBy(action => action.Kind).ToArray());
    }

    public EntityId BuildingId { get; }

    public BuildingDefinitionId DefinitionId { get; }

    public BuildingStatus Status { get; }

    public int Durability { get; }

    public int MaximumDurability { get; }

    public bool IsPacking { get; }

    public int PackingCompletedWork { get; }

    public int PackingRequiredWork { get; }

    public IReadOnlyList<BuildingFunctionActionViewModel> Actions { get; }

    public double PackingProgress => PackingRequiredWork <= 0
        ? 0d
        : Math.Min(1d, (double)PackingCompletedWork / PackingRequiredWork);
}

public sealed class BuildingPackingCommandDraft
{
    public BuildingPackingCommandDraft(
        EntityId buildingId,
        EntityId jobId,
        EntityId outputStackId,
        int priority,
        long tick)
    {
        if (buildingId.IsEmpty || jobId.IsEmpty || outputStackId.IsEmpty)
        {
            throw new ArgumentException("Building, job and output stack ids are required.");
        }

        if (priority < 0 || priority > 1000)
        {
            throw new ArgumentOutOfRangeException(nameof(priority));
        }

        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        BuildingId = buildingId;
        JobId = jobId;
        OutputStackId = outputStackId;
        Priority = priority;
        Tick = tick;
    }

    public EntityId BuildingId { get; }

    public EntityId JobId { get; }

    public EntityId OutputStackId { get; }

    public int Priority { get; }

    public long Tick { get; }
}
}
