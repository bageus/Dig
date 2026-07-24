using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Inventory;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;

namespace Dig.Unity
{

internal sealed partial class DigTerrainWorkSession
{
    internal Result SettleWorldItems(long tick)
    {
        WorldSnapshot world = _worldSession.LoadSnapshot();
        Dictionary<CellId, bool> solid = world.Chunks
            .SelectMany(chunk => chunk.Cells)
            .ToDictionary(cell => cell.Id, cell => cell.IsSolid);

        Result terrain = SettleWorldItems(
            _inventoryRepository,
            world.Size.Height,
            solid,
            tick);
        if (terrain.IsFailure || _buildingInventoryRepository == null)
        {
            return terrain;
        }

        return SettleWorldItems(
            _buildingInventoryRepository,
            world.Size.Height,
            solid,
            tick);
    }

    private static Result SettleWorldItems(
        InMemoryInventoryRepository repository,
        int worldHeight,
        IReadOnlyDictionary<CellId, bool> solid,
        long tick)
    {
        InventorySnapshot snapshot = repository.Get().CreateSnapshot();
        for (int index = 0; index < snapshot.Stacks.Count; index++)
        {
            ItemStackSnapshot stack = snapshot.Stacks[index];
            if (stack.Location.Kind != ItemLocationKind.World
                || !stack.Location.HasCell
                || stack.AvailableQuantity != stack.Quantity
                || stack.HeldQuantity != 0)
            {
                continue;
            }

            CellId source = stack.Location.CellId;
            CellId landing = WorldItemGravityPolicy.ResolveLandingCell(
                source,
                worldHeight,
                cell => !solid.TryGetValue(cell, out bool isSolid) || isSolid);
            if (landing == source)
            {
                continue;
            }

            Result moved = repository.Get().MoveAvailable(
                stack.StackId,
                stack.Quantity,
                ItemLocation.InWorld(landing),
                splitStackId: default,
                tick: tick);
            if (moved.IsFailure)
            {
                return moved;
            }
        }

        return Result.Success();
    }
}

}
