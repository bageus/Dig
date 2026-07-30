using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Domain.Ecology
{

public sealed partial class LivingMaterialEcologyState : AggregateRoot
{
    private readonly Dictionary<EntityId, LivingMaterialIndividual> _creatures =
        new Dictionary<EntityId, LivingMaterialIndividual>();
    private readonly Dictionary<EntityId, EntityId> _creatureByItem =
        new Dictionary<EntityId, EntityId>();

    public LivingMaterialEcologyState(ulong worldSeed)
    {
        WorldSeed = worldSeed;
    }

    public ulong WorldSeed { get; }

    public long EcologyStep { get; private set; }

    public long Version { get; private set; }

    public LivingMaterialSnapshot? Get(EntityId creatureId)
    {
        return _creatures.TryGetValue(creatureId, out LivingMaterialIndividual? value)
            ? value.ToSnapshot()
            : null;
    }

    public LivingMaterialSnapshot? GetByItem(EntityId itemEntityId)
    {
        return _creatureByItem.TryGetValue(itemEntityId, out EntityId creatureId)
            ? Get(creatureId)
            : null;
    }

    public IReadOnlyList<LivingMaterialSnapshot> GetAll()
    {
        return _creatures.Values
            .Select(value => value.ToSnapshot())
            .OrderBy(value => value.PlaneKey)
            .ThenBy(value => value.Species)
            .ThenBy(value => value.CreatureId.ToString(), StringComparer.Ordinal)
            .ToArray();
    }

    public LivingMaterialEcologySnapshot CaptureSnapshot()
    {
        return new LivingMaterialEcologySnapshot(
            WorldSeed,
            EcologyStep,
            Version,
            GetAll());
    }

    public Result Register(
        EntityId creatureId,
        EntityId itemEntityId,
        LivingMaterialSpecies species,
        CellId? worldCell,
        LivingMaterialPlaneKey planeKey,
        long tick)
    {
        ValidateTick(tick);
        if (creatureId.IsEmpty || itemEntityId.IsEmpty)
        {
            return Result.Failure(LivingMaterialErrors.InvalidState);
        }

        if (_creatures.ContainsKey(creatureId) || _creatureByItem.ContainsKey(itemEntityId))
        {
            return Result.Failure(LivingMaterialErrors.AlreadyExists);
        }

        LivingMaterialIndividual value = LivingMaterialIndividual.Create(
            WorldSeed,
            creatureId,
            itemEntityId,
            species,
            worldCell,
            planeKey,
            EcologyStep);
        _creatures.Add(creatureId, value);
        _creatureByItem.Add(itemEntityId, creatureId);
        IncrementVersion(value);
        Raise(new LivingMaterialRegistered(tick, creatureId, species));
        return Result.Success();
    }

    public Result Store(EntityId creatureId, long tick)
    {
        ValidateTick(tick);
        if (!_creatures.TryGetValue(creatureId, out LivingMaterialIndividual? value))
        {
            return Result.Failure(LivingMaterialErrors.NotFound);
        }

        if (value.Containment == LivingMaterialContainment.Stored)
        {
            return Result.Success();
        }

        value.Containment = LivingMaterialContainment.Stored;
        value.Activity = LivingMaterialActivity.Stored;
        value.ActivityStepsRemaining = 0;
        value.MovementCredit = 0;
        value.BlockedReason = null;
        IncrementVersion(value);
        Raise(new LivingMaterialContainmentChanged(tick, creatureId, value.Containment, null));
        return Result.Success();
    }

    public Result Release(
        EntityId creatureId,
        CellId cell,
        LivingMaterialPlaneKey planeKey,
        long tick)
    {
        ValidateTick(tick);
        if (!_creatures.TryGetValue(creatureId, out LivingMaterialIndividual? value))
        {
            return Result.Failure(LivingMaterialErrors.NotFound);
        }

        bool transitioned = value.Containment == LivingMaterialContainment.Stored
            || value.Cell != cell;
        value.Containment = LivingMaterialContainment.Free;
        value.Cell = cell;
        value.AnchorCell = cell;
        value.PlaneKey = planeKey;
        value.Direction = SelectDirection(value, "release-direction");
        value.MovementCredit = 0;
        value.BlockedReason = null;
        value.Activity = value.Species == LivingMaterialSpecies.Hamster && transitioned
            ? LivingMaterialActivity.ReleaseDormant
            : LivingMaterialActivity.Moving;
        value.ActivityStepsRemaining = value.Activity == LivingMaterialActivity.ReleaseDormant
            ? LivingMaterialEcologyProfiles.HamsterReleaseDormancySteps
            : 0;
        value.DeterministicSequence++;
        IncrementVersion(value);
        Raise(new LivingMaterialContainmentChanged(tick, creatureId, value.Containment, cell));
        Raise(new LivingMaterialActivityChanged(
            tick,
            creatureId,
            value.Activity,
            value.ActivityStepsRemaining));
        return Result.Success();
    }

    private void IncrementVersion(LivingMaterialIndividual value)
    {
        value.Version = checked(value.Version + 1);
        Version = checked(Version + 1);
    }

    private int SelectDirection(LivingMaterialIndividual value, string purpose)
    {
        return LivingMaterialDeterminism.SelectInclusive(
            WorldSeed,
            value.CreatureId,
            value.DeterministicSequence,
            purpose,
            0,
            1) == 0 ? -1 : 1;
    }

    private static void ValidateTick(long tick)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }
    }

    private sealed partial class LivingMaterialIndividual
    {
        public EntityId CreatureId;
        public EntityId ItemEntityId;
        public LivingMaterialSpecies Species;
        public LivingMaterialContainment Containment;
        public CellId Cell;
        public CellId AnchorCell;
        public LivingMaterialPlaneKey PlaneKey;
        public int Direction;
        public LivingMaterialActivity Activity;
        public int ActivityStepsRemaining;
        public int MovementCredit;
        public int SuccessfulMovementSteps;
        public int NextSearchAtStep;
        public int NextSleepAtStep;
        public int ReproductionCyclesCompleted;
        public long NextReproductionStep;
        public long DeterministicSequence;
        public string? BlockedReason;
        public long Version;

        public static LivingMaterialIndividual Create(
            ulong worldSeed,
            EntityId creatureId,
            EntityId itemEntityId,
            LivingMaterialSpecies species,
            CellId? worldCell,
            LivingMaterialPlaneKey planeKey,
            long ecologyStep)
        {
            LivingMaterialIndividual value = new LivingMaterialIndividual
            {
                CreatureId = creatureId,
                ItemEntityId = itemEntityId,
                Species = species,
                Containment = worldCell.HasValue
                    ? LivingMaterialContainment.Free
                    : LivingMaterialContainment.Stored,
                Cell = worldCell ?? planeKey.Root,
                AnchorCell = worldCell ?? planeKey.Root,
                PlaneKey = planeKey,
                Direction = LivingMaterialDeterminism.SelectInclusive(
                    worldSeed, creatureId, 0, "initial-direction", 0, 1) == 0 ? -1 : 1,
                Activity = worldCell.HasValue
                    ? LivingMaterialActivity.Moving
                    : LivingMaterialActivity.Stored,
                NextReproductionStep = checked(ecologyStep + LivingMaterialEcologyProfiles.EcologyStepsPerDay),
                NextSearchAtStep = species == LivingMaterialSpecies.Hamster
                    ? LivingMaterialDeterminism.SelectInclusive(worldSeed, creatureId, 0, "initial-search", 4, 8)
                    : int.MaxValue,
                NextSleepAtStep = species == LivingMaterialSpecies.Hamster
                    ? LivingMaterialDeterminism.SelectInclusive(worldSeed, creatureId, 0, "initial-sleep", 16, 32)
                    : int.MaxValue,
                Version = 0,
            };
            return value;
        }

        public LivingMaterialSnapshot ToSnapshot()
        {
            return new LivingMaterialSnapshot(
                CreatureId,
                ItemEntityId,
                Species,
                Containment,
                Containment == LivingMaterialContainment.Free ? Cell : (CellId?)null,
                AnchorCell,
                PlaneKey,
                Direction,
                Activity,
                ActivityStepsRemaining,
                MovementCredit,
                SuccessfulMovementSteps,
                NextSearchAtStep,
                NextSleepAtStep,
                ReproductionCyclesCompleted,
                NextReproductionStep,
                DeterministicSequence,
                BlockedReason,
                Version);
        }
    }
}

}
