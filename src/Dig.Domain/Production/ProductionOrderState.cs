using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;

namespace Dig.Domain.Production
{

internal sealed class ProductionOrderState
{
    private ItemReservationAllocation[] _inputAllocations =
        Array.Empty<ItemReservationAllocation>();
    private long[] _resolvedStepDurations = Array.Empty<long>();
    private long _currentStepWork;
    private int _currentStepIndex;

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


    public ProductionMaterialWorkResult PreviewMaterialWork(long elapsedTicks)
    {
        if (!Recipe.UsesMaterialSteps)
        {
            throw new InvalidOperationException("Recipe does not use material steps.");
        }

        if (elapsedTicks <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsedTicks));
        }

        return ResolveMaterialWork(
            elapsedTicks,
            _currentStepIndex,
            _currentStepWork,
            out _,
            out _);
    }

    public ProductionMaterialWorkResult AddMaterialWork(long elapsedTicks)
    {
        ProductionMaterialWorkResult result = ResolveMaterialWork(
            elapsedTicks,
            _currentStepIndex,
            _currentStepWork,
            out int nextStepIndex,
            out long nextStepWork);
        _currentStepIndex = nextStepIndex;
        _currentStepWork = nextStepWork;
        CompletedWork = _currentStepIndex;
        if (_currentStepIndex == _resolvedStepDurations.Length)
        {
            Status = ProductionOrderStatus.ReadyToComplete;
        }

        IncrementVersion();
        return result;
    }

    private ProductionMaterialWorkResult ResolveMaterialWork(
        long elapsedTicks,
        int stepIndex,
        long stepWork,
        out int nextStepIndex,
        out long nextStepWork)
    {
        if (!Recipe.UsesMaterialSteps)
        {
            throw new InvalidOperationException("Recipe does not use material steps.");
        }

        if (elapsedTicks <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsedTicks));
        }

        List<ItemId> consumed = new List<ItemId>();
        long remaining = elapsedTicks;
        while (remaining > 0 && stepIndex < _resolvedStepDurations.Length)
        {
            long required = _resolvedStepDurations[stepIndex];
            long applied = Math.Min(remaining, required - stepWork);
            stepWork += applied;
            remaining -= applied;
            if (stepWork != required)
            {
                break;
            }

            consumed.Add(Recipe.MaterialSteps[stepIndex].ItemId);
            stepIndex++;
            stepWork = 0;
        }

        nextStepIndex = stepIndex;
        nextStepWork = stepWork;
        return new ProductionMaterialWorkResult(
            consumed,
            stepIndex == _resolvedStepDurations.Length);
    }

    private IReadOnlyList<ProductionMaterialStepSnapshot> CreateMaterialSteps()
    {
        if (!Recipe.UsesMaterialSteps)
        {
            return Array.Empty<ProductionMaterialStepSnapshot>();
        }

        List<ProductionMaterialStepSnapshot> values =
            new List<ProductionMaterialStepSnapshot>(Recipe.MaterialSteps.Count);
        for (int index = 0; index < Recipe.MaterialSteps.Count; index++)
        {
            long required = index < _resolvedStepDurations.Length
                ? _resolvedStepDurations[index]
                : 0;
            long completed = index < _currentStepIndex
                ? required
                : index == _currentStepIndex ? _currentStepWork : 0;
            values.Add(new ProductionMaterialStepSnapshot(
                index,
                Recipe.MaterialSteps[index].ItemId,
                required,
                completed,
                index < _currentStepIndex));
        }

        return values;
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
