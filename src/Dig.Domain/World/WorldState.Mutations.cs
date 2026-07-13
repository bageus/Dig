using System.Collections.ObjectModel;
using Dig.Domain.Core;

namespace Dig.Domain.World;

public sealed partial class WorldState
{
    public Result<WorldMutationResult> SetDigDesignation(
        CellId cellId,
        bool designated,
        long tick)
    {
        ValidateTick(tick);
        if (!Size.Contains(cellId))
        {
            return Result<WorldMutationResult>.Failure(WorldErrors.CellOutOfBounds);
        }

        CellState current = _cells[GetCellIndex(cellId)];
        MaterialDefinition material = Materials.Get(current.MaterialId)!;
        if (designated && !material.IsSolid)
        {
            return Result<WorldMutationResult>.Failure(
                WorldErrors.DigDesignationRequiresSolidCell);
        }

        CellDesignation designation = designated
            ? CellDesignation.Dig
            : CellDesignation.None;
        return ApplyTerrainChanges(
            new[] { new TerrainChange(cellId, current.WithDesignation(designation)) },
            tick);
    }

    public Result<WorldMutationResult> Excavate(
        CellId cellId,
        MaterialId emptyMaterialId,
        long tick)
    {
        ValidateTick(tick);
        if (!Size.Contains(cellId))
        {
            return Result<WorldMutationResult>.Failure(WorldErrors.CellOutOfBounds);
        }

        MaterialDefinition? emptyMaterial = Materials.Get(emptyMaterialId);
        if (emptyMaterial is null)
        {
            return Result<WorldMutationResult>.Failure(WorldErrors.UnknownMaterial);
        }

        if (emptyMaterial.IsSolid)
        {
            return Result<WorldMutationResult>.Failure(
                WorldErrors.ExcavationRequiresEmptyMaterial);
        }

        CellState current = _cells[GetCellIndex(cellId)];
        return ApplyTerrainChanges(
            new[] { new TerrainChange(cellId, current.WithTerrain(emptyMaterialId)) },
            tick);
    }

    public Result<WorldMutationResult> ApplyTerrainChanges(
        IEnumerable<TerrainChange> changes,
        long tick)
    {
        if (changes is null)
        {
            throw new ArgumentNullException(nameof(changes));
        }

        ValidateTick(tick);
        TerrainChange[] requested = changes.ToArray();
        if (requested.Length == 0)
        {
            return Result<WorldMutationResult>.Success(CreateNoChangeResult());
        }

        HashSet<CellId> seenCells = new HashSet<CellId>();
        List<ValidatedChange> actualChanges = new List<ValidatedChange>();
        foreach (TerrainChange change in requested)
        {
            Result validation = ValidateTerrainChange(change, seenCells);
            if (validation.IsFailure)
            {
                return Result<WorldMutationResult>.Failure(validation.Error!);
            }

            CellState previousState = _cells[GetCellIndex(change.CellId)];
            if (previousState != change.TargetState)
            {
                actualChanges.Add(new ValidatedChange(
                    change.CellId,
                    previousState,
                    change.TargetState));
            }
        }

        if (actualChanges.Count == 0)
        {
            return Result<WorldMutationResult>.Success(CreateNoChangeResult());
        }

        actualChanges.Sort((left, right) => left.CellId.CompareTo(right.CellId));
        long nextVersion = checked(Version + 1);
        HashSet<ChunkId> affectedChunks = new HashSet<ChunkId>();

        foreach (ValidatedChange change in actualChanges)
        {
            _cells[GetCellIndex(change.CellId)] = change.CurrentState;
            foreach (ChunkId chunkId in Layout.GetAffectedChunks(change.CellId))
            {
                affectedChunks.Add(chunkId);
            }
        }

        Version = nextVersion;
        foreach (ValidatedChange change in actualChanges)
        {
            Raise(new CellChanged(
                tick,
                nextVersion,
                change.CellId,
                change.PreviousState,
                change.CurrentState));
        }

        ChunkId[] orderedChunks = affectedChunks.OrderBy(chunk => chunk).ToArray();
        foreach (ChunkId chunkId in orderedChunks)
        {
            _chunkVersions[GetChunkIndex(chunkId)] = nextVersion;
            _dirtyChunks.Add(chunkId);
            Raise(new ChunkInvalidated(
                tick,
                nextVersion,
                chunkId,
                nextVersion));
        }

        WorldMutationResult result = new WorldMutationResult(
            nextVersion,
            actualChanges.Count,
            new ReadOnlyCollection<ChunkId>(orderedChunks));
        return Result<WorldMutationResult>.Success(result);
    }

    private Result ValidateTerrainChange(
        TerrainChange change,
        HashSet<CellId> seenCells)
    {
        if (!Size.Contains(change.CellId))
        {
            return Result.Failure(WorldErrors.CellOutOfBounds);
        }

        if (!seenCells.Add(change.CellId))
        {
            return Result.Failure(WorldErrors.DuplicateCellChange);
        }

        MaterialDefinition? material = Materials.Get(change.TargetState.MaterialId);
        if (material is null)
        {
            return Result.Failure(WorldErrors.UnknownMaterial);
        }

        if (change.TargetState.Designation == CellDesignation.Dig && !material.IsSolid)
        {
            return Result.Failure(WorldErrors.InvalidDesignation);
        }

        return Result.Success();
    }

    private WorldMutationResult CreateNoChangeResult()
    {
        return new WorldMutationResult(
            Version,
            changedCellCount: 0,
            Array.Empty<ChunkId>());
    }

    private static void ValidateTick(long tick)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }
    }

    private readonly struct ValidatedChange
    {
        public ValidatedChange(
            CellId cellId,
            CellState previousState,
            CellState currentState)
        {
            CellId = cellId;
            PreviousState = previousState;
            CurrentState = currentState;
        }

        public CellId CellId { get; }

        public CellState PreviousState { get; }

        public CellState CurrentState { get; }
    }
}
