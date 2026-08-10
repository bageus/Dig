using System.Collections;
using System.Linq;
using Dig.Application.Inventory;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Presentation.Agents;
using Dig.Presentation.Buildings;
using NUnit.Framework;
using static Dig.Unity.Tests.BuildingBoxRuntimeLifecyclePlayModeHarness;

namespace Dig.Unity.Tests
{

public sealed class BuildingBoxRuntimeLifecyclePlayModeTests
{
    [Test]
    public void Held_box_arrival_starts_unpack_and_completes_same_lifecycle()
    {
        Runtime runtime = CreateRuntime();
        AgentViewModel worker = runtime.Agents[0];
        ItemStackSnapshot box = MoveCampfireBoxToResident(runtime, worker);
        BuildingBoxGhostViewModel preview = FindValidPreview(
            runtime,
            box.StackId,
            BuildingBoxPlacementKind.AssembleBuilding);

        AssertSuccess(Invoke(
            runtime.Terrain,
            "ConfirmBuildingBoxPlacement",
            preview,
            10L,
            runtime.Agents));

        BuildingWorldViewModel planned = PendingTransformation(runtime, box.StackId);
        Assert.That(planned.SourceBuildingBoxStackId, Is.EqualTo(box.StackId.ToString()));
        Assert.That(planned.BuildingBoxCommitState, Is.EqualTo(BuildingBoxCommitState.Reserved));
        JobSnapshot job = Job(runtime, EntityId.Parse(planned.BuildingBoxJobId!));
        Assert.That(job.Status, Is.EqualTo(JobStatus.Claimed));
        Assert.That(job.AssignedAgentId, Is.EqualTo(EntityId.Parse(worker.Id)));

        AgentViewModel atWork = AtCell(
            worker,
            new CellId(planned.WorkPositionX, planned.WorkPositionY, planned.WorkPositionZ));
        AssertSuccess(Invoke(
            runtime.Terrain,
            "AdvanceBuildingBoxAssembly",
            11L,
            new[] { atWork }));
        BuildingWorldViewModel atSite = PendingTransformation(runtime, box.StackId);
        Assert.That(atSite.BuildingBoxCommitState, Is.EqualTo(BuildingBoxCommitState.AtSite));
        Assert.That(atSite.CompletedWork, Is.EqualTo(0));
        Assert.That(atSite.Status, Is.EqualTo(BuildingStatus.ReadyToBuild));
        Assert.That(atSite.VisualState, Is.EqualTo(BuildingVisualState.Assembly));
        ItemStackSnapshot siteBox = runtime.Inventory.GetStack(box.StackId)!;
        Assert.That(
            siteBox.Location,
            Is.EqualTo(ItemLocation.InBuilding(EntityId.Parse(atSite.Id))));
        Assert.That(
            DropResidentInventoryStackHandler.IsOwnedByResident(
                siteBox.Location,
                EntityId.Parse(worker.Id)),
            Is.False);

        for (int completed = 1; completed <= 3; completed++)
        {
            AssertSuccess(Invoke(
                runtime.Terrain,
                "AdvanceBuildingBoxAssembly",
                11L + completed,
                new[] { atWork }));
            BuildingWorldViewModel progress = PendingTransformation(runtime, box.StackId);
            Assert.That(progress.CompletedWork, Is.EqualTo(completed));
        }

        AssertSuccess(Invoke(
            runtime.Terrain,
            "AdvanceBuildingBoxAssembly",
            15L,
            new[] { atWork }));
        BuildingWorldViewModel completedBuilding = Buildings(runtime)
            .Single(value => value.Id == planned.Id);
        Assert.That(completedBuilding.Status, Is.EqualTo(BuildingStatus.Completed));
        Assert.That(completedBuilding.IsPendingBuildingBoxLifecycle, Is.False);
        Assert.That(completedBuilding.SourceBuildingBoxStackId, Is.EqualTo(box.StackId.ToString()));
        Assert.That(runtime.Inventory.GetStack(box.StackId), Is.Null);
        Assert.That(Job(runtime, job.Id).Status, Is.EqualTo(JobStatus.Completed));
    }

