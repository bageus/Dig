using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Domain.Ecology
{

public sealed partial class VukerEcologyState
{
    public EntityId CreateDeterministicChildId(VukerPairId pairId, int cycleIndex)
    {
        if (pairId.IsEmpty)
        {
            throw new ArgumentException("Pair id is required.", nameof(pairId));
        }

        if (cycleIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cycleIndex));
        }

        string source = WorldSeed + ":" + pairId + ":child:" + cycleIndex;
        using (SHA256 hash = SHA256.Create())
        {
            byte[] digest = hash.ComputeHash(Encoding.UTF8.GetBytes(source));
            byte[] guid = new byte[16];
            Array.Copy(digest, guid, guid.Length);
            guid[6] = (byte)((guid[6] & 0x0f) | 0x40);
            guid[8] = (byte)((guid[8] & 0x3f) | 0x80);
            return new EntityId(new Guid(guid));
        }
    }

    public Result CommitBirth(
        VukerPairId pairId,
        EntityId childId,
        VukerRegionKey region,
        CellId position,
        long tick)
    {
        ValidateTick(tick);
        if (!_pairs.TryGetValue(pairId, out Pair? pair))
        {
            return Result.Failure(VukerEcologyErrors.PairNotFound);
        }

        if (!pair.IsDue(tick) || pair.Region != region)
        {
            return Result.Failure(VukerEcologyErrors.BirthNotDue);
        }

        if (_individuals.ContainsKey(childId))
        {
            return Result.Success();
        }

        if (CountLiving(region) >= VukerEcologyProfile.PopulationCapPerRegion)
        {
            pair.BlockedReason = "population_cap";
            Touch(pair);
            return Result.Failure(VukerEcologyErrors.PopulationCapReached);
        }

        Individual child = new Individual
        {
            Id = childId,
            Lifecycle = VukerLifecycleStage.Child,
            Disposition = VukerDisposition.Wild,
            Region = region,
            Position = position,
            IsAlive = true,
            BirthTick = tick,
            MaturityTick = checked(tick + VukerEcologyProfile.ChildGrowthTicks),
        };
        _individuals.Add(childId, child);
        pair.SuccessfulCycles = checked(pair.SuccessfulCycles + 1);
        pair.BlockedReason = null;
        pair.NextBirthTick = checked(tick + VukerEcologyProfile.ReproductionCooldownTicks);
        if (pair.SuccessfulCycles >= VukerEcologyProfile.MaximumSuccessfulCyclesPerPair)
        {
            pair.TerminalReason = "cycle_limit_reached";
        }

        Touch(child);
        Touch(pair);
        Raise(new VukerChildBorn(
            tick,
            pair.Id,
            child.Id,
            pair.SuccessfulCycles,
            position));
        return Result.Success();
    }

    public Result RecordBirthBlocked(VukerPairId pairId, string reasonCode, long tick)
    {
        ValidateTick(tick);
        if (string.IsNullOrWhiteSpace(reasonCode))
        {
            throw new ArgumentException("Blocked reason is required.", nameof(reasonCode));
        }

        if (!_pairs.TryGetValue(pairId, out Pair? pair))
        {
            return Result.Failure(VukerEcologyErrors.PairNotFound);
        }

        if (!pair.IsDue(tick))
        {
            return Result.Failure(VukerEcologyErrors.BirthNotDue);
        }

        if (string.Equals(pair.BlockedReason, reasonCode, StringComparison.Ordinal))
        {
            return Result.Success();
        }

        pair.BlockedReason = reasonCode.Trim();
        Touch(pair);
        Raise(new VukerBirthBlocked(tick, pairId, pair.BlockedReason));
        return Result.Success();
    }
    private void MatureChildren(long tick)
    {
        foreach (Individual child in _individuals.Values
            .Where(value => value.IsAlive
                && value.Lifecycle == VukerLifecycleStage.Child
                && tick >= value.MaturityTick)
            .OrderBy(value => value.Id.ToString(), StringComparer.Ordinal))
        {
            child.Lifecycle = VukerLifecycleStage.Adult;
            Touch(child);
            Raise(new VukerMatured(tick, child.Id, child.Disposition));
        }
    }

    private void ValidateActivePairs(long tick)
    {
        foreach (Pair pair in _pairs.Values
            .Where(value => value.IsActive)
            .OrderBy(value => value.Id))
        {
            if (!_individuals.TryGetValue(pair.FirstParentId, out Individual? first)
                || !_individuals.TryGetValue(pair.SecondParentId, out Individual? second)
                || !EligibleParent(first)
                || !EligibleParent(second)
                || first.Region != second.Region
                || first.Region != pair.Region)
            {
                BreakPair(pair, tick, "pair_parent_ineligible");
            }
        }
    }

    private void FormPairs(long tick)
    {
        IEnumerable<IGrouping<VukerRegionKey, Individual>> regions = _individuals.Values
            .Where(EligibleParent)
            .Where(value => !value.ActivePairId.HasValue)
            .OrderBy(value => value.Region)
            .ThenBy(value => value.Id.ToString(), StringComparer.Ordinal)
            .GroupBy(value => value.Region);
        foreach (IGrouping<VukerRegionKey, Individual> region in regions)
        {
            Individual[] candidates = region.ToArray();
            for (int index = 0; index + 1 < candidates.Length; index += 2)
            {
                FormPair(candidates[index], candidates[index + 1], tick);
            }
        }
    }

    private void FormPair(Individual first, Individual second, long tick)
    {
        EntityId firstId = string.Compare(
            first.Id.ToString(),
            second.Id.ToString(),
            StringComparison.Ordinal) <= 0
                ? first.Id
                : second.Id;
        EntityId secondId = firstId == first.Id ? second.Id : first.Id;
        VukerPairId pairId = new VukerPairId(
            firstId + ":" + secondId + ":" + NextPairSequence);
        NextPairSequence = checked(NextPairSequence + 1);
        Pair pair = new Pair
        {
            Id = pairId,
            FirstParentId = firstId,
            SecondParentId = secondId,
            Region = first.Region,
            SuccessfulCycles = 0,
            NextBirthTick = checked(tick + VukerEcologyProfile.ReproductionCooldownTicks),
            IsActive = true,
        };
        _pairs.Add(pairId, pair);
        first.ActivePairId = pairId;
        second.ActivePairId = pairId;
        Touch(first);
        Touch(second);
        Touch(pair);
        Raise(new VukerPairFormed(tick, pairId, firstId, secondId, pair.Region));
    }
}

}
