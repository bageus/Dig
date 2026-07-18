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
        return Load(document, materials, items, buildingCatalog: null);
    }

    public Result<LoadedGameState> Load(
        SaveGameDocument document,
        MaterialCatalog materials,
        ItemCatalog items,
        BuildingCatalog? buildingCatalog)
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

            return Result<LoadedGameState>.Success(new LoadedGameState(
                CopyMetadata(document.Metadata),
                world.Value,
                inventory.Value,
                jobs.Value,
                buildings.Value,
                migration.Value));
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

        WorldSize size = new WorldSize(data.Width, data.Height);
        ChunkLayout layout = new ChunkLayout(size, data.ChunkSize);
        List<ChunkSnapshot> chunks = new List<ChunkSnapshot>();
        foreach (WorldChunkSaveData savedChunk in data.Chunks
            .OrderBy(item => item.Y)
            .ThenBy(item => item.X))
        {
            if (savedChunk is null || savedChunk.Cells is null)
            {
                throw new InvalidOperationException("World chunk save data is missing.");
            }

            ChunkId chunkId = new ChunkId(savedChunk.X, savedChunk.Y);
            List<CellSnapshot> cells = new List<CellSnapshot>();
            foreach (WorldCellSaveData savedCell in savedChunk.Cells
                .OrderBy(item => item.Y)
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
                    new CellId(savedCell.X, savedCell.Y),
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

    private static Result ValidateCrossReferences(
        InventoryState inventory,
        JobSystem jobs)
    {
        foreach (ItemStackSnapshot stack in inventory.CreateSnapshot().Stacks)
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
