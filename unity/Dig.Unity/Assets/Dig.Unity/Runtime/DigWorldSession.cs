using System;
using System.Collections.Generic;
using Dig.Application.World;
using Dig.Domain.Core;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Dig.Presentation.World;

namespace Dig.Unity
{
    internal sealed class DigWorldSession
    {
        private readonly DesignateDiggingCommandHandler _designationHandler;
        private readonly WorldPresenter _presenter;
        private long _tick;

        private DigWorldSession(
            DesignateDiggingCommandHandler designationHandler,
            WorldPresenter presenter,
            InMemoryExecutionJournal journal,
            long tick)
        {
            _designationHandler = designationHandler;
            _presenter = presenter;
            Journal = journal;
            _tick = tick;
        }

        public InMemoryExecutionJournal Journal { get; }

        public static DigWorldSession CreateDemo(int width, int height, int chunkSize)
        {
            if (width < 8)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            if (height < 8)
            {
                throw new ArgumentOutOfRangeException(nameof(height));
            }

            MaterialId rock = new MaterialId("demo.rock");
            MaterialId air = new MaterialId("demo.air");
            MaterialCatalog materials = new MaterialCatalog(new[]
            {
                new MaterialDefinition(rock, isSolid: true, hardness: 120),
                new MaterialDefinition(air, isSolid: false, hardness: 0),
            });
            WorldState world = WorldState.CreateFilled(
                new WorldSize(width, height),
                chunkSize,
                materials,
                rock,
                explored: true).Value;
            CarveDemoCavern(world, air, width, height);
            world.SetDigDesignation(new CellId(width - 1, 3), true, tick: 2);
            world.SetDigDesignation(new CellId(width - 1, 4), true, tick: 3);
            world.DequeueUncommittedEvents();

            InMemoryWorldRepository repository = new InMemoryWorldRepository(world);
            InMemoryExecutionJournal journal = new InMemoryExecutionJournal(
                maximumCommands: 100,
                maximumEvents: 500);
            return new DigWorldSession(
                new DesignateDiggingCommandHandler(repository, journal),
                new WorldPresenter(new GetWorldSnapshotQueryHandler(repository)),
                journal,
                tick: 3);
        }

        public WorldViewModel LoadView()
        {
            return _presenter.Load();
        }

        public Result ToggleDesignation(WorldCellViewModel cell)
        {
            _tick = checked(_tick + 1);
            return _designationHandler.Handle(new DesignateDiggingCommand(
                new CellId(cell.X, cell.Y),
                !cell.IsDesignated,
                _tick));
        }

        private static void CarveDemoCavern(
            WorldState world,
            MaterialId air,
            int width,
            int height)
        {
            CellState empty = new CellState(
                air,
                CellDesignation.None,
                isExplored: true,
                damage: 0,
                temperature: 20);
            List<TerrainChange> changes = new List<TerrainChange>();
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    if (IsRockFeature(x, y, width, height))
                    {
                        continue;
                    }

                    changes.Add(new TerrainChange(new CellId(x, y), empty));
                }
            }

            Result<WorldMutationResult> result = world.ApplyTerrainChanges(changes, tick: 1);
            if (result.IsFailure)
            {
                throw new InvalidOperationException(result.Error!.ToString());
            }
        }

        private static bool IsRockFeature(int x, int y, int width, int height)
        {
            bool centralPillar = x == width / 2 && y >= 3 && y <= height - 4;
            bool leftPillar = x == width / 3 && y == height / 2;
            bool rightPillar = x == (width * 2) / 3 && y == (height / 2) + 1;
            return centralPillar || leftPillar || rightPillar;
        }
    }
}
