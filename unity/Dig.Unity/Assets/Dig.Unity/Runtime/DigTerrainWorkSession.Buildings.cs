using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Buildings;
using Dig.Domain.Buildings;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Dig.Presentation.Buildings;
using Dig.Presentation.World;

namespace Dig.Unity
{

internal sealed partial class DigTerrainWorkSession
{
    private static readonly ItemId DemoBuildingBoxItemId =
        new ItemId("demo.building_box.workshop");
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

        BuildingDefinition workshopDefinition = CreateDemoBuildingDefinition();
        BuildingDefinition campfireDefinition =
            CampfireBuildingBoxContent.Definition.Building;
        BuildingCatalog catalog = new BuildingCatalog(new[]
        {
            workshopDefinition,
            campfireDefinition,
        });

        CellId workshopOrigin = FindDemoBuildingOrigin();
        CellId workshopWorkPosition = new CellId(
            workshopOrigin.X,
            workshopOrigin.Y - 1,
            workshopOrigin.Z);
        BuildingSnapshot workshop = CreateCompletedDemoBuilding(
            catalog.Get(workshopDefinition.Id),
            DemoId('b', 1),
            DemoId('c', 1),
            DemoId('d', 1),
            workshopOrigin,
            workshopWorkPosition,
            journal);

        DemoBuildingPlacement campfirePlacement = FindLowerCavePlacement(
            campfireDefinition,
            workshop.Footprint);
        BuildingSnapshot campfire = CreateCompletedDemoBuilding(
            catalog.Get(campfireDefinition.Id),
            DemoId('b', 2),
            DemoId('c', 2),
            DemoId('d', 2),
            campfirePlacement.Origin,
            campfirePlacement.WorkPosition,
            journal);

        BuildingsState buildings = BuildingsState.RestoreWithPacking(
            new[] { workshop, campfire }).Value;
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
        InitializeBuildingBoxWorldInput(catalog, workshopDefinition, journal);
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

    private DemoBuildingPlacement FindLowerCavePlacement(
        BuildingDefinition definition,
        IReadOnlyCollection<CellId> excludedCells)
    {
        Dictionary<CellId, WorldCellViewModel> cells = _worldSession.LoadView().Chunks
            .SelectMany(chunk => chunk.Cells)
            .ToDictionary(
                value => new CellId(value.X, value.Y, value.Z));
        HashSet<CellId> occupied = new HashSet<CellId>(excludedCells);
        foreach (ItemStackSnapshot stack
            in _inventoryRepository.Get().CreateSnapshot().Stacks)
        {
            if (stack.Location.Kind == ItemLocationKind.World
                && stack.Location.HasCell)
            {
                occupied.Add(stack.Location.CellId);
            }
        }

        foreach (CellId origin in cells.Values
            .Where(value => !value.IsSolid && value.Y > 0)
            .Select(value => new CellId(value.X, value.Y, value.Z))
            .Where(value => !occupied.Contains(value))
            .OrderByDescending(value => value.Y)
            .ThenByDescending(value => value.Z)
            .ThenBy(value => value.X))
        {
            CellId below = new CellId(origin.X, origin.Y + 1, origin.Z);
            if (!cells.TryGetValue(below, out WorldCellViewModel belowCell)
                || !belowCell.IsSolid)
            {
                continue;
            }

            CellId[] footprint = definition.ResolveFootprint(
                    origin,
                    BuildingOrientation.North)
                .ToArray();
            if (footprint.Any(value => occupied.Contains(value)
                || !cells.TryGetValue(
                    value,
                    out WorldCellViewModel footprintCell)
                || footprintCell.IsSolid))
            {
                continue;
            }

            CellId? workPosition = definition.ResolveWorkPositions(
                    origin,
                    BuildingOrientation.North)
                .Where(value => !occupied.Contains(value)
                    && cells.TryGetValue(value, out WorldCellViewModel workCell)
                    && !workCell.IsSolid)
                .Select(value => (CellId?)value)
                .FirstOrDefault();
            if (workPosition.HasValue)
            {
                return new DemoBuildingPlacement(origin, workPosition.Value);
            }
        }

        throw new InvalidOperationException(
            "The demo world has no lower cave floor for the completed campfire.");
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

    private CellId FindDemoBuildingOrigin()
    {
        WorldCellViewModel[] openCells = _worldSession.LoadView().Chunks
            .SelectMany(chunk => chunk.Cells)
            .Where(value => !value.IsSolid)
            .ToArray();
        HashSet<CellId> open = new HashSet<CellId>(openCells.Select(
            value => new CellId(value.X, value.Y, value.Z)));
        WorldCellViewModel? cell = openCells
            .Where(value => value.Y > 0
                && open.Contains(new CellId(value.X, value.Y - 1, value.Z)))
            .OrderByDescending(value => value.X)
            .ThenByDescending(value => value.Y)
            .Select(value => (WorldCellViewModel?)value)
            .FirstOrDefault();
        if (!cell.HasValue)
        {
            throw new InvalidOperationException(
                "The demo world has no open building and work-cell pair.");
        }

        return new CellId(cell.Value.X, cell.Value.Y, cell.Value.Z);
    }

    private static BuildingDefinition CreateDemoBuildingDefinition()
    {
        return new BuildingDefinition(
            new BuildingDefinitionId("demo.workshop.box"),
            "Box Workshop",
            new[] { new CellOffset(0, 0) },
            new[] { new CellOffset(0, -1) },
            Array.Empty<BuildingMaterialRequirement>(),
            requiredWork: 3,
            maximumDurability: 100,
            boxPolicy: new BuildingBoxPolicy(DemoBuildingBoxItemId, packingWork: 4));
    }

    private static EntityId DemoId(char prefix, long value)
    {
        return EntityId.Parse(prefix + value.ToString("x31"));
    }
}

}
