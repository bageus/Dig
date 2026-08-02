using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;

namespace Dig.Domain.Production
{

internal sealed partial class ProductionOrderState
{
    private ItemReservationAllocation[] _inputAllocations =
        Array.Empty<ItemReservationAllocation>();
    private long[] _resolvedStepDurations = Array.Empty<long>();
    private long _currentStepWork;
    private int _currentStepIndex;
    private ProductionMaterialStepPhase _currentStepPhase =
        ProductionMaterialStepPhase.AwaitingMaterial;

    public ProductionOrderState(
        EntityId id,
        RecipeDefinition recipe,
        EntityId buildingId,
        long sequence)
    {
        Id = id;
        Recipe = recipe;
        BuildingId = buildingId;
        Sequence = sequence;
        Status = ProductionOrderStatus.Queued;
    }

    public EntityId Id { get; }
    public RecipeDefinition Recipe { get; }
    public EntityId BuildingId { get; }
    public long Sequence { get; }
    public ProductionOrderStatus Status { get; private set; }
    public int CompletedWork { get; private set; }
    public long Version { get; private set; }
    public string? Reason { get; private set; }

    public void ReserveInputs(IReadOnlyCollection<ItemReservationAllocation> allocations)
    {
        _inputAllocations = allocations
            .OrderBy(value => value.StackId.ToString(), StringComparer.Ordinal)
            .ToArray();
        Status = ProductionOrderStatus.InputsReserved;
        Reason = null;
        IncrementVersion();
    }

    public void Start(IReadOnlyCollection<long>? resolvedStepDurations = null)
    {
        if (Recipe.UsesMaterialSteps)
        {
            _resolvedStepDurations = (resolvedStepDurations
                    ?? throw new ArgumentNullException(nameof(resolvedStepDurations)))
                .ToArray();
            if (_resolvedStepDurations.Length != Recipe.MaterialSteps.Count
                || _resolvedStepDurations.Any(value => value <= 0))
            {
                throw new ArgumentException(
                    "Resolved material durations must match recipe steps.",
                    nameof(resolvedStepDurations));
            }

            _currentStepIndex = 0;
            _currentStepWork = 0;
            _currentStepPhase = ProductionMaterialStepPhase.AwaitingMaterial;
            CompletedWork = 0;
        }

        Status = ProductionOrderStatus.InProgress;
        Reason = null;
        IncrementVersion();
    }

    public void AddWork(int effectiveWork)
    {
        CompletedWork = Math.Min(
            Recipe.RequiredWork,
            checked(CompletedWork + effectiveWork));
        if (CompletedWork == Recipe.RequiredWork)
        {
            Status = ProductionOrderStatus.ReadyToComplete;
        }

        IncrementVersion();
    }

    public void ResetForRetry(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Reset reason is required.", nameof(reason));
        }

        _inputAllocations = Array.Empty<ItemReservationAllocation>();
        _resolvedStepDurations = Array.Empty<long>();
        _currentStepWork = 0;
        _currentStepIndex = 0;
        _currentStepPhase = ProductionMaterialStepPhase.AwaitingMaterial;
        CompletedWork = 0;
        Status = ProductionOrderStatus.Queued;
        Reason = reason.Trim();
        IncrementVersion();
    }

    public void Complete()
    {
        Status = ProductionOrderStatus.Completed;
        Reason = null;
        IncrementVersion();
    }

    public void Cancel(string reason)
    {
        Status = ProductionOrderStatus.Cancelled;
        Reason = reason;
        IncrementVersion();
    }

    public void Fail(string reason)
    {
        Status = ProductionOrderStatus.Failed;
        Reason = reason;
        IncrementVersion();
    }

    public ProductionOrderSnapshot CreateSnapshot()
    {
        return new ProductionOrderSnapshot(
            Id,
            Recipe,
            BuildingId,
            Sequence,
            Status,
            CompletedWork,
            Version,
            _inputAllocations,
            Reason,
            CreateMaterialSteps());
    }

    private void IncrementVersion()
    {
        Version = checked(Version + 1);
    }
}
}
