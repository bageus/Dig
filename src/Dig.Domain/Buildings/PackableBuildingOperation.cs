using System;
using System.Collections.Generic;

namespace Dig.Domain.Buildings
{
    public enum PackableBuildingState
    {
        PackedInWorld = 0,
        PackedInInventory = 1,
        UnpackPlanned = 2,
        Unpacking = 3,
        ActiveBuilding = 4,
        PackPlanned = 5,
        Packing = 6,
        InterruptedPartial = 7,
    }

    public enum PackableBuildingOperationKind
    {
        Pack = 0,
        Unpack = 1,
    }

    public readonly struct PackableBuildingIterationCompletion
    {
        public PackableBuildingIterationCompletion(
            int iterationNumber,
            string workerId,
            PackableBuildingOperationKind operation)
        {
            if (iterationNumber < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(iterationNumber));
            }

            if (string.IsNullOrWhiteSpace(workerId))
            {
                throw new ArgumentException("Worker id is required.", nameof(workerId));
            }

            IterationNumber = iterationNumber;
            WorkerId = workerId;
            Operation = operation;
        }

        public int IterationNumber { get; }

        public string WorkerId { get; }

        public PackableBuildingOperationKind Operation { get; }
    }

    public sealed class PackableBuildingWorkProgress
    {
        private readonly List<PackableBuildingIterationCompletion> _completions =
            new List<PackableBuildingIterationCompletion>();

        public PackableBuildingWorkProgress(
            PackableBuildingOperationKind operation,
            int totalIterations,
            decimal baseWorkMinutesPerIteration)
        {
            if (totalIterations < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(totalIterations));
            }

            if (baseWorkMinutesPerIteration <= 0m)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(baseWorkMinutesPerIteration));
            }

            Operation = operation;
            TotalIterations = totalIterations;
            BaseWorkMinutesPerIteration = baseWorkMinutesPerIteration;
        }

        public PackableBuildingOperationKind Operation { get; }

        public int TotalIterations { get; }

        public decimal BaseWorkMinutesPerIteration { get; }

        public int CompletedIterations => _completions.Count;

        public decimal CurrentIterationWorkMinutes { get; private set; }

        public bool IsComplete => CompletedIterations == TotalIterations;

        public IReadOnlyList<PackableBuildingIterationCompletion> Completions =>
            _completions.AsReadOnly();

        public IReadOnlyList<PackableBuildingIterationCompletion> Advance(
            string workerId,
            decimal baseWorkMinutes)
        {
            if (string.IsNullOrWhiteSpace(workerId))
            {
                throw new ArgumentException("Worker id is required.", nameof(workerId));
            }

            if (baseWorkMinutes < 0m)
            {
                throw new ArgumentOutOfRangeException(nameof(baseWorkMinutes));
            }

            if (IsComplete || baseWorkMinutes == 0m)
            {
                return Array.Empty<PackableBuildingIterationCompletion>();
            }

            List<PackableBuildingIterationCompletion> completedNow =
                new List<PackableBuildingIterationCompletion>();
            decimal remaining = baseWorkMinutes;
            while (remaining > 0m && !IsComplete)
            {
                decimal needed = BaseWorkMinutesPerIteration
                    - CurrentIterationWorkMinutes;
                decimal applied = Math.Min(remaining, needed);
                CurrentIterationWorkMinutes += applied;
                remaining -= applied;

                if (CurrentIterationWorkMinutes < BaseWorkMinutesPerIteration)
                {
                    continue;
                }

                CurrentIterationWorkMinutes = 0m;
                PackableBuildingIterationCompletion completion =
                    new PackableBuildingIterationCompletion(
                        CompletedIterations + 1,
                        workerId,
                        Operation);
                _completions.Add(completion);
                completedNow.Add(completion);
            }

            return completedNow;
        }
    }
}
