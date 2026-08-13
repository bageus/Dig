using System.Linq;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Dig.Presentation.Agents;
using Dig.Presentation.Inventory;
using NUnit.Framework;

namespace Dig.Unity.Tests
{

public sealed class ForcedPickupReplacementPlayModeTests
{
    [Test]
    public void Pickup_and_drop_atomically_transfer_between_world_and_resident_inventory()
    {
        ResidentNeedsRuntimePlayModeHarness.Runtime runtime =
            ResidentNeedsRuntimePlayModeHarness.CreateRuntime();
        string residentId = runtime.Residents.LoadView().First().Id;
        EntityId resident = EntityId.Parse(residentId);
        CellId source = runtime.Residents.Repository.Get(resident)!.Position;
        InMemoryInventoryRepository inventoryRepository =
            ResidentNeedsRuntimePlayModeHarness.GetField<InMemoryInventoryRepository>(
                runtime.Terrain,
                "_inventoryRepository");
        EntityId stackId = EntityId.Parse("fa000000000000000000000000000005");
        InventoryState inventory = inventoryRepository.Get();
        Require(inventory.AddUnit(
            stackId,
            CampfireProductionContent.StoneItemId,
            ItemLocation.InWorld(source),
            tick: 0));
        inventoryRepository.Save(inventory);

        Require(runtime.Terrain.CreateWorldItemPickup(
            stackId.ToString(),
            residentId,
            source,
            tick: 1));
        ResidentNeedsRuntimePlayModeHarness.RunTick(runtime);

        ItemStackSnapshot carried = inventoryRepository.Get().GetStack(stackId)!;
        Assert.That(carried.Location.Kind, Is.EqualTo(ItemLocationKind.AgentInventory));
        Assert.That(carried.Location.OwnerId, Is.EqualTo(resident));
        Assert.That(runtime.Terrain.LoadAllWorldItems()
            .Any(value => value.StackId == stackId.ToString()), Is.False);
        Assert.That(runtime.Terrain.LoadResidentInventoryLayout(residentId).Slots
            .Any(value => value.StackId == stackId.ToString()), Is.True);

        CellId destination = source;
        Require(runtime.Terrain.DropResidentInventoryStack(
            residentId,
            stackId.ToString(),
            destination,
            tick: 3));

        ItemStackSnapshot dropped = inventoryRepository.Get().GetStack(stackId)!;
        Assert.That(dropped.Location, Is.EqualTo(ItemLocation.InWorld(destination)));
        Assert.That(runtime.Terrain.LoadAllWorldItems()
            .Any(value => value.StackId == stackId.ToString()), Is.True);
        Assert.That(runtime.Terrain.LoadResidentInventoryLayout(residentId).Slots
            .Any(value => value.StackId == stackId.ToString()), Is.False);
    }

    [Test]
    public void Pickup_acquires_from_same_horizontal_cell_without_exact_surface_pose()
    {
        ResidentNeedsRuntimePlayModeHarness.Runtime runtime =
            ResidentNeedsRuntimePlayModeHarness.CreateRuntime();
        string residentId = runtime.Residents.LoadView().First().Id;
        EntityId resident = EntityId.Parse(residentId);
        CellId source = runtime.Residents.Repository.Get(resident)!.Position;
        Require(runtime.Residents.Repository.Get(resident)!.RestoreSurfacePose(
            new SurfacePose(source, SurfaceFace.Floor, u: 175, v: 825)));
        InMemoryInventoryRepository inventoryRepository =
            ResidentNeedsRuntimePlayModeHarness.GetField<InMemoryInventoryRepository>(
                runtime.Terrain,
                "_inventoryRepository");
        EntityId stackId = EntityId.Parse("fa000000000000000000000000000007");
        InventoryState inventory = inventoryRepository.Get();
        Require(inventory.AddUnit(
            stackId,
            CampfireProductionContent.StoneItemId,
            ItemLocation.InWorld(source),
            tick: 0));
        inventoryRepository.Save(inventory);

        Require(runtime.Terrain.CreateWorldItemPickup(
            stackId.ToString(),
            residentId,
            source,
            tick: 1));
        Require(runtime.Terrain.AdvanceWorldItemPickup(
            tick: 2,
            runtime.Residents.LoadView()));

        ItemStackSnapshot acquired = inventoryRepository.Get().GetStack(stackId)!;
        Assert.That(acquired.Location.Kind, Is.EqualTo(ItemLocationKind.AgentInventory));
        Assert.That(acquired.Location.OwnerId, Is.EqualTo(resident));
        Assert.That(acquired.ReservedQuantity, Is.Zero);
    }

