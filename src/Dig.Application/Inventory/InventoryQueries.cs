using Dig.Application.Messaging;
using Dig.Domain.Inventory;
using Dig.Domain.Storage;

namespace Dig.Application.Inventory;

public sealed class FindAvailableItemsHandler
    : IQueryHandler<FindAvailableItemsQuery, IReadOnlyList<ItemStackSnapshot>>
{
    private readonly IInventoryRepository _repository;

    public FindAvailableItemsHandler(IInventoryRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public IReadOnlyList<ItemStackSnapshot> Handle(FindAvailableItemsQuery query)
    {
        if (query is null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        return _repository.Get().FindAvailable(query.ItemId);
    }
}

public sealed class FindStorageDestinationsHandler
    : IQueryHandler<FindStorageDestinationsQuery, IReadOnlyList<StorageZoneSnapshot>>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IStorageRepository _storageRepository;

    public FindStorageDestinationsHandler(
        IInventoryRepository inventoryRepository,
        IStorageRepository storageRepository)
    {
        _inventoryRepository = inventoryRepository
            ?? throw new ArgumentNullException(nameof(inventoryRepository));
        _storageRepository = storageRepository
            ?? throw new ArgumentNullException(nameof(storageRepository));
    }

    public IReadOnlyList<StorageZoneSnapshot> Handle(FindStorageDestinationsQuery query)
    {
        if (query is null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        InventoryState inventory = _inventoryRepository.Get();
        ItemDefinition item = inventory.Catalog.Get(query.ItemId);
        InventorySnapshot snapshot = inventory.CreateSnapshot();
        return _storageRepository.Get().FindDestinations(
            item,
            query.Quantity,
            zoneId => snapshot.Stacks
                .Where(stack => stack.Location == ItemLocation.InStorage(zoneId))
                .Sum(stack => stack.Quantity));
    }
}
