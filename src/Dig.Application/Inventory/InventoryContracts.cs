using Dig.Application.Messaging;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Storage;

namespace Dig.Application.Inventory;

public interface IInventoryRepository
{
    InventoryState Get();

    void Save(InventoryState inventory);
}

public interface IStorageRepository
{
    StorageState Get();

    void Save(StorageState storage);
}

public sealed class CreateHaulingJobCommand : ICommand<Result>
{
    public CreateHaulingJobCommand(
        EntityId jobId,
        EntityId sourceStackId,
        int quantity,
        EntityId destinationStorageId,
        int priority,
        long tick)
    {
        JobId = jobId;
        SourceStackId = sourceStackId;
        Quantity = quantity;
        DestinationStorageId = destinationStorageId;
        Priority = priority;
        Tick = tick;
    }

    public EntityId JobId { get; }

    public EntityId SourceStackId { get; }

    public int Quantity { get; }

    public EntityId DestinationStorageId { get; }

    public int Priority { get; }

    public long Tick { get; }
}

public sealed class CancelHaulingJobCommand : ICommand<Result>
{
    public CancelHaulingJobCommand(EntityId jobId, string reason, long tick)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Cancellation reason is required.", nameof(reason));
        }

        JobId = jobId;
        Reason = reason.Trim();
        Tick = tick;
    }

    public EntityId JobId { get; }

    public string Reason { get; }

    public long Tick { get; }
}

public sealed class CompleteHaulingJobCommand : ICommand<Result>
{
    public CompleteHaulingJobCommand(EntityId jobId, EntityId splitStackId, long tick)
    {
        JobId = jobId;
        SplitStackId = splitStackId;
        Tick = tick;
    }

    public EntityId JobId { get; }

    public EntityId SplitStackId { get; }

    public long Tick { get; }
}

public sealed class FindAvailableItemsQuery : IQuery<IReadOnlyList<ItemStackSnapshot>>
{
    public FindAvailableItemsQuery(ItemId itemId)
    {
        ItemId = itemId;
    }

    public ItemId ItemId { get; }
}

public sealed class FindStorageDestinationsQuery
    : IQuery<IReadOnlyList<StorageZoneSnapshot>>
{
    public FindStorageDestinationsQuery(ItemId itemId, int quantity)
    {
        ItemId = itemId;
        Quantity = quantity;
    }

    public ItemId ItemId { get; }

    public int Quantity { get; }
}

public static class HaulingErrors
{
    public static readonly DomainError JobNotHauling = new DomainError(
        "hauling.job_not_hauling",
        "The requested job is not a hauling job.");

    public static readonly DomainError InvalidStage = new DomainError(
        "hauling.invalid_stage",
        "The hauling job is not ready to deposit its reserved items.");
}
