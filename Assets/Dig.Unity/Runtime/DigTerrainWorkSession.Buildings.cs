using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Buildings;
using Dig.Domain.Buildings;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Dig.Presentation.Buildings;
using Dig.Presentation.World;

namespace Dig.Unity
{

internal sealed partial class DigTerrainWorkSession
{
    private const int DemoCompletedBuildingDepth = 1;
    private static readonly ItemId DemoResidentToolItemId =
        new ItemId("demo.tool.pickaxe");
    private static readonly ItemId DemoResidentHammerItemId =
        new ItemId("demo.tool.hammer");
    private static readonly ItemId DemoBasketItemId =
        ResidentInventoryExpansionContent.BasketItemId;
    private static readonly ItemId DemoLargeBasketItemId =
        ResidentInventoryExpansionContent.LargeBasketItemId;
    private static readonly ItemId DemoScabbardItemId =
        ResidentInventoryExpansionContent.SheathItemId;
    private static readonly ItemId DemoHarnessItemId =
        ResidentInventoryExpansionContent.WeaponHarnessItemId;
    private static readonly DomainError BuildingsNotInitialized = new DomainError(
        "unity.buildings.not_initialized",
        "The demo building runtime is not initialized.");

    private InMemoryBuildingsRepository? _buildingsRepository;
    private InMemoryInventoryRepository? _buildingInventoryRepository;
    private BuildingWorldPresenter? _buildingPresenter;
    private BuildingFunctionsCommandAdapter? _buildingCommands;
    private long _nextPackingSequence;

    public void InitializeBuildingDemo(InMemoryExecutionJournal journal)
    {
        if (journal == null)
        {
            throw new ArgumentNullException(nameof(journal));
        }

        if (_buildingsRepository != null)
        {
            return;
        }

        BuildingDefinition campfireDefinition =
            CampfireBuildingBoxContent.Definition.Building;
        BuildingCatalog catalog = new BuildingCatalog(
            CampfireProductionContent.CreateBuildings()
                .GroupBy(value => value.Id)
                .Select(group => group.First()));

        DemoBuildingPlacement campfirePlacement = FindSurfaceCampfirePlacement(
            campfireDefinition,
            Array.Empty<CellId>());
        BuildingSnapshot campfire = CreateCompletedDemoBuilding(
            catalog.Get(campfireDefinition.Id),
            DemoId('b', 1),
            DemoId('c', 1),
            DemoId('d', 1),
            campfirePlacement.Origin,
            campfirePlacement.WorkPosition,
            journal);

        BuildingsState buildings = BuildingsState.RestoreWithPacking(
            new[] { campfire }).Value;
        _buildingsRepository = new InMemoryBuildingsRepository(buildings);
        _buildingInventoryRepository = _inventoryRepository;

        BuildingFunctionsPresenter functions = new BuildingFunctionsPresenter();
        _buildingPresenter = new BuildingWorldPresenter(functions);
        _buildingCommands = new BuildingFunctionsCommandAdapter(
            functions,
            new StartBuildingBoxPackingHandler(
                _buildingsRepository,
                _buildingInventoryRepository,
                _jobRepository,
                journal));
        InitializeBuildingPackingExecution(journal);
        InitializeBuildingBoxWorldInput(catalog, campfireDefinition, journal);
    }

    public IReadOnlyList<BuildingWorldViewModel> LoadBuildings()
    {
        if (_buildingsRepository == null || _buildingPresenter == null)
        {
            return Array.Empty<BuildingWorldViewModel>();
        }

        return _buildingPresenter.Load(_buildingsRepository.Get().GetAll());
    }

    public Result StartBuildingPacking(string buildingId, long tick)
    {
        if (_buildingsRepository == null || _buildingCommands == null)
        {
            return Result.Failure(BuildingsNotInitialized);
        }

        if (string.IsNullOrWhiteSpace(buildingId))
        {
            throw new ArgumentException("Building id is required.", nameof(buildingId));
        }

        EntityId id = EntityId.Parse(buildingId);
        BuildingSnapshot? snapshot = _buildingsRepository.Get().Get(id);
        if (snapshot == null)
        {
            return Result.Failure(BuildingErrors.NotFound);
        }

        long sequence = checked(_nextPackingSequence + 1);
        _nextPackingSequence = sequence;
        return _buildingCommands.StartPacking(
            snapshot,
            DemoId('e', sequence),
            DemoId('f', sequence),
            priority: 650,
            tick: tick);
    }