    [Test]
    public void Pickup_splits_one_unit_from_aggregated_world_stack_into_resident_inventory()
    {
        ResidentNeedsRuntimePlayModeHarness.Runtime runtime =
            ResidentNeedsRuntimePlayModeHarness.CreateRuntime();
        string residentId = runtime.Residents.LoadView().First().Id;
        EntityId resident = EntityId.Parse(residentId);
        CellId source = runtime.Residents.Repository.Get(resident)!.Position;
        InMemoryInventoryRepository inventoryRepository =
            ResidentNeedsRuntimePlayModeHarness.GetField<InMemoryInventoryRepository>(
                runtime.Terrain,
                "_inventoryRepository");
        EntityId stackId = EntityId.Parse("fa000000000000000000000000000008");
        InventoryState inventory = inventoryRepository.Get();
        Require(inventory.AddStack(
            stackId,
            CampfireProductionContent.StoneItemId,
            quantity: 4,
            ItemLocation.InWorld(source),
            tick: 0));
        inventoryRepository.Save(inventory);

        Require(runtime.Terrain.ValidateResidentCanPickupStack(
            residentId,
            stackId.ToString()));
        Require(runtime.Terrain.CreateWorldItemPickup(
            stackId.ToString(),
            residentId,
            source,
            tick: 1));
        Require(runtime.Terrain.AdvanceWorldItemPickup(
            tick: 2,
            runtime.Residents.LoadView()));

        ItemStackSnapshot remainder = inventoryRepository.Get().GetStack(stackId)!;
        Assert.That(remainder.Location, Is.EqualTo(ItemLocation.InWorld(source)));
        Assert.That(remainder.Quantity, Is.EqualTo(3));
        Assert.That(remainder.ReservedQuantity, Is.Zero);
        ResidentInventoryLayoutViewModel layout =
            runtime.Terrain.LoadResidentInventoryLayout(residentId);
        ResidentInventoryLayoutSlotViewModel carried = layout.Slots.Single(value =>
            value.StackId != null
            && value.StackId != stackId.ToString());
        Assert.That(carried.Quantity, Is.EqualTo(1));
        Assert.That(inventoryRepository.Get().GetStack(EntityId.Parse(carried.StackId!))!.Location.OwnerId,
            Is.EqualTo(resident));
    }

