using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Ecology;
using Dig.Domain.Inventory;
using Dig.Domain.Navigation;
using Dig.Domain.World;

namespace Dig.Application.Saving
{

public static class LivingMaterialEcologySaveAdapter
{
    private const int CurrentTimingCadenceVersion = 1;
    private const int LegacyEcologyStepsPerDay = 96;

    public static LivingMaterialEcologySaveData Encode(LivingMaterialEcologyState state)
    {
        if (state == null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        LivingMaterialEcologySnapshot snapshot = state.CaptureSnapshot();
        LivingMaterialEcologySaveData data = new LivingMaterialEcologySaveData
        {
            WorldSeed = snapshot.WorldSeed,
            EcologyStep = snapshot.EcologyStep,
            Version = snapshot.Version,
            TimingCadenceVersion = CurrentTimingCadenceVersion,
        };
        foreach (LivingMaterialSnapshot creature in snapshot.Creatures)
        {
            CellId cell = creature.Cell ?? default;
            data.Creatures.Add(new LivingMaterialIndividualSaveData
            {
                CreatureId = creature.CreatureId.ToString(),
                ItemEntityId = creature.ItemEntityId.ToString(),
                Species = (int)creature.Species,
                Containment = (int)creature.Containment,
                HasCell = creature.Cell.HasValue,
                CellX = cell.X,
                CellY = cell.Y,
                CellZ = cell.Z,
                AnchorX = creature.AnchorCell.X,
                AnchorY = creature.AnchorCell.Y,
                AnchorZ = creature.AnchorCell.Z,
                PlaneRootX = creature.PlaneKey.Root.X,
                PlaneRootY = creature.PlaneKey.Root.Y,
                PlaneRootZ = creature.PlaneKey.Root.Z,
                Direction = creature.Direction,
                Activity = (int)creature.Activity,
                ActivityStepsRemaining = creature.ActivityStepsRemaining,
                MovementCredit = creature.MovementCredit,
                SuccessfulMovementSteps = creature.SuccessfulMovementSteps,
                NextSearchAtStep = creature.NextSearchAtStep,
                NextSleepAtStep = creature.NextSleepAtStep,
                ReproductionCyclesCompleted = creature.ReproductionCyclesCompleted,
                NextReproductionStep = creature.NextReproductionStep,
                DeterministicSequence = creature.DeterministicSequence,
                BlockedReason = creature.BlockedReason,
                Version = creature.Version,
                HasSurfacePose = creature.IsFree,
                SurfaceU = creature.SurfacePose.U,
                SurfaceV = creature.SurfacePose.V,
            });
        }

        return data;
    }

    public static Result<LivingMaterialEcologyState> Decode(
        LivingMaterialEcologySaveData? data,
        InventoryState inventory,
        ulong fallbackWorldSeed)
    {
        if (inventory == null)
        {
            throw new ArgumentNullException(nameof(inventory));
        }

        data ??= new LivingMaterialEcologySaveData { WorldSeed = fallbackWorldSeed };
        try
        {
            List<LivingMaterialSnapshot> creatures = new List<LivingMaterialSnapshot>();
            foreach (LivingMaterialIndividualSaveData saved in data.Creatures
                .OrderBy(value => value.CreatureId, StringComparer.Ordinal))
            {
                EntityId creatureId = EntityId.Parse(saved.CreatureId);
                EntityId itemEntityId = EntityId.Parse(saved.ItemEntityId);
                ItemStackSnapshot? stack = inventory.GetStack(itemEntityId);
                if (stack == null || stack.Quantity != 1
                    || !LivingMaterialEcologyProfiles.TryResolve(
                        stack.ItemId,
                        out LivingMaterialSpecies itemSpecies)
                    || itemSpecies != (LivingMaterialSpecies)saved.Species)
                {
                    return Result<LivingMaterialEcologyState>.Failure(
                        LivingMaterialErrors.InvalidSnapshot);
                }

                LivingMaterialContainment containment =
                    (LivingMaterialContainment)saved.Containment;
                bool itemIsFree = stack.Location.Kind == ItemLocationKind.World;
                if (itemIsFree != (containment == LivingMaterialContainment.Free)
                    || (saved.HasCell && !itemIsFree)
                    || (itemIsFree && (!saved.HasCell
                        || stack.Location.CellId != new CellId(
                            saved.CellX, saved.CellY, saved.CellZ))))
                {
                    return Result<LivingMaterialEcologyState>.Failure(
                        LivingMaterialErrors.InvalidSnapshot);
                }

                creatures.Add(new LivingMaterialSnapshot(
                    creatureId,
                    itemEntityId,
                    itemSpecies,
                    containment,
                    saved.HasCell
                        ? new CellId(saved.CellX, saved.CellY, saved.CellZ)
                        : (CellId?)null,
                    new CellId(saved.AnchorX, saved.AnchorY, saved.AnchorZ),
                    new LivingMaterialPlaneKey(new CellId(
                        saved.PlaneRootX, saved.PlaneRootY, saved.PlaneRootZ)),
                    saved.Direction,
                    (LivingMaterialActivity)saved.Activity,
                    saved.ActivityStepsRemaining,
                    saved.MovementCredit,
                    saved.SuccessfulMovementSteps,
                    saved.NextSearchAtStep,
                    saved.NextSleepAtStep,
                    saved.ReproductionCyclesCompleted,
                    MigrateNextReproductionStep(data, saved.NextReproductionStep),
                    saved.DeterministicSequence,
                    saved.BlockedReason,
                    saved.Version,
                    saved.HasCell
                        ? new SurfacePose(
                            new CellId(saved.CellX, saved.CellY, saved.CellZ),
                            SurfaceFace.Floor,
                            saved.HasSurfacePose ? saved.SurfaceU : SurfacePose.CellCentre,
                            saved.HasSurfacePose ? saved.SurfaceV : SurfacePose.CellCentre)
                        : (SurfacePose?)null));
            }

            return LivingMaterialEcologyState.Restore(
                new LivingMaterialEcologySnapshot(
                    data.WorldSeed == 0 ? fallbackWorldSeed : data.WorldSeed,
                    data.EcologyStep,
                    data.Version,
                    creatures));
        }
        catch (Exception exception) when (
            exception is ArgumentException
            || exception is FormatException
            || exception is OverflowException)
        {
            return Result<LivingMaterialEcologyState>.Failure(
                LivingMaterialErrors.InvalidSnapshot);
        }
    }

    private static long MigrateNextReproductionStep(
        LivingMaterialEcologySaveData data,
        long nextStep)
    {
        if (data.TimingCadenceVersion >= CurrentTimingCadenceVersion
            || nextStep <= data.EcologyStep)
        {
            return nextStep;
        }

        long remaining = nextStep - data.EcologyStep;
        return checked(data.EcologyStep + (remaining
            * LivingMaterialEcologyProfiles.EcologyStepsPerDay
            / LegacyEcologyStepsPerDay));
    }
}

}
