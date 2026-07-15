using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

            return Result<LoadedGameState>.Success(new LoadedGameState(
                CopyMetadata(document.Metadata),
                world.Value,
                inventory.Value,
                jobs.Value,
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

    private static InventorySnapshot BuildInventorySnapshot(InventorySaveData data)
    {
        if (data is null || data.Stacks is null)
        {
            throw new InvalidOperationException("Inventory save data is missing.");
        }

        List<ItemStackSnapshot> stacks = new List<ItemStackSnapshot>();
        foreach (ItemStackSaveData savedStack in data.Stacks
            .OrderBy(item => item.StackId, StringComparer.Ordinal))
        {
            if (savedStack is null || savedStack.Reservations is null)
            {
                throw new InvalidOperationException("Inventory stack save data is missing.");
            }

            EntityId stackId = EntityId.Parse(savedStack.StackId);
            List<ItemQuantityReservationSnapshot> reservations = savedStack.Reservations
                .OrderBy(item => item.JobId, StringComparer.Ordinal)
                .Select(item => new ItemQuantityReservationSnapshot(
                    EntityId.Parse(item.JobId),
                    item.Quantity))
                .ToList();
            stacks.Add(new ItemStackSnapshot(
                stackId,
                new ItemId(savedStack.ItemId),
                savedStack.Quantity,
                ParseLocation(savedStack.Location),
                reservations));
        }

        return new InventorySnapshot(data.Version, stacks);
    }

    private static ItemLocation ParseLocation(ItemLocationSaveData data)
    {
        if (data is null || !Enum.IsDefined(typeof(ItemLocationKind), data.Kind))
        {
            throw new InvalidOperationException("Item location save data is invalid.");
        }

        ItemLocationKind kind = (ItemLocationKind)data.Kind;
        return kind switch
        {
            ItemLocationKind.World => ItemLocation.InWorld(ParseCell(data)),
            ItemLocationKind.AgentInventory => ItemLocation.InAgent(
                EntityId.Parse(RequireOwner(data))),
            ItemLocationKind.BuildingInventory => ItemLocation.InBuilding(
                EntityId.Parse(RequireOwner(data))),
            ItemLocationKind.Storage => ItemLocation.InStorage(
                EntityId.Parse(RequireOwner(data))),
            ItemLocationKind.Equipped => ItemLocation.EquippedBy(
                EntityId.Parse(RequireOwner(data))),
            _ => throw new InvalidOperationException("Unsupported item location kind."),
        };
    }

    private static CellId ParseCell(ItemLocationSaveData data)
    {
        if (!data.CellX.HasValue || !data.CellY.HasValue || data.OwnerId is not null)
        {
            throw new InvalidOperationException("World item location is malformed.");
        }

        return new CellId(data.CellX.Value, data.CellY.Value);
    }

    private static string RequireOwner(ItemLocationSaveData data)
    {
        if (string.IsNullOrWhiteSpace(data.OwnerId)
            || data.CellX.HasValue
            || data.CellY.HasValue)
        {
            throw new InvalidOperationException("Owned item location is malformed.");
        }

        return data.OwnerId;
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