    private BuildingSnapshot CreateCompletedDemoBuilding(
        BuildingDefinition definition,
        EntityId buildingId,
        EntityId sourceStackId,
        EntityId assemblyJobId,
        CellId origin,
        CellId workPosition,
        InMemoryExecutionJournal journal)
    {
        CreateCompletedAssemblyJob(
            buildingId,
            sourceStackId,
            assemblyJobId,
            origin,
            workPosition,
            journal);
        return new BuildingSnapshot(
            buildingId,
            definition,
            origin,
            BuildingOrientation.North,
            definition.ResolveFootprint(origin, BuildingOrientation.North),
            workPosition,
            BuildingStatus.Completed,
            definition.RequiredWork,
            definition.MaximumDurability,
            version: 1,
            diagnosticReason: null,
            boxPlan: new BuildingBoxPlanSnapshot(
                sourceStackId,
                assemblyJobId,
                BuildingBoxCommitState.Consumed));
    }

    private void CreateCompletedAssemblyJob(
        EntityId buildingId,
        EntityId sourceStackId,
        EntityId assemblyJobId,
        CellId origin,
        CellId workPosition,
        InMemoryExecutionJournal journal)
    {
        JobSystem jobs = _jobRepository.Get();
        BuildingBoxAssemblyJobDefinition definition =
            new BuildingBoxAssemblyJobDefinition(
                assemblyJobId,
                buildingId,
                sourceStackId,
                origin,
                workPosition,
                priority: 600,
                createdTick: 0,
                retryPolicy: JobRetryPolicy.Default);
        Require(jobs.Add(definition));
        Require(jobs.MakeAvailable(assemblyJobId, tick: 0));
        Require(jobs.Claim(assemblyJobId, DemoId('a', 1), tick: 0));
        Require(jobs.Start(assemblyJobId, tick: 0));
        Require(jobs.Complete(assemblyJobId, tick: 0));
        _jobRepository.Save(jobs);
        journal.Append(jobs.DequeueUncommittedEvents());
    }

    private DemoBuildingPlacement FindSurfaceCampfirePlacement(
        BuildingDefinition definition,
        IReadOnlyCollection<CellId> excludedCells)
    {
        TunnelDemoLayout layout = _worldSession.CreateTunnelNavigationVolume().DemoLayout
            ?? throw new InvalidOperationException("The demo tunnel layout is required.");
        Dictionary<CellId, WorldCellViewModel> cells = _worldSession.LoadView().Chunks
            .SelectMany(chunk => chunk.Cells)
            .ToDictionary(value => new CellId(value.X, value.Y, value.Z));
        HashSet<CellId> occupied = new HashSet<CellId>(excludedCells);
        foreach (ItemStackSnapshot stack in _inventoryRepository.Get().CreateSnapshot().Stacks)
        {
            if (stack.Location.Kind == ItemLocationKind.World && stack.Location.HasCell)
            {
                occupied.Add(stack.Location.CellId);
            }
        }

        CellId origin = new CellId(
            layout.ShaftX - 2,
            layout.SurfaceY,
            DemoCompletedBuildingDepth);
        CellId[] footprint = definition.ResolveFootprint(
                origin,
                BuildingOrientation.North)
            .ToArray();
        bool validFootprint = footprint.All(value =>
        {
            CellId support = new CellId(value.X, value.Y + 1, value.Z);
            return !occupied.Contains(value)
                && cells.TryGetValue(value, out WorldCellViewModel footprintCell)
                && !footprintCell.IsSolid
                && cells.TryGetValue(support, out WorldCellViewModel supportCell)
                && supportCell.IsSolid;
        });
        if (!validFootprint)
        {
            throw new InvalidOperationException(
                "The surface campfire Z1 anchor two cells left of the shaft is invalid.");
        }

        CellId? workPosition = definition.ResolveWorkPositions(
                origin,
                BuildingOrientation.North)
            .Where(value => !occupied.Contains(value)
                && !footprint.Contains(value)
                && value.Y == origin.Y
                && value.Z == origin.Z
                && cells.TryGetValue(value, out WorldCellViewModel workCell)
                && !workCell.IsSolid
                && cells.TryGetValue(
                    new CellId(value.X, value.Y + 1, value.Z),
                    out WorldCellViewModel supportCell)
                && supportCell.IsSolid)
            .OrderBy(value => Math.Abs(value.X - origin.X))
            .ThenBy(value => value)
            .Select(value => (CellId?)value)
            .FirstOrDefault();
        if (!workPosition.HasValue)
        {
            throw new InvalidOperationException(
                "The surface campfire has no valid Z1 work position.");
        }

        return new DemoBuildingPlacement(origin, workPosition.Value);
    }

    private readonly struct DemoBuildingPlacement
    {
        internal DemoBuildingPlacement(CellId origin, CellId workPosition)
        {
            Origin = origin;
            WorkPosition = workPosition;
        }

        internal CellId Origin { get; }
        internal CellId WorkPosition { get; }
    }

    private static EntityId DemoId(char prefix, long value)
    {
        return EntityId.Parse(prefix + value.ToString("x31"));
    }
}

}
