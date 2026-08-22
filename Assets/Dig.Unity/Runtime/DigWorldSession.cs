using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.World;
using Dig.Domain.Core;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Dig.Domain.Runtime;
using Dig.Domain.Exploration;
using Dig.Presentation.Agents;
using Dig.Infrastructure.InMemory;
using Dig.Presentation.World;

namespace Dig.Unity
{

internal sealed partial class DigWorldSession
{
    private const int DefaultDemoGenerationSeed = 1337;
    internal static readonly DomainError ProtectedRock = new DomainError(
        "world.excavation.protected_rock",
        "Protected terrain cannot be excavated.");

    private readonly DesignateDiggingCommandHandler _designationHandler;
    private readonly WorldPresenter _presenter;
    private readonly InMemoryWorldRepository _repository;
    private readonly MaterialId _emptyMaterialId;
    private readonly MaterialId _solidMaterialId;
    private readonly int _solidHardness;
    private readonly ExcavationBoundaryPolicy _boundaryPolicy;
    private readonly TunnelDemoLayout _demoTunnelLayout;
    private readonly SimulationState _simulationState;
    private readonly ExplorationState _exploration;
    private bool _explorationChanged;

    private DigWorldSession(
        DesignateDiggingCommandHandler designationHandler,
        WorldPresenter presenter,
        InMemoryWorldRepository repository,
        MaterialId emptyMaterialId,
        MaterialId solidMaterialId,
        int solidHardness,
        ExcavationBoundaryPolicy boundaryPolicy,
        TunnelDemoLayout demoTunnelLayout,
        ExplorationState exploration,
        InMemoryExecutionJournal journal,
        SimulationState simulationState)
    {
        _designationHandler = designationHandler;
        _presenter = presenter;
        _repository = repository;
        _emptyMaterialId = emptyMaterialId;
        _solidMaterialId = solidMaterialId;
        _solidHardness = solidHardness;
        _boundaryPolicy = boundaryPolicy;
        _demoTunnelLayout = demoTunnelLayout
            ?? throw new ArgumentNullException(nameof(demoTunnelLayout));
        _exploration = exploration ?? throw new ArgumentNullException(nameof(exploration));
        Journal = journal;
        _simulationState = simulationState
            ?? throw new ArgumentNullException(nameof(simulationState));
    }

    public InMemoryExecutionJournal Journal { get; }

    internal InMemoryWorldRepository Repository => _repository;

    internal SimulationState SimulationState => _simulationState;

    internal MaterialId EmptyMaterialId => _emptyMaterialId;

    internal MaterialId SolidMaterialId => _solidMaterialId;

    internal int SolidHardness => _solidHardness;

    internal IReadOnlyList<CellId> ProtectedCells => LoadAllProtectedCells();

    public static DigWorldSession CreateDemo(int width, int height, int chunkSize)
    {
        return CreateDemo(width, height, chunkSize, DefaultDemoGenerationSeed);
    }

    public static DigWorldSession CreateDemo(
        int width,
        int height,
        int chunkSize,
        int generationSeed,
        SimulationState? simulationState = null)
    {
        if (width < 8)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height < 8)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        MaterialId rock = DefaultTerrainMaterials.StoneRock;
        MaterialId air = new MaterialId("demo.air");
        MaterialCatalog terrainMaterials = DefaultTerrainMaterials.CreateCatalog();
        MaterialCatalog materials = new MaterialCatalog(
            terrainMaterials.Definitions.Concat(new[]
            {
                new MaterialDefinition(air, isSolid: false, hardness: 0),
            }));
        int rockHardness = materials.Get(rock)!.Hardness;
        WorldState world = WorldState.CreateFilled(
            new WorldSize(width, height),
            chunkSize,
            materials,
            rock,
            explored: false).Value;
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
        ExplorationState exploration = new ExplorationState();
        DigWorldSession session = new DigWorldSession(
            new DesignateDiggingCommandHandler(repository, journal),
            new WorldPresenter(
                new GetWorldSnapshotQueryHandler(repository),
                exploration.GetVisibility),
            repository,
            air,
            rock,
            rockHardness,
            boundaryPolicy,
            layout,
            exploration,
            journal,
            simulationState ?? SimulationState.Create(
                worldSeed: (ulong)generationSeed,
                tickDuration: GameTimeCadence.NormalTickDuration));
        session.InitializeDemoTunnelPlan(layout);
        session.InitializeNaturalCaveProtection(layout);
        session.InitializeDemoDeposits(generationSeed);
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

