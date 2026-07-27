using Dig.Application.Inventory;
using Dig.Domain.Core;
using Dig.Infrastructure.InMemory;

namespace Dig.Unity
{

internal sealed partial class DigTerrainWorkSession
{
    internal Result SettleWorldItems(long tick)
    {
        Dig.Domain.World.WorldSnapshot world = _worldSession.LoadSnapshot();
        Result terrain = SettleWorldItems(_inventoryRepository, world, tick);
        if (terrain.IsFailure || _buildingInventoryRepository == null)
        {
            return terrain;
        }

        return SettleWorldItems(_buildingInventoryRepository, world, tick);
    }

    private static Result SettleWorldItems(
        InMemoryInventoryRepository repository,
        Dig.Domain.World.WorldSnapshot world,
        long tick)
    {
        Result settled = WorldItemGravitySettlement.Settle(
            repository.Get(),
            world,
            tick);
        if (settled.IsSuccess)
        {
            repository.Save(repository.Get());
        }

        return settled;
    }
}

}
