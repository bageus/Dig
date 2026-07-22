using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;

namespace Dig.Application.Saving
{

public sealed partial class SaveGameLoader
{
    private readonly SaveMigrationPipeline _migrations;
    private readonly JobDefinitionSaveRegistry _jobDefinitions;

    public SaveGameLoader(
        SaveMigrationPipeline migrations,
        JobDefinitionSaveRegistry jobDefinitions)
    {
        _migrations = migrations ?? throw new ArgumentNullException(nameof(migrations));
        _jobDefinitions = jobDefinitions
            ?? throw new ArgumentNullException(nameof(jobDefinitions));
    }

    public Result<LoadedGameState> Load(
        SaveGameDocument document,
        MaterialCatalog materials,
        ItemCatalog items)
    {
        return Load(
            document,
            materials,
            items,
            buildingCatalog: null,
            terrainDepositCatalog: null);
    }

    public Result<LoadedGameState> Load(
        SaveGameDocument document,
        MaterialCatalog materials,
        ItemCatalog items,
        BuildingCatalog? buildingCatalog)
    {
        return Load(
            document,
            materials,
            items,
            buildingCatalog,
            terrainDepositCatalog: null);
    }

    public Result<LoadedGameState> Load(
        SaveGameDocument document,
        MaterialCatalog materials,
        ItemCatalog items,
        BuildingCatalog? buildingCatalog,
        TerrainDepositCatalog? terrainDepositCatalog)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        try
        {
            Result<SaveMigrationReport> migration = _migrations.Apply(document);
            if (migration.IsFailure)
            {
                return Result<LoadedGameState>.Failure(migration.Error!);
            }

            List<string> migrationSteps = migration.Value.AppliedSteps.ToList();
            migrationSteps.AddRange(
                AgentSkillSaveDataMigrator.Apply(document.AgentSkills));
            SaveMigrationReport migrationReport = new SaveMigrationReport(
                migrationSteps);

            ValidateMetadata(document.Metadata);
            Result<WorldState> world = WorldState.Restore(
                BuildWorldSnapshot(document.World, materials),
                materials);
            if (world.IsFailure)
            {
                return Result<LoadedGameState>.Failure(world.Error!);
            }

            Result<InventoryState> inventory = InventoryState.Restore(
                BuildInventorySnapshot(document.Inventory),
                items);
            if (inventory.IsFailure)
            {
                return Result<LoadedGameState>.Failure(inventory.Error!);
            }

            Result<JobSystem> jobs = BuildJobSystem(document.Jobs);
            if (jobs.IsFailure)
            {
                return Result<LoadedGameState>.Failure(jobs.Error!);
            }

            Result<BuildingsState> buildings = BuildBuildingsState(
                document.Buildings,
                buildingCatalog);
            if (buildings.IsFailure)
            {
                return Result<LoadedGameState>.Failure(buildings.Error!);
            }

            Result references = ValidateCrossReferences(inventory.Value, jobs.Value);
            if (references.IsFailure)
            {
                return Result<LoadedGameState>.Failure(references.Error!);
            }

            references = ValidateBuildingReferences(
                inventory.Value,
                jobs.Value,
                buildings.Value);
            if (references.IsFailure)
            {
                return Result<LoadedGameState>.Failure(references.Error!);
            }

            IReadOnlyDictionary<EntityId, Dig.Domain.Agents.AgentSkillProgressionSnapshot>
                agentSkills = BuildAgentSkills(document.AgentSkills);
            IReadOnlyDictionary<EntityId, bool> agentAutomaticPlanning =
                BuildAgentAutomaticPlanning(document.AgentSkills);
            IReadOnlyDictionary<EntityId, CellId> agentPositions =
                BuildAgentPositions(document.AgentPositions, document.World);
            IReadOnlyCollection<TerrainDepositInstance> terrainDeposits =
                BuildTerrainDeposits(
                    document.TerrainDeposits,
                    document.World,
                    terrainDepositCatalog);

            return Result<LoadedGameState>.Success(new LoadedGameState(
                CopyMetadata(document.Metadata),
                world.Value,
                inventory.Value,
                jobs.Value,
                buildings.Value,
                migrationReport,
                agentSkills,
                agentAutomaticPlanning,
                agentPositions,
                terrainDeposits));
        }
        catch (UnknownTerrainDepositDefinitionException)
        {
            return Result<LoadedGameState>.Failure(
                SaveErrors.UnknownTerrainDepositDefinition);
        }
        catch (KeyNotFoundException)
        {
            return Result<LoadedGameState>.Failure(SaveErrors.UnknownJobType);
        }
        catch (Exception exception) when (
            exception is ArgumentException
            || exception is InvalidOperationException
            || exception is FormatException
            || exception is OverflowException)
        {
            return Result<LoadedGameState>.Failure(SaveErrors.InvalidDocument);
        }
    }