    [Test]
    public void Second_direct_pickup_releases_first_job_reservation_before_new_claim()
    {
        ResidentNeedsRuntimePlayModeHarness.Runtime runtime =
            ResidentNeedsRuntimePlayModeHarness.CreateRuntime();
        string residentId = runtime.Residents.LoadView().First().Id;
        InMemoryInventoryRepository inventoryRepository =
            ResidentNeedsRuntimePlayModeHarness.GetField<InMemoryInventoryRepository>(
                runtime.Terrain,
                "_inventoryRepository");
        InMemoryJobRepository jobRepository =
            ResidentNeedsRuntimePlayModeHarness.GetField<InMemoryJobRepository>(
                runtime.Terrain,
                "_jobRepository");
        EntityId firstStackId = EntityId.Parse("fa000000000000000000000000000001");
        EntityId secondStackId = EntityId.Parse("fa000000000000000000000000000002");
        CellId firstCell = new CellId(2, 2, 0);
        CellId secondCell = new CellId(3, 2, 0);
        InventoryState inventory = inventoryRepository.Get();
        Require(inventory.AddUnit(
            firstStackId,
            CampfireProductionContent.StoneItemId,
            ItemLocation.InWorld(firstCell),
            tick: 0));
        Require(inventory.AddUnit(
            secondStackId,
            CampfireProductionContent.MushroomCapItemId,
            ItemLocation.InWorld(secondCell),
            tick: 0));
        inventoryRepository.Save(inventory);

        Require(runtime.Terrain.CreateWorldItemPickup(
            firstStackId.ToString(),
            residentId,
            firstCell,
            tick: 1));
        Assert.That(
            inventoryRepository.Get().GetStack(firstStackId)!.ReservedQuantity,
            Is.EqualTo(1));

        Require(runtime.Terrain.CreateWorldItemPickup(
            secondStackId.ToString(),
            residentId,
            secondCell,
            tick: 2));

        ItemStackSnapshot first = inventoryRepository.Get().GetStack(firstStackId)!;
        ItemStackSnapshot second = inventoryRepository.Get().GetStack(secondStackId)!;
        Assert.That(first.ReservedQuantity, Is.Zero);
        Assert.That(second.ReservedQuantity, Is.EqualTo(1));
        JobSnapshot[] pickupJobs = jobRepository.Get().GetAll()
            .Where(value => value.Definition is WorldItemPickupJobDefinition)
            .OrderBy(value => value.Definition.CreatedTick)
            .ToArray();
        Assert.That(
            inventoryRepository.Get().GetResidentSlotClaims(pickupJobs[0].Id),
            Is.Empty);
        Assert.That(pickupJobs.Length, Is.EqualTo(2));
        Assert.That(pickupJobs[0].Status, Is.EqualTo(JobStatus.Cancelled));
        Assert.That(pickupJobs[1].IsTerminal, Is.False);
        Assert.That(pickupJobs[1].AssignedAgentId?.ToString(), Is.EqualTo(residentId));
    }

    [Test]
    public void Pickup_replaces_active_manual_route_and_acquires_exact_stack()
    {
        ResidentNeedsRuntimePlayModeHarness.Runtime runtime =
            ResidentNeedsRuntimePlayModeHarness.CreateRuntime();
        string residentId = runtime.Residents.LoadView().First().Id;
        EntityId resident = EntityId.Parse(residentId);
        CellId current = runtime.Residents.Repository.Get(resident)!.Position;
        CellId destination = runtime.Residents.TunnelVolume.SupportedCells
            .Where(value => value != current)
            .Where(value => runtime.Residents.TunnelVolume
                .FindPath(current, value).Succeeded)
            .OrderByDescending(value => System.Math.Abs(value.X - current.X)
                + System.Math.Abs(value.Y - current.Y)
                + System.Math.Abs(value.Z - current.Z))
            .First();
        Assert.That(runtime.Residents.MoveResidentThroughTunnel(
            residentId,
            destination).Result.IsSuccess, Is.True);
        Assert.That(runtime.Residents.HasManualTunnelMovement(residentId), Is.True);

        runtime.Terrain.BindDirectCommandManualMovementCancellation(
            value => runtime.Residents.CancelManualTunnelMovement(value.ToString()));
        InMemoryInventoryRepository inventoryRepository =
            ResidentNeedsRuntimePlayModeHarness.GetField<InMemoryInventoryRepository>(
                runtime.Terrain,
                "_inventoryRepository");
        EntityId stackId = EntityId.Parse("fa000000000000000000000000000003");
        InventoryState inventory = inventoryRepository.Get();
        Require(inventory.AddUnit(
            stackId,
            CampfireProductionContent.StoneItemId,
            ItemLocation.InWorld(destination),
            tick: 0));
        inventoryRepository.Save(inventory);

        Require(runtime.Terrain.CreateWorldItemPickup(
            stackId.ToString(),
            residentId,
            destination,
            tick: 1));

        Assert.That(runtime.Residents.HasManualTunnelMovement(residentId), Is.False);
        bool reachedSource = false;
        for (int tick = 0; tick < 24; tick++)
        {
            ResidentNeedsRuntimePlayModeHarness.RunTick(runtime);
            if (runtime.Residents.Repository.Get(resident)!.Position != destination)
            {
                continue;
            }

            reachedSource = true;
            ItemStackSnapshot onArrival = inventoryRepository.Get().GetStack(stackId)!;
            Assert.That(
                onArrival.Location.Kind,
                Is.EqualTo(ItemLocationKind.AgentInventory),
                "Pickup must enter resident inventory on the same tick the resident reaches the source cell.");
            break;
        }

        Assert.That(reachedSource, Is.True, "Resident never reached the pickup source cell.");
        ItemStackSnapshot acquired = inventoryRepository.Get().GetStack(stackId)!;
        Assert.That(acquired.Location.Kind, Is.EqualTo(ItemLocationKind.AgentInventory));
        Assert.That(acquired.Location.OwnerId, Is.EqualTo(resident));
        Assert.That(acquired.ReservedQuantity, Is.Zero);
        Assert.That(runtime.Terrain.LoadAllWorldItems()
            .Any(value => value.StackId == stackId.ToString()), Is.False);
        Assert.That(runtime.Terrain.LoadResidentInventoryLayout(residentId).Slots
            .Any(value => value.StackId == stackId.ToString()), Is.True);
    }

