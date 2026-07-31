using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Agents;
using Dig.Application.Jobs;
using Dig.Application.Production;
using Dig.Domain.Agents;
using Dig.Domain.Buildings;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Navigation;
using Dig.Domain.Production;
using Dig.Domain.Technology;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Dig.Presentation.Agents;
using Dig.Presentation.Production;
using Dig.Presentation.Navigation;

namespace Dig.Unity
{

internal sealed partial class DigTerrainWorkSession
{
    private readonly Dictionary<EntityId, BuildingProductionRoutePlan>
        _buildingProductionRoutes = new Dictionary<EntityId, BuildingProductionRoutePlan>();
    private readonly Dictionary<EntityId, BuildingProductionRoutePlan>
        _buildingSupplyRoutes = new Dictionary<EntityId, BuildingProductionRoutePlan>();

    private ProductionContentCatalog? _productionContent;
    private InMemoryProductionRepository? _productionRepository;
    private InMemoryTechnologyRepository? _productionTechnologyRepository;
    private InMemoryBuildingSupplyRepository? _buildingSupplyRepository;
    private InMemoryAgentRepository? _productionAgents;
    private BuildingProductionPresenter? _buildingProductionPresenter;
    private EnqueueProductionOrderHandler? _enqueueProduction;
    private PrepareProductionOrderHandler? _prepareProduction;
    private BeginProductionWorkHandler? _beginProduction;
    private ApplyProductionWorkHandler? _applyProductionWork;
    private CompleteProductionOrderHandler? _completeProduction;
    private CancelProductionOrderHandler? _cancelProduction;
    private CreateProductionOutputPackageHandler? _createProductionPackage;
    private InterruptProductionOrderHandler? _interruptProduction;
    private StartProductionPackageUseHandler? _startProductionPackageUse;
    private AdvanceProductionPackageUseHandler? _advanceProductionPackageUse;
    private CompleteProductionPackageUseHandler? _completeProductionPackageUse;
    private CancelProductionPackageUseHandler? _cancelProductionPackageUse;
    private CreateBuildingSupplyJobHandler? _createBuildingSupply;
    private AcquireBuildingSupplySourceHandler? _acquireBuildingSupplySource;
    private DepositBuildingSupplyHandler? _depositBuildingSupply;
    private CancelBuildingSupplyHandler? _cancelBuildingSupply;
    private SetBuildingStockDeliveryHandler? _setBuildingStockDelivery;
    private InMemoryJobCandidateProvider? _productionCandidates;
    private AssignAvailableJobsHandler? _productionAssignment;
    private NavigationPathfinder? _productionPathfinder;
    private long _nextProductionOrderSequence;
    private long _nextProductionJobSequence;
    private long _nextProductionOutputSequence;
    private long _nextProductionPackageSequence;
    private long _nextProductionPackageUseJobSequence;
    private long _nextProductionPackageUseOutputSequence;
    private long _nextSupplyJobSequence;
    private long _nextSupplyTransitSequence;
    private long _nextSupplyDepositSequence;

