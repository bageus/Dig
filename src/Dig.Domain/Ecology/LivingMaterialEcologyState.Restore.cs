using System;
using Dig.Domain.Core;

namespace Dig.Domain.Ecology
{

public sealed partial class LivingMaterialEcologyState
{
    public static Result<LivingMaterialEcologyState> Restore(
        LivingMaterialEcologySnapshot snapshot)
    {
        if (snapshot == null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        try
        {
            LivingMaterialEcologyState state = new LivingMaterialEcologyState(snapshot.WorldSeed)
            {
                EcologyStep = snapshot.EcologyStep,
                Version = snapshot.Version,
            };
            foreach (LivingMaterialSnapshot saved in snapshot.Creatures)
            {
                LivingMaterialIndividual value = LivingMaterialIndividual.FromSnapshot(saved);
                if (!state._creatures.TryAdd(value.CreatureId, value)
                    || !state._creatureByItem.TryAdd(value.ItemEntityId, value.CreatureId))
                {
                    return Result<LivingMaterialEcologyState>.Failure(
                        LivingMaterialErrors.InvalidSnapshot);
                }
            }

            return Result<LivingMaterialEcologyState>.Success(state);
        }
        catch (ArgumentException)
        {
            return Result<LivingMaterialEcologyState>.Failure(
                LivingMaterialErrors.InvalidSnapshot);
        }
    }

    private sealed partial class LivingMaterialIndividual
    {
        public static LivingMaterialIndividual FromSnapshot(LivingMaterialSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            return new LivingMaterialIndividual
            {
                CreatureId = snapshot.CreatureId,
                ItemEntityId = snapshot.ItemEntityId,
                Species = snapshot.Species,
                Containment = snapshot.Containment,
                Cell = snapshot.Cell ?? snapshot.AnchorCell,
                SurfacePose = snapshot.SurfacePose,
                AnchorCell = snapshot.AnchorCell,
                PlaneKey = snapshot.PlaneKey,
                Direction = snapshot.Direction,
                Activity = snapshot.Activity,
                ActivityStepsRemaining = snapshot.ActivityStepsRemaining,
                MovementCredit = snapshot.MovementCredit,
                SuccessfulMovementSteps = snapshot.SuccessfulMovementSteps,
                NextSearchAtStep = snapshot.NextSearchAtStep,
                NextSleepAtStep = snapshot.NextSleepAtStep,
                ReproductionCyclesCompleted = snapshot.ReproductionCyclesCompleted,
                NextReproductionStep = snapshot.NextReproductionStep,
                DeterministicSequence = snapshot.DeterministicSequence,
                BlockedReason = snapshot.BlockedReason,
                Version = snapshot.Version,
            };
        }
    }
}

}