    [Test]
    public void Pickup_acquires_item_from_reachable_unsupported_surface()
    {
        ResidentNeedsRuntimePlayModeHarness.Runtime runtime =
            ResidentNeedsRuntimePlayModeHarness.CreateRuntime();
        string residentId = runtime.Residents.LoadView().First().Id;
        EntityId resident = EntityId.Parse(residentId);
        CellId current = runtime.Residents.Repository.Get(resident)!.Position;
        CellId source = runtime.Residents.TunnelVolume.SupportedCells
            .Where(value => value != current)
            .Where(value => !runtime.Residents.TunnelVolume.HasFullActorSupport(value))
            .Where(value => runtime.Residents.TunnelVolume
                .FindPath(current, value).Succeeded)
            .OrderBy(value => System.Math.Abs(value.X - current.X)
                + System.Math.Abs(value.Y - current.Y)
                + System.Math.Abs(value.Z - current.Z))
            .First();
        InMemoryInventoryRepository inventoryRepository =
            ResidentNeedsRuntimePlayModeHarness.GetField<InMemoryInventoryRepository>(
                runtime.Terrain,
                "_inventoryRepository");
        EntityId stackId = EntityId.Parse("fa000000000000000000000000000006");
        InventoryState inventory = inventoryRepository.Get();
        Require(inventory.AddUnit(
            stackId,
            CampfireProductionContent.StoneItemId,
            ItemLocation.InWorld(source),
            tick: 0));
        inventoryRepository.Save(inventory);

        Require(runtime.Terrain.CreateWorldItemPickup(
            stackId.ToString(),
            residentId,
            source,
            tick: 1));

        for (int tick = 0; tick < 64; tick++)
        {
            ResidentNeedsRuntimePlayModeHarness.RunTick(runtime);
            if (inventoryRepository.Get().GetStack(stackId)!.Location.Kind
                == ItemLocationKind.AgentInventory)
            {
                break;
            }
        }

        ItemStackSnapshot acquired = inventoryRepository.Get().GetStack(stackId)!;
        Assert.That(acquired.Location.Kind, Is.EqualTo(ItemLocationKind.AgentInventory));
        Assert.That(acquired.Location.OwnerId, Is.EqualTo(resident));
        Assert.That(acquired.ReservedQuantity, Is.Zero);
    }

    private static void Require(Result result)
    {
        Assert.That(result.IsSuccess, Is.True, result.Error?.ToString());
    }
}

}