    internal void InitializeBuildingProductionDemo(
        InMemoryAgentRepository agents,
        InMemoryExecutionJournal journal)
    {
        if (agents == null || journal == null)
        {
            throw new ArgumentNullException(nameof(agents));
        }

        if (_buildingsRepository == null
            || _buildingInventoryRepository == null
            || _buildingBoxCatalog == null)
        {
            throw new InvalidOperationException(
                "Building production requires initialized buildings and inventory.");
        }

        ContentValidationResult validated = ProductionContentCatalog.ValidateAndCreate(
            _buildingInventoryRepository.Get().Catalog,
            _buildingBoxCatalog,
            CampfireProductionContent.CreateRecipes(
                CampfireProductionContent.ProductionMaterialTicks),
            Array.Empty<TechnologyDefinition>(),
            new[] { CampfireProductionContent.CreateWorkstation() });
        if (!validated.Succeeded || validated.Catalog == null)
        {
            throw new InvalidOperationException(
                "Campfire production content is invalid: "
                + string.Join(" | ", validated.Issues.Select(value => value.ToString())));
        }

        _productionContent = validated.Catalog;
        _productionRepository = new InMemoryProductionRepository(new ProductionState());
        _productionTechnologyRepository = new InMemoryTechnologyRepository(
            new TechnologyState());
        _buildingSupplyRepository = new InMemoryBuildingSupplyRepository(
            new BuildingSupplyState());
        _productionAgents = agents;
        _buildingProductionPresenter = new BuildingProductionPresenter(
            _productionContent,
            _buildingInventoryRepository.Get().Catalog);
        _enqueueProduction = new EnqueueProductionOrderHandler(
            _productionContent,
            _productionRepository);
        _prepareProduction = new PrepareProductionOrderHandler(
            _productionRepository,
            _productionTechnologyRepository,
            _buildingsRepository,
            _buildingInventoryRepository,
            _jobRepository,
            new FixedEnergyAvailability(true),
            journal);
        _beginProduction = new BeginProductionWorkHandler(
            _productionRepository,
            _jobRepository,
            agents,
            journal);
        _applyProductionWork = new ApplyProductionWorkHandler(
            _productionRepository,
            _buildingInventoryRepository,
            _jobRepository,
            agents,
            journal);
        _completeProduction = new CompleteProductionOrderHandler(
            _productionRepository,
            _buildingInventoryRepository,
            _jobRepository,
            journal,
            _skillGrants);
        _cancelProduction = new CancelProductionOrderHandler(
            _productionRepository,
            _buildingInventoryRepository,
            _jobRepository,
            journal);
        _createProductionPackage = new CreateProductionOutputPackageHandler(
            _productionRepository,
            _buildingInventoryRepository,
            _jobRepository,
            journal);
        _interruptProduction = new InterruptProductionOrderHandler(
            _productionRepository,
            _buildingInventoryRepository,
            _jobRepository,
            journal);
        _startProductionPackageUse = new StartProductionPackageUseHandler(
            _productionRepository,
            _buildingInventoryRepository,
            _jobRepository,
            journal);
        _advanceProductionPackageUse = new AdvanceProductionPackageUseHandler(
            _jobRepository,
            journal);
        _completeProductionPackageUse = new CompleteProductionPackageUseHandler(
            _productionRepository,
            _buildingInventoryRepository,
            _jobRepository,
            journal);
        _cancelProductionPackageUse = new CancelProductionPackageUseHandler(
            _jobRepository,
            journal);
        _createBuildingSupply = new CreateBuildingSupplyJobHandler(
            _productionContent,
            _buildingSupplyRepository,
            _buildingsRepository,
            _buildingInventoryRepository,
            _jobRepository,
            journal);
        _acquireBuildingSupplySource = new AcquireBuildingSupplySourceHandler(
            _buildingInventoryRepository,
            _jobRepository,
            journal);
        _depositBuildingSupply = new DepositBuildingSupplyHandler(
            _buildingSupplyRepository,
            _buildingInventoryRepository,
            _jobRepository,
            journal);
        _cancelBuildingSupply = new CancelBuildingSupplyHandler(
            _buildingSupplyRepository,
            _buildingInventoryRepository,
            _jobRepository,
            journal);
        _setBuildingStockDelivery = new SetBuildingStockDeliveryHandler(
            _buildingSupplyRepository);
        _productionCandidates = new InMemoryJobCandidateProvider();
        _productionAssignment = new AssignAvailableJobsHandler(
            _jobRepository,
            _productionCandidates,
            journal);
        _productionPathfinder = new NavigationPathfinder();
        SynchronizeProductionWorkstationRegistrations(tick: 0);
    }

    internal BuildingProductionViewModel? LoadBuildingProduction(string buildingId)
    {
        EnsureBuildingProductionInitialized();
        if (string.IsNullOrWhiteSpace(buildingId))
        {
            throw new ArgumentException("Building id is required.", nameof(buildingId));
        }

        EntityId id = EntityId.Parse(buildingId);
        BuildingSnapshot? building = _buildingsRepository!.Get().Get(id);
        if (building == null
            || building.Status != BuildingStatus.Completed
            || !_productionContent!.ContainsWorkstation(building.Definition.Id))
        {
            return null;
        }

        BuildingSupplySnapshot? supply = _buildingSupplyRepository!.Get().Get(
            id,
            _buildingInventoryRepository!.Get().CreateSnapshot());
        return supply == null
            ? null
            : _buildingProductionPresenter!.Present(
                id,
                _productionRepository!.Get(),
                supply);
    }

