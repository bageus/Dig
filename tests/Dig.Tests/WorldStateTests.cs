using Dig.Application.Messaging;
using Dig.Application.World;
using Dig.Domain.Core;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Xunit;

namespace Dig.Tests;

public sealed class WorldStateTests
{
    private static readonly MaterialId Rock = new MaterialId("rock");
    private static readonly MaterialId Air = new MaterialId("air");

    [Fact]
    public void Interior_change_invalidates_only_owner_chunk()
    {
        WorldState world = CreateWorld();

        Result<WorldMutationResult> result = world.SetDigDesignation(
            new CellId(1, 1),
            designated: true,
            tick: 10);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.WorldVersion);
        Assert.Equal(1, result.Value.ChangedCellCount);
        Assert.Equal(new[] { new ChunkId(0, 0) }, result.Value.InvalidatedChunks);
        Assert.Equal(result.Value.InvalidatedChunks, world.PeekDirtyChunks());
        Assert.Equal(1, world.GetChunkVersion(new ChunkId(0, 0)).Value);
        Assert.Equal(0, world.GetChunkVersion(new ChunkId(1, 0)).Value);
        Assert.IsType<CellChanged>(world.PeekUncommittedEvents()[0]);
        Assert.IsType<ChunkInvalidated>(world.PeekUncommittedEvents()[1]);
    }

    [Fact]
    public void Cell_on_two_chunk_boundaries_invalidates_four_chunks()
    {
        WorldState world = CreateWorld();

        Result<WorldMutationResult> result = world.SetDigDesignation(
            new CellId(3, 3),
            designated: true,
            tick: 1);

        Assert.True(result.IsSuccess);
        Assert.Equal(
            new[]
            {
                new ChunkId(0, 0),
                new ChunkId(1, 0),
                new ChunkId(0, 1),
                new ChunkId(1, 1),
            },
            result.Value.InvalidatedChunks);
        Assert.Equal(4, world.PeekUncommittedEvents().OfType<ChunkInvalidated>().Count());
    }

    [Fact]
    public void Invalid_batch_is_rejected_without_partial_mutation()
    {
        WorldState world = CreateWorld();
        CellId validCell = new CellId(1, 1);
        CellState previous = world.GetCell(validCell).Value.State;
        CellState changed = previous.WithDesignation(CellDesignation.Dig);

        Result<WorldMutationResult> result = world.ApplyTerrainChanges(
            new[]
            {
                new TerrainChange(validCell, changed),
                new TerrainChange(new CellId(99, 99), changed),
            },
            tick: 2);

        Assert.True(result.IsFailure);
        Assert.Equal(WorldErrors.CellOutOfBounds, result.Error);
        Assert.Equal(0, world.Version);
        Assert.Equal(previous, world.GetCell(validCell).Value.State);
        Assert.Empty(world.PeekDirtyChunks());
        Assert.Empty(world.PeekUncommittedEvents());
    }

    [Fact]
    public void Duplicate_cell_in_batch_is_rejected_atomically()
    {
        WorldState world = CreateWorld();
        CellId cellId = new CellId(1, 1);
        CellState changed = world.GetCell(cellId).Value.State
            .WithDesignation(CellDesignation.Dig);

        Result<WorldMutationResult> result = world.ApplyTerrainChanges(
            new[]
            {
                new TerrainChange(cellId, changed),
                new TerrainChange(cellId, changed),
            },
            tick: 2);

        Assert.True(result.IsFailure);
        Assert.Equal(WorldErrors.DuplicateCellChange, result.Error);
        Assert.Equal(0, world.Version);
        Assert.Equal(CellDesignation.None, world.GetCell(cellId).Value.State.Designation);
    }

    [Fact]
    public void Bulk_changes_increment_world_once_and_deduplicate_chunks()
    {
        WorldState world = CreateWorld();
        CellId first = new CellId(1, 1);
        CellId second = new CellId(2, 1);

        Result<WorldMutationResult> result = world.ApplyTerrainChanges(
            new[]
            {
                DigDesignation(world, second),
                DigDesignation(world, first),
            },
            tick: 3);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, world.Version);
        Assert.Equal(2, result.Value.ChangedCellCount);
        Assert.Equal(new[] { new ChunkId(0, 0) }, result.Value.InvalidatedChunks);
        Assert.Equal(2, world.PeekUncommittedEvents().OfType<CellChanged>().Count());
        Assert.Single(world.PeekUncommittedEvents().OfType<ChunkInvalidated>());
        CellChanged firstEvent = world.PeekUncommittedEvents()
            .OfType<CellChanged>()
            .First();
        Assert.Equal(first, firstEvent.CellId);
    }

    [Fact]
    public void Snapshot_remains_immutable_after_world_changes()
    {
        WorldState world = CreateWorld();
        CellId target = new CellId(1, 1);
        ChunkSnapshot before = world.CreateChunkSnapshot(new ChunkId(0, 0)).Value;

        Result<WorldMutationResult> excavation = world.Excavate(target, Air, tick: 4);
        ChunkSnapshot after = world.CreateChunkSnapshot(new ChunkId(0, 0)).Value;

        Assert.True(excavation.IsSuccess);
        CellSnapshot beforeCell = before.Cells.Single(cell => cell.Id == target);
        CellSnapshot afterCell = after.Cells.Single(cell => cell.Id == target);
        Assert.True(beforeCell.IsSolid);
        Assert.Equal(0, before.WorldVersion);
        Assert.Equal(0, before.ChunkVersion);
        Assert.False(afterCell.IsSolid);
        Assert.Equal(Air, afterCell.State.MaterialId);
        Assert.Equal(1, after.WorldVersion);
        Assert.Equal(1, after.ChunkVersion);
    }

    [Fact]
    public void Dirty_chunks_can_be_drained_and_full_snapshot_rebuilt()
    {
        WorldState world = CreateWorld(width: 10, height: 6, chunkSize: 4);
        world.SetDigDesignation(new CellId(3, 3), designated: true, tick: 5);

        IReadOnlyList<ChunkId> drained = world.DrainDirtyChunks();
        WorldSnapshot snapshot = world.CreateSnapshot();

        Assert.NotEmpty(drained);
        Assert.Empty(world.PeekDirtyChunks());
        Assert.Equal(6, snapshot.Chunks.Count);
        Assert.Equal(60, snapshot.Chunks.Sum(chunk => chunk.Cells.Count));
        Assert.Equal(world.Version, snapshot.Version);
    }

    [Fact]
    public void Empty_cell_cannot_receive_dig_designation()
    {
        WorldState world = CreateWorld(fillMaterial: Air);

        Result<WorldMutationResult> result = world.SetDigDesignation(
            new CellId(0, 0),
            designated: true,
            tick: 6);

        Assert.True(result.IsFailure);
        Assert.Equal(WorldErrors.DigDesignationRequiresSolidCell, result.Error);
        Assert.Equal(0, world.Version);
    }

    [Fact]
    public void Application_commands_publish_facts_and_query_is_read_only()
    {
        WorldState world = CreateWorld();
        InMemoryWorldRepository repository = new InMemoryWorldRepository(world);
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();
        CommandPipeline commands = new CommandPipeline(journal);
        QueryPipeline queries = new QueryPipeline();
        DesignateDiggingCommandHandler commandHandler =
            new DesignateDiggingCommandHandler(repository, journal);
        GetCellQueryHandler queryHandler = new GetCellQueryHandler(repository);
        CellId target = new CellId(1, 1);

        Result commandResult = commands.Execute(
            new DesignateDiggingCommand(target, designated: true, tick: 7),
            commandHandler,
            tick: 7);
        long versionAfterCommand = world.Version;
        Result<CellSnapshot> queryResult = queries.Execute(
            new GetCellQuery(target),
            queryHandler);

        Assert.True(commandResult.IsSuccess);
        Assert.True(queryResult.IsSuccess);
        Assert.Equal(CellDesignation.Dig, queryResult.Value.State.Designation);
        Assert.Equal(versionAfterCommand, world.Version);
        Assert.Single(journal.Commands);
        Assert.Contains(journal.Events, item => item is CellChanged);
        Assert.Contains(journal.Events, item => item is ChunkInvalidated);
    }

    private static TerrainChange DigDesignation(WorldState world, CellId cellId)
    {
        CellState target = world.GetCell(cellId).Value.State
            .WithDesignation(CellDesignation.Dig);
        return new TerrainChange(cellId, target);
    }

    private static WorldState CreateWorld(
        int width = 8,
        int height = 8,
        int chunkSize = 4,
        MaterialId? fillMaterial = null)
    {
        MaterialCatalog materials = new MaterialCatalog(
            new[]
            {
                new MaterialDefinition(Rock, isSolid: true, hardness: 100),
                new MaterialDefinition(Air, isSolid: false, hardness: 0),
            });
        Result<WorldState> result = WorldState.CreateFilled(
            new WorldSize(width, height),
            chunkSize,
            materials,
            fillMaterial ?? Rock,
            explored: true);
        Assert.True(result.IsSuccess);
        return result.Value;
    }
}
