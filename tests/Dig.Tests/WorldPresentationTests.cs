using System.Linq;
using Dig.Application.World;
using Dig.Domain.Core;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Dig.Presentation.World;
using Xunit;

namespace Dig.Tests
{

public sealed class WorldPresentationTests
{
    [Fact]
    public void Full_world_query_and_presenter_preserve_stable_order()
    {
        WorldState world = CreateWorld();
        WorldPresenter presenter = CreatePresenter(world);

        WorldViewModel view = presenter.Load();

        Assert.Equal(4, view.Width);
        Assert.Equal(3, view.Height);
        Assert.Equal(4, view.Depth);
        Assert.Equal(2, view.ChunkSize);
        Assert.Equal(world.Version, view.Version);
        Assert.Equal(
            new[]
            {
                "0,0,0", "1,0,0", "0,1,0", "1,1,0",
                "0,0,1", "1,0,1", "0,1,1", "1,1,1",
                "0,0,2", "1,0,2", "0,1,2", "1,1,2",
                "0,0,3", "1,0,3", "0,1,3", "1,1,3",
            },
            view.Chunks.Select(chunk => $"{chunk.X},{chunk.Y},{chunk.Z}").ToArray());
        Assert.All(
            view.Chunks,
            chunk => Assert.Equal(
                chunk.Cells.OrderBy(cell => cell.Z).ThenBy(cell => cell.Y).ThenBy(cell => cell.X).ToArray(),
                chunk.Cells.ToArray()));
    }

    [Fact]
    public void Loading_the_view_does_not_mutate_world_state()
    {
        WorldState world = CreateWorld();
        long version = world.Version;
        int dirtyCount = world.PeekDirtyChunks().Count;
        WorldPresenter presenter = CreatePresenter(world);

        WorldViewModel first = presenter.Load();
        WorldViewModel second = presenter.Load();

        Assert.Equal(version, world.Version);
        Assert.Equal(dirtyCount, world.PeekDirtyChunks().Count);
        Assert.Equal(first.Version, second.Version);
        Assert.NotSame(first, second);
    }

    [Fact]
    public void Application_command_is_reflected_by_the_next_view_model()
    {
        WorldState world = CreateWorld();
        InMemoryWorldRepository repository = new InMemoryWorldRepository(world);
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();
        DesignateDiggingCommandHandler command = new DesignateDiggingCommandHandler(
            repository,
            journal);
        WorldPresenter presenter = new WorldPresenter(
            new GetWorldSnapshotQueryHandler(repository));
        long previousVersion = presenter.Load().Version;

        Result result = command.Handle(new DesignateDiggingCommand(
            new CellId(2, 1),
            designated: true,
            tick: 2));
        WorldViewModel current = presenter.Load();
        WorldCellViewModel cell = current.Chunks
            .SelectMany(chunk => chunk.Cells)
            .Single(value => value.X == 2 && value.Y == 1 && value.Z == 0);

        Assert.True(result.IsSuccess);
        Assert.Equal(previousVersion + 1, current.Version);
        Assert.True(cell.IsDesignated);
        Assert.NotEmpty(journal.Events);
    }

    private static WorldPresenter CreatePresenter(WorldState world)
    {
        return new WorldPresenter(new GetWorldSnapshotQueryHandler(
            new InMemoryWorldRepository(world)));
    }

    private static WorldState CreateWorld()
    {
        MaterialId rock = new MaterialId("demo.rock");
        MaterialId air = new MaterialId("demo.air");
        MaterialCatalog materials = new MaterialCatalog(new[]
        {
            new MaterialDefinition(rock, isSolid: true, hardness: 100),
            new MaterialDefinition(air, isSolid: false, hardness: 0),
        });
        WorldState world = WorldState.CreateFilled(
            new WorldSize(4, 3),
            chunkSize: 2,
            materials,
            rock,
            explored: true).Value;
        CellState empty = new CellState(
            air,
            CellDesignation.None,
            isExplored: true,
            damage: 0,
            temperature: 20);
        Assert.True(world.ApplyTerrainChanges(
            new[] { new TerrainChange(new CellId(1, 1), empty) },
            tick: 1).IsSuccess);
        return world;
    }
}
}