    internal IReadOnlyList<BuildingProductionViewModel> LoadAllBuildingProduction()
    {
        EnsureBuildingProductionInitialized();
        InventorySnapshot inventory = _buildingInventoryRepository!.Get().CreateSnapshot();
        return _buildingSupplyRepository!.Get().GetAll(inventory)
            .Where(value => _buildingsRepository!.Get().Get(value.BuildingId)?.Status
                == BuildingStatus.Completed)
            .Select(value => _buildingProductionPresenter!.Present(
                value.BuildingId,
                _productionRepository!.Get(),
                value))
            .ToArray();
    }

    internal IReadOnlyList<BuildingInternalStockUnitViewModel>
        LoadAllBuildingInternalStockUnits()
    {
        EnsureBuildingProductionInitialized();
        return _buildingInventoryRepository!.Get().CreateSnapshot().Stacks
            .Where(stack => stack.Location.Kind == ItemLocationKind.BuildingInventory
                && stack.Location.HasOwner)
            .OrderBy(stack => stack.Location.OwnerId.ToString(), StringComparer.Ordinal)
            .ThenBy(stack => stack.ItemId)
            .ThenBy(stack => stack.StackId.ToString(), StringComparer.Ordinal)
            .SelectMany(stack => Enumerable.Range(0, stack.Quantity)
                .Select(unitIndex => new BuildingInternalStockUnitViewModel(
                    stack.StackId.ToString(),
                    stack.Location.OwnerId,
                    stack.ItemId,
                    unitIndex,
                    isAvailable: unitIndex < stack.AvailableQuantity)))
            .ToArray();
    }

    internal Result EnqueueBuildingProduction(
        string buildingId,
        string recipeId,
        long tick)
    {
        EnsureBuildingProductionInitialized();
        EntityId id = EntityId.Parse(buildingId);
        RecipeId recipe = new RecipeId(recipeId);
        BuildingSnapshot? building = _buildingsRepository!.Get().Get(id);
        if (building == null
            || building.Status != BuildingStatus.Completed
            || !_productionContent!.ContainsWorkstation(building.Definition.Id)
            || !_productionContent.GetWorkstation(building.Definition.Id)
                .RecipeIds.Contains(recipe))
        {
            return Result.Failure(ProductionErrors.WorkstationMismatch);
        }

        EntityId orderId = NextProductionEntityId(
            'e',
            ref _nextProductionOrderSequence);
        return _enqueueProduction!.Handle(new EnqueueProductionOrderCommand(
            orderId,
            recipe,
            id,
            tick));
    }

    internal Result CancelOneBuildingProduction(
        string buildingId,
        string recipeId,
        long tick)
    {
        EnsureBuildingProductionInitialized();
        EntityId building = EntityId.Parse(buildingId);
        RecipeId recipe = new RecipeId(recipeId);
        ProductionOrderSnapshot? order = _productionRepository!.Get().GetAll()
            .Where(value => value.BuildingId == building
                && value.Recipe.Id == recipe
                && !value.IsTerminal)
            .OrderBy(value => value.Status == ProductionOrderStatus.Queued ? 0 : 1)
            .ThenByDescending(value => value.Sequence)
            .FirstOrDefault();
        if (order == null)
        {
            return Result.Failure(ProductionErrors.OrderNotFound);
        }

        EntityId jobId = _jobRepository.Get().GetAll()
            .Where(value => value.Definition is ProductionWorkJobDefinition work
                && work.OrderId == order.Id
                && !value.IsTerminal)
            .Select(value => value.Id)
            .FirstOrDefault();
        Result result = _cancelProduction!.Handle(new CancelProductionOrderCommand(
            order.Id,
            jobId,
            "player_cancelled",
            tick));
        if (result.IsSuccess
            && !jobId.IsEmpty
            && (_jobRepository.Get().Get(jobId)?.IsTerminal ?? true))
        {
            _buildingProductionRoutes.Remove(jobId);
        }

        return result;
    }

    internal Result SetBuildingStockDelivery(
        string buildingId,
        string itemId,
        bool enabled,
        long tick)
    {
        EnsureBuildingProductionInitialized();
        return _setBuildingStockDelivery!.Handle(
            new SetBuildingStockDeliveryCommand(
                EntityId.Parse(buildingId),
                new ItemId(itemId),
                enabled,
                tick));
    }

}

}
