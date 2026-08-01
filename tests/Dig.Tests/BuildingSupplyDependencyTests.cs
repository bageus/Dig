using System.Linq;
using Dig.Application.Production;
using Dig.Application.Saving;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class BuildingSupplyDependencyTests
{
    [Fact]
    public void Extraction_and_deferred_delivery_share_one_planning_pass_and_job_identity()
    {
        CampfireProductionTestHarness harness = new CampfireProductionTestHarness(1);
        EntityId extractionJobId = CampfireProductionTestHarness.Id(700);
        EntityId deliveryJobId = CampfireProductionTestHarness.Id(701);
        CellId sourceCell = new CellId(1, 1, 0);
        Assert.True(harness.Jobs.Add(new DigJobDefinition(
            extractionJobId,
            new DigJobTarget(sourceCell),
            priority: 625,
            createdTick: 1,
            retryPolicy: JobRetryPolicy.Default)).IsSuccess);
        Assert.True(harness.Jobs.MakeAvailable(extractionJobId, 1).IsSuccess);
        Assert.True(harness.Jobs.Claim(
            extractionJobId,
            CampfireProductionTestHarness.WorkerId,
            1).IsSuccess);

        CreateDeferredBuildingSupplyJobHandler create =
            new CreateDeferredBuildingSupplyJobHandler(
                harness.Content,
                harness.BuildingsRepository,
                harness.JobsRepository,
                harness.Journal);
        Assert.True(create.Handle(new CreateDeferredBuildingSupplyJobCommand(
            deliveryJobId,
            CampfireProductionTestHarness.BuildingId,
            new[]
            {
                new ItemConsumptionRequest(
                    CampfireProductionContent.MushroomCapItemId,
                    1),
            },
            new[] { extractionJobId },
            Enumerable.Range(0, 12)
                .Select(value => CampfireProductionTestHarness.Id(710 + value))
                .ToArray(),
            new[] { CampfireProductionTestHarness.Id(730) },
            priority: 625,
            tick: 1)).IsSuccess);

        JobSnapshot pending = harness.Jobs.Get(deliveryJobId)!;
        BuildingSupplyJobDefinition pendingDefinition =
            Assert.IsType<BuildingSupplyJobDefinition>(pending.Definition);
        Assert.Equal(JobStatus.Created, pending.Status);
        Assert.False(pendingDefinition.IsSourceResolved);
        Assert.Equal(extractionJobId, Assert.Single(pendingDefinition.Dependencies));
        Assert.Equal(1, Assert.Single(pendingDefinition.RequestedItems).Quantity);

        Assert.True(harness.Jobs.Complete(extractionJobId, 2).IsSuccess);
        EntityId capId = CampfireProductionTestHarness.Id(731);
        Assert.True(harness.Inventory.AddStack(
            capId,
            CampfireProductionContent.MushroomCapItemId,
            1,
            ItemLocation.InWorld(sourceCell),
            2).IsSuccess);

        ResolveDeferredBuildingSupplyJobHandler resolve =
            new ResolveDeferredBuildingSupplyJobHandler(
                harness.Content,
                harness.SupplyRepository,
                harness.BuildingsRepository,
                harness.InventoryRepository,
                harness.JobsRepository,
                harness.Journal);
        Result resolved = resolve.Handle(new ResolveDeferredBuildingSupplyJobCommand(
            deliveryJobId,
            CampfireProductionTestHarness.WorkerId,
            new[] { sourceCell },
            new[] { sourceCell },
            tick: 3));

        Assert.True(resolved.IsSuccess, resolved.Error?.ToString());
        JobSnapshot delivery = harness.Jobs.Get(deliveryJobId)!;
        BuildingSupplyJobDefinition definition =
            Assert.IsType<BuildingSupplyJobDefinition>(delivery.Definition);
        Assert.Equal(JobStatus.Claimed, delivery.Status);
        Assert.True(definition.IsSourceResolved);
        Assert.Equal(capId, Assert.Single(definition.Allocations).StackId);
        Assert.Equal(1, harness.Inventory.GetStack(capId)!.ReservedQuantity);
        Assert.Equal(deliveryJobId, harness.Supply.Get(
            CampfireProductionTestHarness.BuildingId,
            harness.Inventory.CreateSnapshot())!.ActiveSupplyJobId);
    }

    [Fact]
    public void Deferred_delivery_can_retry_with_a_later_resident_after_capacity_failure()
    {
        CampfireProductionTestHarness harness = new CampfireProductionTestHarness(1);
        EntityId extractionJobId = CampfireProductionTestHarness.Id(750);
        EntityId deliveryJobId = CampfireProductionTestHarness.Id(751);
        EntityId secondResidentId = CampfireProductionTestHarness.Id(752);
        CellId sourceCell = new CellId(1, 1, 0);
        Assert.True(harness.Jobs.Add(new DigJobDefinition(
            extractionJobId,
            new DigJobTarget(sourceCell),
            priority: 625,
            createdTick: 1,
            retryPolicy: JobRetryPolicy.Default)).IsSuccess);
        Assert.True(harness.Jobs.MakeAvailable(extractionJobId, 1).IsSuccess);
        Assert.True(harness.Jobs.Claim(
            extractionJobId,
            CampfireProductionTestHarness.WorkerId,
            1).IsSuccess);
        Assert.True(harness.Jobs.Complete(extractionJobId, 2).IsSuccess);

        for (int index = 0; index < 6; index++)
        {
            Assert.True(harness.Inventory.AddStack(
                CampfireProductionTestHarness.Id(760 + index),
                CampfireProductionContent.StoneItemId,
                1,
                ItemLocation.InResidentSlot(
                    CampfireProductionTestHarness.WorkerId,
                    ResidentInventoryCompartment.Main,
                    index),
                2).IsSuccess);
        }

        EntityId capId = CampfireProductionTestHarness.Id(770);
        Assert.True(harness.Inventory.AddStack(
            capId,
            CampfireProductionContent.MushroomCapItemId,
            1,
            ItemLocation.InWorld(sourceCell),
            2).IsSuccess);
        CreateDeferredBuildingSupplyJobHandler create =
            new CreateDeferredBuildingSupplyJobHandler(
                harness.Content,
                harness.BuildingsRepository,
                harness.JobsRepository,
                harness.Journal);
        Assert.True(create.Handle(new CreateDeferredBuildingSupplyJobCommand(
            deliveryJobId,
            CampfireProductionTestHarness.BuildingId,
            new[]
            {
                new ItemConsumptionRequest(
                    CampfireProductionContent.MushroomCapItemId,
                    1),
            },
            new[] { extractionJobId },
            Enumerable.Range(0, 12)
                .Select(value => CampfireProductionTestHarness.Id(780 + value))
                .ToArray(),
            new[] { CampfireProductionTestHarness.Id(795) },
            priority: 625,
            tick: 2)).IsSuccess);
        ResolveDeferredBuildingSupplyJobHandler resolve =
            new ResolveDeferredBuildingSupplyJobHandler(
                harness.Content,
                harness.SupplyRepository,
                harness.BuildingsRepository,
                harness.InventoryRepository,
                harness.JobsRepository,
                harness.Journal);

        Result first = resolve.Handle(new ResolveDeferredBuildingSupplyJobCommand(
            deliveryJobId,
            CampfireProductionTestHarness.WorkerId,
            new[] { sourceCell },
            new[] { sourceCell },
            tick: 3));
        Assert.True(first.IsFailure);
        Assert.Equal(JobStatus.Created, harness.Jobs.Get(deliveryJobId)!.Status);

        Result second = resolve.Handle(new ResolveDeferredBuildingSupplyJobCommand(
            deliveryJobId,
            secondResidentId,
            new[] { sourceCell },
            new[] { sourceCell },
            tick: 4));
        Assert.True(second.IsSuccess, second.Error?.ToString());
        Assert.Equal(
            secondResidentId,
            harness.Jobs.Get(deliveryJobId)!.AssignedAgentId!.Value);
        Assert.Equal(1, harness.Inventory.GetStack(capId)!.ReservedQuantity);
    }

    [Fact]
    public void Deferred_delivery_definition_round_trips_requested_item_and_dependency()
    {
        EntityId dependencyId = CampfireProductionTestHarness.Id(740);
        BuildingSupplyJobDefinition definition = new BuildingSupplyJobDefinition(
            CampfireProductionTestHarness.Id(741),
            CampfireProductionTestHarness.BuildingId,
            new CellId(4, 3, 0),
            new[]
            {
                new ItemConsumptionRequest(
                    CampfireProductionContent.MushroomCapItemId,
                    1),
            },
            new[] { CampfireProductionTestHarness.Id(742) },
            new[] { CampfireProductionTestHarness.Id(743) },
            priority: 625,
            createdTick: 5,
            retryPolicy: JobRetryPolicy.Default,
            dependencies: new[] { dependencyId });
        BuildingSupplyJobSaveCodec codec = new BuildingSupplyJobSaveCodec();

        BuildingSupplyJobDefinition decoded = Assert.IsType<BuildingSupplyJobDefinition>(
            codec.Decode(codec.Encode(definition)));

        Assert.False(decoded.IsSourceResolved);
        Assert.Equal(dependencyId, Assert.Single(decoded.Dependencies));
        Assert.Equal(
            CampfireProductionContent.MushroomCapItemId,
            Assert.Single(decoded.RequestedItems).ItemId);
    }
}

}