    private static WorldSnapshot BuildWorldSnapshot(
        WorldSaveData data,
        MaterialCatalog materials)
    {
        if (data is null || data.Chunks is null)
        {
            throw new InvalidOperationException("World save data is missing.");
        }

        WorldSize size = new WorldSize(data.Width, data.Height, data.Depth);
        ChunkLayout layout = new ChunkLayout(size, data.ChunkSize);
        List<ChunkSnapshot> chunks = new List<ChunkSnapshot>();
        foreach (WorldChunkSaveData savedChunk in data.Chunks
            .OrderBy(item => item.Z)
            .ThenBy(item => item.Y)
            .ThenBy(item => item.X))
        {
            if (savedChunk is null || savedChunk.Cells is null)
            {
                throw new InvalidOperationException("World chunk save data is missing.");
            }

            ChunkId chunkId = new ChunkId(savedChunk.X, savedChunk.Y, savedChunk.Z);
            List<CellSnapshot> cells = new List<CellSnapshot>();
            foreach (WorldCellSaveData savedCell in savedChunk.Cells
                .OrderBy(item => item.Z)
                .ThenBy(item => item.Y)
                .ThenBy(item => item.X))
            {
                if (savedCell is null
                    || !Enum.IsDefined(typeof(CellDesignation), savedCell.Designation))
                {
                    throw new InvalidOperationException("World cell save data is invalid.");
                }

                MaterialId materialId = new MaterialId(savedCell.MaterialId);
                MaterialDefinition material = materials.Get(materialId)
                    ?? throw new InvalidOperationException(
                        $"Unknown saved material '{materialId}'.");
                CellState state = new CellState(
                    materialId,
                    (CellDesignation)savedCell.Designation,
                    savedCell.IsExplored,
                    savedCell.Damage,
                    savedCell.Temperature);
                cells.Add(new CellSnapshot(
                    new CellId(savedCell.X, savedCell.Y, savedCell.Z),
                    state,
                    material.IsSolid,
                    material.Hardness,
                    data.Version));
            }

            chunks.Add(new ChunkSnapshot(
                chunkId,
                layout.GetBounds(chunkId),
                data.Version,
                savedChunk.Version,
                cells));
        }

        return new WorldSnapshot(
            size,
            data.ChunkSize,
            data.Version,
            new ReadOnlyCollection<ChunkSnapshot>(chunks));
    }


    private static IReadOnlyDictionary<EntityId, CellId> BuildAgentPositions(
        AgentPositionsSaveData data,
        WorldSaveData world)
    {
        if (data is null || data.Agents is null)
        {
            throw new InvalidOperationException("Agent positions save data is missing.");
        }

        WorldSize size = new WorldSize(world.Width, world.Height, world.Depth);
        Dictionary<EntityId, CellId> result = new Dictionary<EntityId, CellId>();
        foreach (AgentPositionSaveData saved in data.Agents
            .OrderBy(value => value.AgentId, StringComparer.Ordinal))
        {
            if (saved is null)
            {
                throw new InvalidOperationException("Agent position entry is missing.");
            }

            EntityId id = EntityId.Parse(saved.AgentId);
            CellId position = new CellId(saved.X, saved.Y, saved.Z);
            if (!size.Contains(position) || !result.TryAdd(id, position))
            {
                throw new InvalidOperationException("Agent position is invalid or duplicated.");
            }
        }

        return new ReadOnlyDictionary<EntityId, CellId>(result);
    }

    private static Result ValidateCrossReferences(
        InventoryState inventory,
        JobSystem jobs)
    {
        InventorySnapshot snapshot = inventory.CreateSnapshot();
        foreach (ItemStackSnapshot stack in snapshot.Stacks)
        {
            foreach (ItemQuantityReservationSnapshot reservation in stack.Reservations)
            {
                JobSnapshot? job = jobs.Get(reservation.JobId);
                if (job is null || job.IsTerminal)
                {
                    return Result.Failure(SaveErrors.InvalidDocument);
                }
            }
        }

        foreach (IGrouping<EntityId, ResidentInventorySlotClaimSnapshot> group
            in snapshot.ResidentSlotClaims.GroupBy(claim => claim.JobId))
        {
            JobSnapshot? job = jobs.Get(group.Key);
            ResidentInventorySlotClaimSnapshot[] claims = group.ToArray();
            if (job is null
                || job.IsTerminal
                || job.Definition is not HaulJobDefinition hauling
                || !job.AssignedAgentId.HasValue
                || (job.Status != JobStatus.Claimed
                    && job.Status != JobStatus.InProgress)
                || claims.Any(claim => claim.ResidentId != job.AssignedAgentId.Value)
                || claims.Any(claim => claim.ItemId != hauling.ItemId)
                || claims.Sum(claim => claim.Quantity) != hauling.Quantity)
            {
                return Result.Failure(SaveErrors.InvalidDocument);
            }
        }

        return Result.Success();
    }

    private static void ValidateMetadata(SaveMetadataData metadata)
    {
        if (metadata is null
            || string.IsNullOrWhiteSpace(metadata.SlotId)
            || string.IsNullOrWhiteSpace(metadata.DisplayName)
            || string.IsNullOrWhiteSpace(metadata.SavedAtUtc)
            || metadata.SimulationTick < 0
            || metadata.GeneratorVersion <= 0)
        {
            throw new InvalidOperationException("Save metadata is invalid.");
        }
    }

    private static SaveMetadataData CopyMetadata(SaveMetadataData metadata)
    {
        return new SaveMetadataData
        {
            SlotId = metadata.SlotId,
            DisplayName = metadata.DisplayName,
            SavedAtUtc = metadata.SavedAtUtc,
            SimulationTick = metadata.SimulationTick,
            WorldSeed = metadata.WorldSeed,
            GeneratorVersion = metadata.GeneratorVersion,
        };
    }
}

}
