using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Buildings;
using Dig.Domain.Buildings;
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

            BuildingDefinition definition = CreateDemoBuildingDefinition();
            BuildingCatalog catalog = new BuildingCatalog(new[] { definition });
            CellId origin = FindDemoBuildingOrigin();
            CellId workPosition = new CellId(origin.X, origin.Y - 1);
            EntityId buildingId = DemoId('b', 1);
            EntityId sourceStackId = DemoId('c', 1);
            EntityId assemblyJobId = DemoId('d', 1);
            CreateCompletedAssemblyJob(
                buildingId,
                sourceStackId,
                assemblyJobId,
                origin,
                workPosition,
                journal);

            BuildingSnapshot snapshot = new BuildingSnapshot(
                buildingId,
                catalog.Get(definition.Id),
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
            BuildingsState buildings = BuildingsState.RestoreWithPacking(
                new[] { snapshot }).Value;
            _buildingsRepository = new InMemoryBuildingsRepository(buildings);
            _buildingInventoryRepository = new InMemoryInventoryRepository(
                new InventoryState(new ItemCatalog(new[]
                {
                    new ItemDefinition(
                        DemoBuildingBoxItemId,
                        "Workshop BuildingBox",
                        maximumStackSize: 1,
                        isTool: false),
                })));

            BuildingFunctionsPresenter functions = new BuildingFunctionsPresenter();
            _buildingPresenter = new BuildingWorldPresenter(functions);
            _buildingCommands = new BuildingFunctionsCommandAdapter(
                functions,
                new StartBuildingBoxPackingHandler(
                    _buildingsRepository,
                    _buildingInventoryRepository,
                    _jobRepository,
                    journal));
            InitializeBuildingBoxWorldInput(catalog, definition, journal);
            InitializeBuildingPackingExecution(journal);
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

        private CellId FindDemoBuildingOrigin()
        {
            WorldCellViewModel[] openCells = _worldSession.LoadView().Chunks
                .SelectMany(chunk => chunk.Cells)
                .Where(value => !value.IsSolid)
                .ToArray();
            HashSet<CellId> open = new HashSet<CellId>(openCells.Select(
                value => new CellId(value.X, value.Y)));
            WorldCellViewModel? cell = openCells
                .Where(value => value.Y > 0
                    && open.Contains(new CellId(value.X, value.Y - 1)))
                .OrderByDescending(value => value.X)
                .ThenByDescending(value => value.Y)
                .Select(value => (WorldCellViewModel?)value)
                .FirstOrDefault();
            if (!cell.HasValue)
            {
                throw new InvalidOperationException(
                    "The demo world has no open building and work-cell pair.");
            }

            return new CellId(cell.Value.X, cell.Value.Y);
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
