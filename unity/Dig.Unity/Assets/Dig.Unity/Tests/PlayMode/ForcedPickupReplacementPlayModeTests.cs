using System.Linq;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using NUnit.Framework;

namespace Dig.Unity.Tests
{

public sealed class ForcedPickupReplacementPlayModeTests
{
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
        Assert.That(pickupJobs.Length, Is.EqualTo(2));
        Assert.That(pickupJobs[0].Status, Is.EqualTo(JobStatus.Cancelled));
        Assert.That(pickupJobs[1].IsTerminal, Is.False);
        Assert.That(pickupJobs[1].AssignedAgentId?.ToString(), Is.EqualTo(residentId));
    }

    private static void Require(Result result)
    {
        Assert.That(result.IsSuccess, Is.True, result.Error?.ToString());
    }
}

}
