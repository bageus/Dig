using System;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Domain.Ecology
{

public sealed partial class LivingMaterialEcologyState
{
    public Result AdvanceOneEcologyStep(long tick)
    {
        ValidateTick(tick);
        EcologyStep = checked(EcologyStep + 1);
        foreach (LivingMaterialIndividual value in _creatures.Values
            .OrderBy(item => item.PlaneKey)
            .ThenBy(item => item.Species)
            .ThenBy(item => item.CreatureId.ToString(), StringComparer.Ordinal))
        {
            if (value.Containment == LivingMaterialContainment.Stored)
            {
                continue;
            }

            bool consumedActivityStep = value.ActivityStepsRemaining > 0;
            bool changed = AdvanceActivityTimer(value, tick);
            if (!consumedActivityStep
                && (value.Activity == LivingMaterialActivity.Moving
                    || value.Activity == LivingMaterialActivity.Blocked))
            {
                LivingMaterialSpeciesProfile profile = LivingMaterialEcologyProfiles.Get(value.Species);
                int next = checked(value.MovementCredit + profile.MovementCreditPerEcologyStep);
                value.MovementCredit = Math.Min(
                    LivingMaterialEcologyProfiles.MovementThreshold,
                    next);
                changed = true;
            }

            if (changed)
            {
                IncrementVersion(value);
            }
        }

        Version = checked(Version + 1);
        return Result.Success();
    }

    public Result CommitMovement(
        EntityId creatureId,
        CellId target,
        LivingMaterialPlaneKey planeKey,
        int direction,
        long tick)
    {
        ValidateTick(tick);
        if (!_creatures.TryGetValue(creatureId, out LivingMaterialIndividual? value))
        {
            return Result.Failure(LivingMaterialErrors.NotFound);
        }

        LivingMaterialSpeciesProfile profile = LivingMaterialEcologyProfiles.Get(value.Species);
        if (value.Containment != LivingMaterialContainment.Free
            || value.MovementCredit < LivingMaterialEcologyProfiles.MovementThreshold
            || (value.Activity != LivingMaterialActivity.Moving
                && value.Activity != LivingMaterialActivity.Blocked))
        {
            return Result.Failure(LivingMaterialErrors.InvalidState);
        }

        if (direction < -1 || direction > 1 || direction == 0
            || target.Y != value.AnchorCell.Y
            || target.Z != value.AnchorCell.Z
            || Math.Abs(target.X - value.AnchorCell.X) > profile.WanderRadius
            || planeKey != value.PlaneKey
            || Math.Abs(target.X - value.Cell.X) != 1)
        {
            return Result.Failure(LivingMaterialErrors.InvalidMovement);
        }

        CellId from = value.Cell;
        value.Cell = target;
        value.Direction = direction;
        value.MovementCredit -= LivingMaterialEcologyProfiles.MovementThreshold;
        value.SuccessfulMovementSteps = checked(value.SuccessfulMovementSteps + 1);
        value.BlockedReason = null;
        value.DeterministicSequence = checked(value.DeterministicSequence + 1);
        ApplyPostMovementActivity(value, tick);
        IncrementVersion(value);
        Raise(new LivingMaterialMoved(tick, creatureId, from, target));
        return Result.Success();
    }

    public Result CommitBlocked(
        EntityId creatureId,
        int nextDirection,
        string reason,
        long tick)
    {
        ValidateTick(tick);
        if (!_creatures.TryGetValue(creatureId, out LivingMaterialIndividual? value))
        {
            return Result.Failure(LivingMaterialErrors.NotFound);
        }

        if (value.Containment != LivingMaterialContainment.Free
            || (value.Activity != LivingMaterialActivity.Moving
                && value.Activity != LivingMaterialActivity.Blocked)
            || value.MovementCredit < LivingMaterialEcologyProfiles.MovementThreshold
            || nextDirection < -1 || nextDirection > 1 || nextDirection == 0
            || string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure(LivingMaterialErrors.InvalidState);
        }

        value.Direction = nextDirection;
        value.Activity = LivingMaterialActivity.Blocked;
        value.BlockedReason = reason.Trim();
        value.DeterministicSequence = checked(value.DeterministicSequence + 1);
        IncrementVersion(value);
        Raise(new LivingMaterialActivityChanged(tick, creatureId, value.Activity, 0));
        return Result.Success();
    }

    private bool AdvanceActivityTimer(LivingMaterialIndividual value, long tick)
    {
        if (value.ActivityStepsRemaining <= 0)
        {
            return false;
        }

        value.ActivityStepsRemaining--;
        if (value.ActivityStepsRemaining > 0)
        {
            return true;
        }

        value.Activity = LivingMaterialActivity.Moving;
        value.BlockedReason = null;
        Raise(new LivingMaterialActivityChanged(tick, value.CreatureId, value.Activity, 0));
        return true;
    }

    private void ApplyPostMovementActivity(LivingMaterialIndividual value, long tick)
    {
        if (value.Species != LivingMaterialSpecies.Hamster)
        {
            value.Activity = LivingMaterialActivity.Moving;
            value.ActivityStepsRemaining = 0;
            return;
        }

        if (value.SuccessfulMovementSteps >= value.NextSleepAtStep)
        {
            int duration = SelectInclusive(value, "sleep-duration", 4, 8);
            int interval = SelectInclusive(value, "sleep-interval", 16, 32);
            value.Activity = LivingMaterialActivity.HamsterSleeping;
            value.ActivityStepsRemaining = duration;
            value.NextSleepAtStep = checked(value.SuccessfulMovementSteps + interval);
            Raise(new LivingMaterialActivityChanged(
                tick, value.CreatureId, value.Activity, duration));
            return;
        }

        if (value.SuccessfulMovementSteps >= value.NextSearchAtStep)
        {
            int duration = SelectInclusive(value, "search-duration", 1, 2);
            int interval = SelectInclusive(value, "search-interval", 4, 8);
            value.Activity = LivingMaterialActivity.HamsterSearching;
            value.ActivityStepsRemaining = duration;
            value.NextSearchAtStep = checked(value.SuccessfulMovementSteps + interval);
            Raise(new LivingMaterialActivityChanged(
                tick, value.CreatureId, value.Activity, duration));
            return;
        }

        value.Activity = LivingMaterialActivity.Moving;
        value.ActivityStepsRemaining = 0;
    }

    private int SelectInclusive(
        LivingMaterialIndividual value,
        string purpose,
        int minimum,
        int maximum)
    {
        int selected = LivingMaterialDeterminism.SelectInclusive(
            WorldSeed,
            value.CreatureId,
            value.DeterministicSequence,
            purpose,
            minimum,
            maximum);
        value.DeterministicSequence = checked(value.DeterministicSequence + 1);
        return selected;
    }
}

}
