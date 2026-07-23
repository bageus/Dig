using System;
using System.Linq;
using Dig.Domain.Buildings;
using Xunit;

namespace Dig.Tests
{
    public sealed class PackableBuildingLifecycleTests
    {
        [Fact]
        public void Packed_world_box_can_be_planned_and_unpacked()
        {
            PackableBuildingLifecycle lifecycle = Create(
                PackableBuildingState.PackedInWorld);

            lifecycle.PlanUnpack();
            lifecycle.StartPlannedWork("worker-a");
            lifecycle.AdvanceWork(30m);

            Assert.Equal(PackableBuildingState.ActiveBuilding, lifecycle.State);
            Assert.Null(lifecycle.ActiveWorkerId);
            Assert.Equal(3, lifecycle.Progress!.CompletedIterations);
        }

        [Fact]
        public void Packed_inventory_box_can_start_unpacking()
        {
            PackableBuildingLifecycle lifecycle = Create(
                PackableBuildingState.PackedInInventory);

            lifecycle.PlanUnpack();
            lifecycle.StartPlannedWork("inventory-owner");

            Assert.Equal(PackableBuildingState.Unpacking, lifecycle.State);
            Assert.Equal("inventory-owner", lifecycle.ActiveWorkerId);
        }

        [Fact]
        public void Active_building_can_be_packed_back_into_world_box()
        {
            PackableBuildingLifecycle lifecycle = Create(
                PackableBuildingState.ActiveBuilding);

            lifecycle.PlanPack();
            lifecycle.StartPlannedWork("worker-a");
            lifecycle.AdvanceWork(30m);

            Assert.Equal(PackableBuildingState.PackedInWorld, lifecycle.State);
            Assert.Equal(
                PackableBuildingOperationKind.Pack,
                lifecycle.Progress!.Operation);
        }

        [Fact]
        public void Cancelling_plan_restores_original_stable_state()
        {
            PackableBuildingLifecycle inventory = Create(
                PackableBuildingState.PackedInInventory);
            inventory.PlanUnpack();
            inventory.CancelPlannedOperation();

            PackableBuildingLifecycle building = Create(
                PackableBuildingState.ActiveBuilding);
            building.PlanPack();
            building.CancelPlannedOperation();

            Assert.Equal(PackableBuildingState.PackedInInventory, inventory.State);
            Assert.Null(inventory.Progress);
            Assert.Equal(PackableBuildingState.ActiveBuilding, building.State);
            Assert.Null(building.Progress);
        }

        [Fact]
        public void Interrupted_work_preserves_partial_iteration_progress()
        {
            PackableBuildingLifecycle lifecycle = Create(
                PackableBuildingState.PackedInWorld);
            lifecycle.PlanUnpack();
            lifecycle.StartPlannedWork("worker-a");
            lifecycle.AdvanceWork(6m);

            lifecycle.InterruptActiveWork();
            lifecycle.ResumeInterruptedWork("worker-b");
            var completions = lifecycle.AdvanceWork(4m);

            Assert.Single(completions);
            Assert.Equal("worker-b", completions[0].WorkerId);
            Assert.Equal(1, lifecycle.Progress!.CompletedIterations);
            Assert.Equal(0m, lifecycle.Progress.CurrentIterationWorkMinutes);
        }

        [Fact]
        public void Different_workers_receive_their_own_iteration_records()
        {
            PackableBuildingLifecycle lifecycle = Create(
                PackableBuildingState.PackedInWorld);
            lifecycle.PlanUnpack();
            lifecycle.StartPlannedWork("worker-a");
            lifecycle.AdvanceWork(20m);
            lifecycle.InterruptActiveWork();
            lifecycle.ResumeInterruptedWork("worker-b");
            lifecycle.AdvanceWork(10m);

            string[] workers = lifecycle.Progress!.Completions
                .Select(value => value.WorkerId)
                .ToArray();
            Assert.Equal(new[] { "worker-a", "worker-a", "worker-b" }, workers);
        }

        [Fact]
        public void One_advance_can_complete_multiple_iterations_without_overflow()
        {
            PackableBuildingLifecycle lifecycle = Create(
                PackableBuildingState.PackedInWorld);
            lifecycle.PlanUnpack();
            lifecycle.StartPlannedWork("worker-a");

            var completed = lifecycle.AdvanceWork(100m);

            Assert.Equal(3, completed.Count);
            Assert.Equal(3, lifecycle.Progress!.Completions.Count);
            Assert.Equal(0m, lifecycle.Progress.CurrentIterationWorkMinutes);
            Assert.Equal(PackableBuildingState.ActiveBuilding, lifecycle.State);
        }

        [Fact]
        public void Completed_operation_cannot_be_finalized_twice()
        {
            PackableBuildingLifecycle lifecycle = Create(
                PackableBuildingState.PackedInWorld);
            lifecycle.PlanUnpack();
            lifecycle.StartPlannedWork("worker-a");
            lifecycle.AdvanceWork(30m);

            Assert.Throws<InvalidOperationException>(() => lifecycle.AdvanceWork(10m));
            Assert.Equal(3, lifecycle.Progress!.Completions.Count);
        }

        [Theory]
        [InlineData(PackableBuildingState.UnpackPlanned)]
        [InlineData(PackableBuildingState.Unpacking)]
        [InlineData(PackableBuildingState.PackPlanned)]
        [InlineData(PackableBuildingState.Packing)]
        [InlineData(PackableBuildingState.InterruptedPartial)]
        public void Runtime_only_states_cannot_be_used_as_initial_state(
            PackableBuildingState state)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Create(state));
        }

        [Fact]
        public void Invalid_transition_is_rejected()
        {
            PackableBuildingLifecycle lifecycle = Create(
                PackableBuildingState.ActiveBuilding);

            Assert.Throws<InvalidOperationException>(() => lifecycle.PlanUnpack());
            Assert.Throws<InvalidOperationException>(
                () => lifecycle.StartPlannedWork("worker-a"));
            Assert.Throws<InvalidOperationException>(() => lifecycle.InterruptActiveWork());
        }

        private static PackableBuildingLifecycle Create(
            PackableBuildingState state)
        {
            return new PackableBuildingLifecycle(
                packageId: "package-campfire-1",
                buildingDefinitionId: "building.campfire",
                initialState: state,
                packIterations: 3,
                unpackIterations: 3,
                baseWorkMinutesPerIteration: 10m);
        }
    }
}
