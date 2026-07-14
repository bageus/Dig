using Dig.Application.Inventory;
using Dig.Domain.Inventory;
using Dig.Domain.Storage;

namespace Dig.Infrastructure.InMemory;

public sealed class InMemoryInventoryRepository : IInventoryRepository
{
    private InventoryState _inventory;

    public InMemoryInventoryRepository(InventoryState inventory)
    {
        _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
    }

    public InventoryState Get()
    {
        return _inventory;
    }

    public void Save(InventoryState inventory)
    {
        _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
    }
}

public sealed class InMemoryStorageRepository : IStorageRepository
{
    private StorageState _storage;

    public InMemoryStorageRepository(StorageState? storage = null)
    {
        _storage = storage ?? new StorageState();
    }

    public StorageState Get()
    {
        return _storage;
    }

    public void Save(StorageState storage)
    {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
    }
}
