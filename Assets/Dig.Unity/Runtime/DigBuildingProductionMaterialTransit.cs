using System;
using System.Linq;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Production;
using Dig.Domain.World;

namespace Dig.Unity
{

internal sealed partial class DigTerrainWorkSession
{
    private static bool TryResolveCurrentProductionMaterialStep(
        ProductionOrderSnapshot order,
        out ProductionMaterialStepSnapshot step)
    {
        for (int index = 0; index < order.MaterialSteps.Count; index++)
        {
            if (!order.MaterialSteps[index].Consumed)
            {
                step = order.MaterialSteps[index];
                return true;
            }
        }

        step = default;
        return false;
    }

    private bool HasCarriedProductionMaterial(
        EntityId orderId,
        EntityId residentId,
        ItemId itemId)
    {
        return _buildingInventoryRepository!.Get().CreateSnapshot().Stacks.Any(stack =>
            stack.ItemId == itemId
            && stack.Location.Kind == ItemLocationKind.AgentInventory
            && stack.Location.HasOwner
            && stack.Location.OwnerId == residentId
            && stack.Reservations.Any(value =>
                value.JobId == orderId && value.Quantity > 0));
    }

    private static CellId ResolveBuildingInternalStockCell(BuildingSnapshot building)
    {
        CellId row = building.Footprint
            .OrderBy(value => Math.Abs(value.Y - building.Origin.Y))
            .ThenBy(value => Math.Abs(value.Z - building.Origin.Z))
            .ThenBy(value => value.Y)
            .ThenBy(value => value.Z)
            .First();
        return new CellId(
            building.Footprint.Min(value => value.X) - 1,
            row.Y,
            row.Z);
    }


}

}