    internal TunnelNavigationVolume CreateTunnelNavigationVolume()
    {
        return TunnelNavigationVolume.FromWorldSnapshot(
            LoadSnapshot(),
            PlannedTunnelCells,
            PlannedVerticalTunnelCells,
            _demoTunnelLayout);
    }

    internal Result ExcavateSpatialCell(CellId cell)
    {
        WorldState world = _repository.Get();
        Result<CellSnapshot> current = world.GetCell(cell);
        if (current.IsFailure)
        {
            return Result.Failure(current.Error!);
        }

        // A previous finalize attempt may have committed World before a derived
        // topology/job step failed. Treat the authoritative open cell as success so
        // the retry can finish synchronization and cleanup instead of stalling forever.
        if (!current.Value.IsSolid)
        {
            return Result.Success();
        }

        Result<WorldMutationResult> result = world.Excavate(
            cell,
            _emptyMaterialId,
            _simulationState.Clock.TickIndex);
        return result.IsSuccess
            ? Result.Success()
            : Result.Failure(result.Error!);
    }

    internal Result ActivateCaveRoomVolume(CaveRoomPlan plan)
    {
        if (plan == null)
        {
            throw new ArgumentNullException(nameof(plan));
        }

        WorldState world = _repository.Get();
        List<TerrainChange> changes = new List<TerrainChange>();
        for (int index = 0; index < plan.VolumeCells.Count; index++)
        {
            CellId cell = plan.VolumeCells[index];
            Result<CellSnapshot> current = world.GetCell(cell);
            if (current.IsFailure)
            {
                return Result.Failure(current.Error!);
            }

            if (current.Value.IsSolid)
            {
                changes.Add(new TerrainChange(
                    cell,
                    current.Value.State.WithTerrain(_emptyMaterialId)));
            }
        }

        if (changes.Count == 0)
        {
            return Result.Success();
        }

        Result<WorldMutationResult> result = world.ApplyTerrainChanges(
            changes,
            _simulationState.Clock.TickIndex);
        return result.IsSuccess
            ? Result.Success()
            : Result.Failure(result.Error!);
    }

    internal IReadOnlyList<ChunkId> PeekDirtyChunks()
    {
        return _repository.Get().PeekDirtyChunks();
    }

    internal IReadOnlyList<ChunkId> DrainDirtyChunks()
    {
        return _repository.Get().DrainDirtyChunks();
    }

    internal bool IsProtected(CellId cell)
    {
        return _boundaryPolicy.IsProtected(cell) || IsNaturalCaveProtected(cell);
    }

    public Result ToggleDesignation(WorldCellViewModel cell)
    {
        return SetDesignation(new CellId(cell.X, cell.Y, cell.Z), !cell.IsDesignated);
    }

    internal bool IsExcavationOpen(CellId cell)
    {
        Result<CellSnapshot> snapshot = _repository.Get().GetCell(cell);
        return snapshot.IsSuccess && snapshot.Value.State.IsExcavationOpen;
    }

    internal Result SetDesignation(CellId cell, bool active)
    {
        if (active && IsProtected(cell))
        {
            return Result.Failure(ProtectedRock);
        }

        return _designationHandler.Handle(new DesignateDiggingCommand(
            cell,
            active,
            _simulationState.Clock.TickIndex));
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
            isExplored: false,
            damage: 0,
            temperature: 20);
        HashSet<CellId> airCells = new HashSet<CellId>();
        for (int z = 0; z < tunnel.Depth; z++)
        {
            for (int y = 0; y < layout.SurfaceY; y++)
            {
                for (int x = 0; x < tunnel.Width; x++)
                {
                    airCells.Add(new CellId(x, y, z));
                }
            }
        }

        foreach (CellId cell in tunnel.Cells)
        {
            airCells.Add(cell);
        }

        for (int z = 0; z < tunnel.Depth; z++)
        {
            for (int y = layout.CaveCeilingY + 1; y <= layout.CaveFloorY; y++)
            {
                for (int x = layout.CaveMinX; x <= layout.CaveMaxX; x++)
                {
                    airCells.Add(new CellId(x, y, z));
                }
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
