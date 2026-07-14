using System.Linq;
using Dig.Application.Buildings;
using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Xunit;

namespace Dig.Tests
{

internal sealed class BuildingConstructionHarness
{
    private readonly EntityId _buildingId;
    private readonly EntityId _sourceStackId;
    private readonly EntityId _workerId;

    public BuildingConstructionHarness(
        InventoryState inventory,
        EntityId buildingId,
        EntityId sourceStackId,
        EntityId workerId)
    {
        _buildingId = buildingId;
        _sourceStackId = sourceStackId;
        _workerId = workerId;
        Inventory = inventory;
        Buildings = new BuildingsState();
        Jobs = new JobSystem();
        InventoryRepository = new InMemoryInventoryRepository(Inventory);
        BuildingsRepository = new InMemoryBuildingsRepository(Buildings);
        JobRepository = new InMemoryJobRepository(Jobs);
        WorldRepository = new InMemoryWorldRepository(
            BuildingPlacementTests.CreateEmptyWorld());
        Candidates = new InMemoryJobCandidateProvider();
        Journal = new InMemoryExecutionJournal();
        Catalog = new BuildingCatalog(new[]
        {
            BuildingPlacementTests.CreateDefinition(),
        });
    }

    public InventoryState Inventory { get; }

    public BuildingsState Buildings { get; }

    public JobSystem Jobs { get; }

    public InMemoryInventoryRepository InventoryRepository { get; }

    public InMemoryBuildingsRepository BuildingsRepository { get; }

    public InMemoryJobRepository JobRepository { get; }

    public InMemoryWorldRepository WorldRepository { get; }

    public InMemoryJobCandidateProvider Candidates { get; }

    public InMemoryExecutionJournal Journal { get; }

    public BuildingCatalog Catalog { get; }

    public void Place()
    {
        Result result = new PlaceBuildingHandler(
            Catalog,
            WorldRepository,
            BuildingsRepository,
            new BuildingPlacementValidator(),
            Journal).Handle(new PlaceBuildingCommand(
                _buildingId,
                new BuildingDefinitionId("workshop.basic"),
                new CellId(3, 3),
                BuildingOrientation.North,
                new[] { new CellId(3, 2) },
                tick: 1));
        Assert.True(result.IsSuccess, result.Error?.ToString());
    }

    public void Deliver(EntityId jobId, int quantity, EntityId movedStackId)
    {
        Assert.True(new CreateBuildingDeliveryHandler(
            BuildingsRepository,
            InventoryRepository,
            JobRepository,
            Journal).Handle(new CreateBuildingDeliveryCommand(
                jobId,
                _buildingId,
                _sourceStackId,
                quantity,
                priority: 600,
                tick: 2)).IsSuccess);
        AssignAndAdvanceToDeposit(jobId, tick: 3);
        Assert.True(new CompleteBuildingDeliveryHandler(
            InventoryRepository,
            JobRepository,
            Journal).Handle(new CompleteBuildingDeliveryCommand(
                jobId,
                movedStackId,
                tick: 6)).IsSuccess);
    }

    public void AssignAndAdvanceToDeposit(EntityId jobId, long tick)
    {
        SetCandidate(jobId);
        Assert.Single(new AssignAvailableJobsHandler(
            JobRepository,
            Candidates,
            Journal).Handle(new AssignAvailableJobsCommand(tick)).Assignments);
        AdvanceJobHandler advance = new AdvanceJobHandler(JobRepository, Journal);
        Assert.True(advance.Handle(new AdvanceJobCommand(jobId, tick + 1)).IsSuccess);
        Assert.True(advance.Handle(new AdvanceJobCommand(jobId, tick + 2)).IsSuccess);
        Assert.True(advance.Handle(new AdvanceJobCommand(jobId, tick + 3)).IsSuccess);
    }

    public void AssignAndAdvanceToPerformWork(EntityId jobId, long tick)
    {
        SetCandidate(jobId);
        Assert.Single(new AssignAvailableJobsHandler(
            JobRepository,
            Candidates,
            Journal).Handle(new AssignAvailableJobsCommand(tick)).Assignments);
        AdvanceJobHandler advance = new AdvanceJobHandler(JobRepository, Journal);
        Assert.True(advance.Handle(new AdvanceJobCommand(jobId, tick + 1)).IsSuccess);
        Assert.True(advance.Handle(new AdvanceJobCommand(jobId, tick + 2)).IsSuccess);
    }

    private void SetCandidate(EntityId jobId)
    {
        Candidates.SetCandidates(jobId, new[]
        {
            new JobCandidate(_workerId, 5000, distanceCost: 1, isAvailable: true),
        });
    }
}
}
