using System;
using System.Linq;
using Dig.Application.Agents;
using Dig.Domain.Content;
using Dig.Domain.Inventory;

namespace Dig.Unity
{

internal sealed partial class DigTerrainWorkSession
{
    private readonly ResidentMovementModeResolver _movementModeResolver =
        new ResidentMovementModeResolver(ResidentMovementModePolicy.CreateDefault());

    internal ResidentMovementModeResolution ResolveResidentMovementMode(
        ResidentMovementRuntimeRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        InventoryState inventory = _inventoryRepository.Get();
        InventorySnapshot snapshot = inventory.CreateSnapshot();
        bool carriesBuildingBox = snapshot.Stacks.Any(stack =>
            IsOwnedByResident(stack.Location, request.ResidentId)
            && inventory.Catalog.Get(stack.ItemId).HasCategory(
                CampfireBuildingBoxContent.BuildingBoxCategoryId));

        // Personal mobility item stable IDs and numeric multipliers remain Q-014.
        // The resolver already supports both item kinds; runtime activation stays
        // false until those data definitions are added to the authoritative catalog.
        return _movementModeResolver.Resolve(new ResidentMovementModeRequest(
            request.ResidentId,
            request.Alertness,
            request.ActiveIntent,
            request.CommandSource,
            request.TraversalKind,
            request.RepeatedManualCommand,
            request.RemainingPathSteps,
            inventory.GetResidentMoveSpeedMultiplier(request.ResidentId),
            carriesBuildingBox,
            hasRideHamster: false,
            hasHoverboard: false));
    }

    private static bool IsOwnedByResident(
        ItemLocation location,
        Dig.Domain.Core.EntityId residentId)
    {
        return location.HasOwner
            && location.OwnerId == residentId
            && (location.Kind == ItemLocationKind.AgentInventory
                || location.Kind == ItemLocationKind.Equipped);
    }
}

}
