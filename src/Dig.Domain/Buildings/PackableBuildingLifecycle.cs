using System;
using System.Collections.Generic;

namespace Dig.Domain.Buildings
{
    public sealed class PackableBuildingLifecycle
    {
        private readonly int _packIterations;
        private readonly int _unpackIterations;
        private readonly decimal _baseWorkMinutesPerIteration;
        private PackableBuildingState _stateBeforePlan;
        private PackableBuildingOperationKind? _interruptedOperation;

        public PackableBuildingLifecycle(
            string packageId,
            string buildingDefinitionId,
            PackableBuildingState initialState,
            int packIterations,
            int unpackIterations,
            decimal baseWorkMinutesPerIteration)
        {
            if (string.IsNullOrWhiteSpace(packageId))
            {
                throw new ArgumentException("Package id is required.", nameof(packageId));
            }

            if (string.IsNullOrWhiteSpace(buildingDefinitionId))
            {
                throw new ArgumentException(
                    "Building definition id is required.",
                    nameof(buildingDefinitionId));
            }

            if (!IsStableInitialState(initialState))
            {
                throw new ArgumentOutOfRangeException(nameof(initialState));
            }

            if (packIterations < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(packIterations));
            }

            if (unpackIterations < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(unpackIterations));
            }

            if (baseWorkMinutesPerIteration <= 0m)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(baseWorkMinutesPerIteration));
            }

            PackageId = packageId;
            BuildingDefinitionId = buildingDefinitionId;
            State = initialState;
            _packIterations = packIterations;
            _unpackIterations = unpackIterations;
            _baseWorkMinutesPerIteration = baseWorkMinutesPerIteration;
        }

        public string PackageId { get; }

        public string BuildingDefinitionId { get; }

        public PackableBuildingState State { get; private set; }

        public string? ActiveWorkerId { get; private set; }

        public PackableBuildingWorkProgress? Progress { get; private set; }

        public void PlanUnpack()
        {
            RequireState(
                PackableBuildingState.PackedInWorld,
                PackableBuildingState.PackedInInventory);
            _stateBeforePlan = State;
            Progress = new PackableBuildingWorkProgress(
                PackableBuildingOperationKind.Unpack,
                _unpackIterations,
                _baseWorkMinutesPerIteration);
            State = PackableBuildingState.UnpackPlanned;
        }

        public void PlanPack()
        {
            RequireState(PackableBuildingState.ActiveBuilding);
            _stateBeforePlan = State;
            Progress = new PackableBuildingWorkProgress(
                PackableBuildingOperationKind.Pack,
                _packIterations,
                _baseWorkMinutesPerIteration);
            State = PackableBuildingState.PackPlanned;
        }

        public void CancelPlannedOperation()
        {
            RequireState(
                PackableBuildingState.UnpackPlanned,
                PackableBuildingState.PackPlanned);
            State = _stateBeforePlan;
            Progress = null;
            ActiveWorkerId = null;
            _interruptedOperation = null;
        }

        public void StartPlannedWork(string workerId)
        {
            RequireWorker(workerId);
            if (State == PackableBuildingState.UnpackPlanned)
            {
                State = PackableBuildingState.Unpacking;
            }
            else if (State == PackableBuildingState.PackPlanned)
            {
                State = PackableBuildingState.Packing;
            }
            else
            {
                throw InvalidTransition("start planned work");
            }

            ActiveWorkerId = workerId;
        }

        public void InterruptActiveWork()
        {
            RequireState(
                PackableBuildingState.Unpacking,
                PackableBuildingState.Packing);
            _interruptedOperation = State == PackableBuildingState.Unpacking
                ? PackableBuildingOperationKind.Unpack
                : PackableBuildingOperationKind.Pack;
            State = PackableBuildingState.InterruptedPartial;
            ActiveWorkerId = null;
        }

        public void ResumeInterruptedWork(string workerId)
        {
            RequireWorker(workerId);
            RequireState(PackableBuildingState.InterruptedPartial);
            if (!_interruptedOperation.HasValue || Progress == null)
            {
                throw new InvalidOperationException(
                    "Interrupted operation has no resumable progress.");
            }

            State = _interruptedOperation == PackableBuildingOperationKind.Unpack
                ? PackableBuildingState.Unpacking
                : PackableBuildingState.Packing;
            ActiveWorkerId = workerId;
        }

        public IReadOnlyList<PackableBuildingIterationCompletion> AdvanceWork(
            decimal baseWorkMinutes)
        {
            RequireState(
                PackableBuildingState.Unpacking,
                PackableBuildingState.Packing);
            if (Progress == null || string.IsNullOrWhiteSpace(ActiveWorkerId))
            {
                throw new InvalidOperationException(
                    "Active work requires progress and an assigned worker.");
            }

            IReadOnlyList<PackableBuildingIterationCompletion> completed =
                Progress.Advance(ActiveWorkerId, baseWorkMinutes);
            if (!Progress.IsComplete)
            {
                return completed;
            }

            State = Progress.Operation == PackableBuildingOperationKind.Unpack
                ? PackableBuildingState.ActiveBuilding
                : PackableBuildingState.PackedInWorld;
            ActiveWorkerId = null;
            _interruptedOperation = null;
            return completed;
        }

        private void RequireState(params PackableBuildingState[] allowed)
        {
            for (int index = 0; index < allowed.Length; index++)
            {
                if (State == allowed[index])
                {
                    return;
                }
            }

            throw InvalidTransition("change lifecycle state");
        }

        private InvalidOperationException InvalidTransition(string action)
        {
            return new InvalidOperationException(
                $"Cannot {action} while package '{PackageId}' is in state {State}.");
        }

        private static void RequireWorker(string workerId)
        {
            if (string.IsNullOrWhiteSpace(workerId))
            {
                throw new ArgumentException("Worker id is required.", nameof(workerId));
            }
        }

        private static bool IsStableInitialState(PackableBuildingState state)
        {
            return state == PackableBuildingState.PackedInWorld
                || state == PackableBuildingState.PackedInInventory
                || state == PackableBuildingState.ActiveBuilding;
        }
    }
}
