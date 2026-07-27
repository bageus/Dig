using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;

namespace Dig.Application.Inventory
{

public static class WorldItemGravitySettlement
{
    public static Result Settle(
        InventoryState inventory,
        WorldSnapshot world,
        long tick)
    {
        if (inventory == null)
        {
            throw new ArgumentNullException(nameof(inventory));
        }

        if (world == null)
        {
            throw new ArgumentNullException(nameof(world));
        }

        Dictionary<CellId, bool> solid = world.Chunks
            .SelectMany(chunk => chunk.Cells)
            .ToDictionary(cell => cell.Id, cell => cell.IsSolid);
        InventorySnapshot snapshot = inventory.CreateSnapshot();
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
                world.Size.Height,
                cell => !solid.TryGetValue(cell, out bool isSolid) || isSolid);
            if (landing == source)
            {
                continue;
            }

            Result moved = inventory.MoveAvailable(
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