    [Test]
    public void Held_box_natural_route_commits_immediately_on_arrival()
    {
        Runtime runtime = CreateRuntime();
        AgentViewModel worker = runtime.Agents[0];
        ItemStackSnapshot box = MoveCampfireBoxToResident(runtime, worker);
        BuildingBoxGhostViewModel preview = FindValidPreview(
            runtime,
            box.StackId,
            BuildingBoxPlacementKind.AssembleBuilding);
        AssertSuccess(Invoke(
            runtime.Terrain,
            "ConfirmBuildingBoxPlacement",
            preview,
            40L,
            runtime.Agents));

        BuildingWorldViewModel planned = PendingTransformation(runtime, box.StackId);
        int maximumTicks = 128;
        for (int index = 0; index < maximumTicks; index++)
        {
            AdvanceAssemblyTick(runtime, 41L + index);
            BuildingWorldViewModel current = PendingTransformation(runtime, box.StackId);
            if (current.BuildingBoxCommitState == BuildingBoxCommitState.AtSite)
            {
                Assert.That(current.CompletedWork, Is.EqualTo(0));
                Assert.That(current.VisualState, Is.EqualTo(BuildingVisualState.Assembly));
                ItemStackSnapshot siteBox = runtime.Inventory.GetStack(box.StackId)!;
                Assert.That(
                    siteBox.Location,
                    Is.EqualTo(ItemLocation.InBuilding(EntityId.Parse(current.Id))));
                Dig.Presentation.Inventory.ResidentInventoryLayoutViewModel layout =
                    (Dig.Presentation.Inventory.ResidentInventoryLayoutViewModel)Invoke(
                        runtime.Terrain,
                        "LoadResidentInventoryLayout",
                        worker.Id);
                Assert.That(
                    layout.Slots.Any(slot => slot.StackId == box.StackId.ToString()),
                    Is.False);
                return;
            }
        }

        Assert.Fail(
            $"BuildingBox {box.StackId} did not commit after natural navigation to "
            + $"{planned.WorkPositionX},{planned.WorkPositionY},{planned.WorkPositionZ}.");
    }

    [Test]
    public void Held_box_relocation_deposits_from_adjacent_supported_cell()
    {
        Runtime runtime = CreateRuntime();
        AgentViewModel worker = runtime.Agents[0];
        ItemStackSnapshot box = MoveCampfireBoxToResident(runtime, worker);
        BuildingBoxGhostViewModel preview = FindValidPreview(
            runtime,
            box.StackId,
            BuildingBoxPlacementKind.RelocateBox);

        AssertSuccess(Invoke(
            runtime.Terrain,
            "ConfirmBuildingBoxPlacement",
            preview,
            20L,
            runtime.Agents));

        JobSnapshot relocation = runtime.Jobs.GetAll()
            .Single(value => !value.IsTerminal
                && value.Definition is BuildingBoxPickupJobDefinition definition
                && definition.IsRelocation
                && definition.StackId == box.StackId);
        CellId workCell = FindSupportedAdjacentCell(runtime, preview.Origin);
        Assert.That(workCell, Is.Not.EqualTo(preview.Origin));

        AssertSuccess(Invoke(
            runtime.Terrain,
            "AdvanceBuildingBoxPickup",
            21L,
            new[] { AtCell(worker, workCell) }));

        ItemStackSnapshot deposited = runtime.Inventory.GetStack(box.StackId)!;
        Assert.That(deposited.StackId, Is.EqualTo(box.StackId));
        Assert.That(deposited.Location, Is.EqualTo(ItemLocation.InWorld(preview.Origin)));
        Assert.That(deposited.Quantity, Is.EqualTo(1));
        Assert.That(deposited.ReservedQuantity, Is.EqualTo(0));
        Assert.That(Job(runtime, relocation.Id).Status, Is.EqualTo(JobStatus.Completed));
        Assert.That(
            ((IEnumerable)Invoke(runtime.Terrain, "LoadBuildingBoxRelocationPlans"))
                .Cast<object>(),
            Is.Empty);
    }

    [Test]
    public void Direct_move_cancellation_removes_plan_and_keeps_same_box()
    {
        Runtime runtime = CreateRuntime();
        AgentViewModel worker = runtime.Agents[0];
        ItemStackSnapshot box = MoveCampfireBoxToResident(runtime, worker);
        BuildingBoxGhostViewModel preview = FindValidPreview(
            runtime,
            box.StackId,
            BuildingBoxPlacementKind.AssembleBuilding);
        AssertSuccess(Invoke(
            runtime.Terrain,
            "ConfirmBuildingBoxPlacement",
            preview,
            30L,
            runtime.Agents));
        BuildingWorldViewModel planned = PendingTransformation(runtime, box.StackId);
        EntityId jobId = EntityId.Parse(planned.BuildingBoxJobId!);

        AssertSuccess(Invoke(
            runtime.Terrain,
            "PrepareResidentsForDirectCommand",
            new[] { worker.Id },
            31L));

        Assert.That(Job(runtime, jobId).Status, Is.EqualTo(JobStatus.Cancelled));
        Assert.That(
            Buildings(runtime).Any(value => value.SourceBuildingBoxStackId
                == box.StackId.ToString()
                && value.IsPendingBuildingBoxLifecycle),
            Is.False);
        ItemStackSnapshot preserved = runtime.Inventory.GetStack(box.StackId)!;
        Assert.That(preserved.StackId, Is.EqualTo(box.StackId));
        Assert.That(
            DropResidentInventoryStackHandler.IsOwnedByResident(
                preserved.Location,
                EntityId.Parse(worker.Id)),
            Is.True);
        Assert.That(preserved.Location.HasResidentSlot, Is.True);
        Assert.That(preserved.Quantity, Is.EqualTo(1));
        Assert.That(preserved.ReservedQuantity, Is.EqualTo(0));
    }
}

}
