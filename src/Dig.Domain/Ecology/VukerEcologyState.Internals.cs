using System;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Domain.Ecology
{

public sealed partial class VukerEcologyState
{
    private void BreakPair(Individual individual, long tick, string reasonCode)
    {
        if (!individual.ActivePairId.HasValue)
        {
            return;
        }

        BreakPair(_pairs[individual.ActivePairId.Value], tick, reasonCode);
    }

    private void BreakPair(Pair pair, long tick, string reasonCode)
    {
        if (!pair.IsActive)
        {
            return;
        }

        pair.IsActive = false;
        pair.TerminalReason = reasonCode;
        ClearPairLink(pair.FirstParentId, pair.Id);
        ClearPairLink(pair.SecondParentId, pair.Id);
        Touch(pair);
        Raise(new VukerPairBroken(tick, pair.Id, reasonCode));
    }

    private void ClearPairLink(EntityId parentId, VukerPairId pairId)
    {
        if (_individuals.TryGetValue(parentId, out Individual? parent)
            && parent.ActivePairId == pairId)
        {
            parent.ActivePairId = null;
            Touch(parent);
        }
    }

    private static EntityId OtherParent(Pair pair, EntityId id)
    {
        return pair.FirstParentId == id ? pair.SecondParentId : pair.FirstParentId;
    }

    private static bool EligibleParent(Individual value)
    {
        return value.IsAlive
            && value.Lifecycle == VukerLifecycleStage.Adult
            && value.Disposition == VukerDisposition.Wild;
    }

    private void Touch(Individual value)
    {
        value.Version = checked(value.Version + 1);
        Version = checked(Version + 1);
    }

    private void Touch(Pair value)
    {
        value.Version = checked(value.Version + 1);
        Version = checked(Version + 1);
    }

    private static void ValidateTick(long tick)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }
    }

    private sealed class Individual
    {
        public EntityId Id;
        public VukerLifecycleStage Lifecycle;
        public VukerDisposition Disposition;
        public VukerRegionKey Region;
        public CellId Position;
        public bool IsAlive;
        public long BirthTick;
        public long MaturityTick;
        public EntityId? KidnapReservedBy;
        public EntityId? TamedByResidentId;
        public VukerPairId? ActivePairId;
        public long Version;

        public VukerIndividualSnapshot ToSnapshot()
        {
            return new VukerIndividualSnapshot(
                Id,
                Lifecycle,
                Disposition,
                Region,
                Position,
                IsAlive,
                BirthTick,
                MaturityTick,
                KidnapReservedBy,
                TamedByResidentId,
                ActivePairId,
                Version);
        }

        public static Individual FromSnapshot(VukerIndividualSnapshot snapshot)
        {
            return new Individual
            {
                Id = snapshot.EntityId,
                Lifecycle = snapshot.Lifecycle,
                Disposition = snapshot.Disposition,
                Region = snapshot.Region,
                Position = snapshot.Position,
                IsAlive = snapshot.IsAlive,
                BirthTick = snapshot.BirthTick,
                MaturityTick = snapshot.MaturityTick,
                KidnapReservedBy = snapshot.KidnapReservedBy,
                TamedByResidentId = snapshot.TamedByResidentId,
                ActivePairId = snapshot.ActivePairId,
                Version = snapshot.Version,
            };
        }
    }

    private sealed class Pair
    {
        public VukerPairId Id;
        public EntityId FirstParentId;
        public EntityId SecondParentId;
        public VukerRegionKey Region;
        public int SuccessfulCycles;
        public long NextBirthTick;
        public bool IsActive;
        public string? TerminalReason;
        public string? BlockedReason;
        public long Version;

        public bool IsDue(long tick)
        {
            return IsActive
                && SuccessfulCycles < VukerEcologyProfile.MaximumSuccessfulCyclesPerPair
                && tick >= NextBirthTick;
        }

        public VukerPairSnapshot ToSnapshot()
        {
            return new VukerPairSnapshot(
                Id,
                FirstParentId,
                SecondParentId,
                Region,
                SuccessfulCycles,
                NextBirthTick,
                IsActive,
                TerminalReason,
                BlockedReason,
                Version);
        }

        public static Pair FromSnapshot(VukerPairSnapshot snapshot)
        {
            return new Pair
            {
                Id = snapshot.PairId,
                FirstParentId = snapshot.FirstParentId,
                SecondParentId = snapshot.SecondParentId,
                Region = snapshot.Region,
                SuccessfulCycles = snapshot.SuccessfulCycles,
                NextBirthTick = snapshot.NextBirthTick,
                IsActive = snapshot.IsActive,
                TerminalReason = snapshot.TerminalReason,
                BlockedReason = snapshot.BlockedReason,
                Version = snapshot.Version,
            };
        }
    }
}

}
