using System;
using System.Collections.Generic;
using Dig.Application.World;
using Dig.Domain.Core;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Dig.Presentation.World;

namespace Dig.Unity
{
    internal sealed partial class DigWorldSession
    {
        internal static readonly DomainError ProtectedRock = new DomainError(
            "world.excavation.protected_rock",
            "Protected terrain cannot be excavated.");

        private readonly DesignateDiggingCommandHandler _designationHandler;
        private readonly WorldPresenter _presenter;
        private readonly InMemoryWorldRepository _repository;
        private readonly MaterialId _emptyMaterialId;
        private readonly ExcavationBoundaryPolicy _boundaryPolicy;
        private long _tick;

        private DigWorldSession(
            DesignateDiggingCommandHandler designationHandler,
            WorldPresenter presenter,
            InMemoryWorldRepository repository,
            MaterialId emptyMaterialId,
            ExcavationBoundaryPolicy boundaryPolicy,
            InMemoryExecutionJournal journal,
            long tick)
        {
            _designationHandler = designationHandler;
            _presenter = presenter;
            _repository = repository;
            _emptyMaterialId = emptyMaterialId;
            _boundaryPolicy = boundaryPolicy;
            Journal = journal;
            _tick = tick;
        }

        public InMemoryExecutionJournal Journal { get; }

        internal InMemoryWorldRepository Repository => _repository;

        internal MaterialId EmptyMaterialId => _emptyMaterialId;

        internal IReadOnlyList<CellId> ProtectedCells => LoadAllProtectedCells();

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
            TunnelNavigationVolume tunnel = TunnelNavigationVolume.CreateDemo(width, height);
            TunnelDemoLayout layout = tunnel.DemoLayout
                ?? throw new InvalidOperationException("The tunnel demo layout is required.");
            ExcavationBoundaryPolicy boundaryPolicy = new ExcavationBoundaryPolicy(
                width,
                height,
                topRockY: layout.SurfaceY + 1);
            CarveDemoAir(world, air, tunnel);
            world.DequeueUncommittedEvents();

            InMemoryWorldRepository repository = new InMemoryWorldRepository(world);
            InMemoryExecutionJournal journal = new InMemoryExecutionJournal(
                maximumCommands: 100,
                maximumEvents: 500);
            DigWorldSession session = new DigWorldSession(
                new DesignateDiggingCommandHandler(repository, journal),
                new WorldPresenter(new GetWorldSnapshotQueryHandler(repository)),
                repository,
                air,
                boundaryPolicy,
                journal,
                tick: 1);
            session.InitializeDemoTunnelPlan(layout);
            return session;
        }

        public WorldViewModel LoadView()
        {
            return _presenter.Load();
        }

        internal WorldSnapshot LoadSnapshot()
        {
            return _repository.Get().CreateSnapshot();
        }

        internal IReadOnlyList<ChunkId> DrainDirtyChunks()
        {
            return _repository.Get().DrainDirtyChunks();
        }

        internal bool IsProtected(CellId cell)
        {
            return _boundaryPolicy.IsProtected(cell) || IsCaveRoomProtected(cell);
        }

        public Result ToggleDesignation(WorldCellViewModel cell)
        {
            return SetDesignation(new CellId(cell.X, cell.Y), !cell.IsDesignated);
        }

        internal Result SetDesignation(CellId cell, bool active)
        {
            if (active && IsProtected(cell))
            {
                return Result.Failure(ProtectedRock);
            }

            _tick = checked(_tick + 1);
            return _designationHandler.Handle(new DesignateDiggingCommand(
                cell,
                active,
                _tick));
        }

        private static void CarveDemoAir(
            WorldState world,
            MaterialId air,
            TunnelNavigationVolume tunnel)
        {
            TunnelDemoLayout layout = tunnel.DemoLayout
                ?? throw new InvalidOperationException("The tunnel demo layout is required.");
            CellState empty = new CellState(
                air,
                CellDesignation.None,
                isExplored: true,
                damage: 0,
                temperature: 20);
            HashSet<CellId> airCells = new HashSet<CellId>();
            for (int y = 0; y < layout.SurfaceY; y++)
            {
                for (int x = 0; x < tunnel.Width; x++)
                {
                    airCells.Add(new CellId(x, y));
                }
            }

            foreach (SpatialCellId cell in tunnel.Cells)
            {
                if (cell.Z == 0)
                {
                    airCells.Add(cell.Projection);
                }
            }

            for (int y = layout.CaveCeilingY + 1; y <= layout.CaveFloorY; y++)
            {
                for (int x = layout.CaveMinX; x <= layout.CaveMaxX; x++)
                {
                    airCells.Add(new CellId(x, y));
                }
            }

            List<TerrainChange> changes = new List<TerrainChange>(airCells.Count);
            foreach (CellId cell in airCells)
            {
                changes.Add(new TerrainChange(cell, empty));
            }

            Result<WorldMutationResult> result = world.ApplyTerrainChanges(changes, tick: 1);
            if (result.IsFailure)
            {
                throw new InvalidOperationException(result.Error!.ToString());
            }
        }
    }
}