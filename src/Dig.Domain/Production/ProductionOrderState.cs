using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;

namespace Dig.Domain.Production;

internal sealed class ProductionOrderState
{
    private ItemReservationAllocation[] _inputAllocations =
        Array.Empty<ItemReservationAllocation>();

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

    public void Start()
    {
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
            Reason);
    }

    private void IncrementVersion()
    {
        Version = checked(Version + 1);
    }
}
