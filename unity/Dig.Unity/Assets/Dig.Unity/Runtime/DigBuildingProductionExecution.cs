using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Agents;
using Dig.Application.Ecology;
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
    private AcquireProductionMaterialHandler? _acquireProductionMaterial;
    private CompleteProductionOrderHandler? _completeProduction;
    private CancelProductionOrderHandler? _cancelProduction;
    private CreateProductionOutputPackageHandler? _createProductionPackage;
    private InterruptProductionOrderHandler? _interruptProduction;
    private StartProductionPackageUseHandler? _startProductionPackageUse;
    private AdvanceProductionPackageUseHandler? _advanceProductionPackageUse;
    private CompleteProductionPackageUseHandler? _completeProductionPackageUse;
    private CancelProductionPackageUseHandler? _cancelProductionPackageUse;
    private CreateBuildingSupplyJobHandler? _createBuildingSupply;
    private CreateDeferredBuildingSupplyJobHandler? _createDeferredBuildingSupply;
    private ResolveDeferredBuildingSupplyJobHandler? _resolveDeferredBuildingSupply;
    private CancelDeferredBuildingSupplyJobHandler? _cancelDeferredBuildingSupply;
    private AcquireBuildingSupplySourceHandler? _acquireBuildingSupplySource;
    private DepositBuildingSupplyHandler? _depositBuildingSupply;
    private CancelBuildingSupplyHandler? _cancelBuildingSupply;
    private SetBuildingStockDeliveryHandler? _setBuildingStockDelivery;
    private EnableProductionInputDeliveryHandler? _enableProductionInputDelivery;
    private InMemoryJobCandidateProvider? _productionCandidates;
    private AssignAvailableJobsHandler? _productionAssignment;
    private NavigationPathfinder? _productionPathfinder;
    private long _nextProductionOrderSequence;
    private long _nextProductionJobSequence;
    private long _nextProductionOutputSequence;
    private long _nextProductionPackageSequence;
    private long _nextProductionPackageUseJobSequence;
    private long _nextProductionPackageUseOutputSequence;
    private long _nextProductionMaterialTransitSequence;
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
        _acquireProductionMaterial = new AcquireProductionMaterialHandler(
            _productionRepository,
            _buildingInventoryRepository,
            _jobRepository,
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
        _createDeferredBuildingSupply = new CreateDeferredBuildingSupplyJobHandler(
            _productionContent,
            _buildingsRepository,
            _jobRepository,
            journal);
        _resolveDeferredBuildingSupply = new ResolveDeferredBuildingSupplyJobHandler(
            _productionContent,
            _buildingSupplyRepository,
            _buildingsRepository,
            _buildingInventoryRepository,
            _jobRepository,
            journal);
        _cancelDeferredBuildingSupply = new CancelDeferredBuildingSupplyJobHandler(
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
        _enableProductionInputDelivery = new EnableProductionInputDeliveryHandler(
            _buildingSupplyRepository);
        _productionCandidates = new InMemoryJobCandidateProvider();
        _productionAssignment = new AssignAvailableJobsHandler(
            _jobRepository,
            _productionCandidates,
            journal);
        _productionPathfinder = new NavigationPathfinder();
        SynchronizeProductionWorkstationRegistrations(tick: 0);
    }

}

}
