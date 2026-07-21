using System;
using Dig.Application.Buildings;
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

internal sealed class BuildingBoxHarness
{
    private static readonly MaterialId Air = new MaterialId("air");
    private readonly InMemoryJobCandidateProvider _candidates =
        new InMemoryJobCandidateProvider();
    private long _tick = 1;

    public BuildingBoxHarness(bool carriedByResident = false)
    {
        BuildingId = Id(10);
        JobId = Id(11);
        SourceStackId = Id(12);
        WorkerId = Id(13);
        SourceCell = new CellId(1, 1);
        BoxItemId = new ItemId("building_box.workshop");
        ItemCatalog items = new ItemCatalog(new[]
        {
            new ItemDefinition(
                BoxItemId,
                "Workshop Box",
                maximumStackSize: 1,
                isTool: false),
        });
        Inventory = new InventoryState(items);
        Inventory.AddStack(
            SourceStackId,
            BoxItemId,
            quantity: 1,
            carriedByResident
                ? ItemLocation.InAgent(WorkerId)
                : ItemLocation.InWorld(SourceCell),
            tick: 0);
        Buildings = new BuildingsState();
        Jobs = new JobSystem();
        InventoryRepository = new InMemoryInventoryRepository(Inventory);
        BuildingsRepository = new InMemoryBuildingsRepository(Buildings);
        JobRepository = new InMemoryJobRepository(Jobs);
        WorldRepository = new InMemoryWorldRepository(CreateWorld());
        Journal = new InMemoryExecutionJournal();
        Catalog = new BuildingCatalog(new[] { CreateDefinition(BoxItemId) });
    }

    public EntityId BuildingId { get; }
    public EntityId JobId { get; }
    public EntityId SourceStackId { get; }
    public EntityId WorkerId { get; }
    public CellId SourceCell { get; }
    public ItemId BoxItemId { get; }
    public InventoryState Inventory { get; }
    public BuildingsState Buildings { get; }
    public JobSystem Jobs { get; }
    public InMemoryInventoryRepository InventoryRepository { get; }
    public InMemoryBuildingsRepository BuildingsRepository { get; }
    public InMemoryJobRepository JobRepository { get; }
    public InMemoryWorldRepository WorldRepository { get; }
    public InMemoryExecutionJournal Journal { get; }
    public BuildingCatalog Catalog { get; }

    public Result Confirm(EntityId buildingId, EntityId jobId, CellId origin)
    {
        return new ConfirmBuildingBoxPlacementHandler(
            Catalog,
            WorldRepository,
            BuildingsRepository,
            InventoryRepository,
            JobRepository,
            new BuildingPlacementValidator(),
            Journal).Handle(new ConfirmBuildingBoxPlacementCommand(
                buildingId,
                jobId,
                SourceStackId,
                new BuildingDefinitionId("workshop.box"),
                origin,
                BuildingOrientation.North,
                reachableCells: new[] { new CellId(origin.X, origin.Y - 1) },
                priority: 600,
                tick: _tick++));
    }

    public void AssignAndAdvanceToDeposit()
    {
        _candidates.SetCandidates(JobId, new[]
        {
            new JobCandidate(WorkerId, 5000, distanceCost: 1, isAvailable: true),
        });
        Assert.Single(new AssignAvailableJobsHandler(
            JobRepository,
            _candidates,
            Journal).Handle(new AssignAvailableJobsCommand(_tick++)).Assignments);
        Advance();
        Assert.Equal(JobStageKind.AcquireItem, Jobs.Get(JobId)!.Stage);
        Result acquired = new AcquireBuildingBoxForAssemblyHandler(
            BuildingsRepository,
            InventoryRepository,
            JobRepository,
            Journal).Handle(new AcquireBuildingBoxForAssemblyCommand(
                BuildingId,
                JobId,
                SourceCell,
                _tick++));
        Assert.True(acquired.IsSuccess, acquired.Error?.ToString());
        ItemStackSnapshot carried = Inventory.GetStack(SourceStackId)!;
        Assert.Equal(ItemLocation.InAgent(WorkerId), carried.Location);
        Assert.Equal(1, carried.ReservedQuantity);
        Advance();
        Advance();
        Assert.Equal(JobStageKind.DepositItem, Jobs.Get(JobId)!.Stage);
    }

    public Result CommitToSite()
    {
        return new CommitBuildingBoxToSiteHandler(
            BuildingsRepository,
            InventoryRepository,
            JobRepository,
            Journal).Handle(new CommitBuildingBoxToSiteCommand(
                BuildingId,
                JobId,
                _tick++));
    }

    public void AdvanceToPerformWork()
    {
        Advance();
        Assert.Equal(JobStageKind.PerformWork, Jobs.Get(JobId)!.Stage);
    }

    public Result AddWork(int amount)
    {
        return new AddBuildingBoxAssemblyWorkHandler(
            BuildingsRepository,
            JobRepository,
            Journal).Handle(new AddBuildingBoxAssemblyWorkCommand(
                BuildingId,
                JobId,
                amount,
                _tick++));
    }

    public void AdvanceToFinalize()
    {
        Advance();
        Assert.Equal(JobStageKind.Finalize, Jobs.Get(JobId)!.Stage);
    }

    public Result Complete()
    {
        return new CompleteBuildingBoxAssemblyHandler(
            BuildingsRepository,
            InventoryRepository,
            JobRepository,
            Journal).Handle(new CompleteBuildingBoxAssemblyCommand(
                BuildingId,
                JobId,
                _tick++));
    }

    public Result Cancel(string reason)
    {
        return new CancelBuildingBoxPlanHandler(
            BuildingsRepository,
            InventoryRepository,
            JobRepository,
            Journal).Handle(new CancelBuildingBoxPlanCommand(
                BuildingId,
                reason,
                _tick++));
    }

    private void Advance()
    {
        Assert.True(new AdvanceJobHandler(
            JobRepository,
            Journal).Handle(new AdvanceJobCommand(JobId, _tick++)).IsSuccess);
    }

    private static BuildingDefinition CreateDefinition(ItemId boxItemId)
    {
        return new BuildingDefinition(
            new BuildingDefinitionId("workshop.box"),
            "Box Workshop",
            new[] { new CellOffset(0, 0) },
            new[] { new CellOffset(0, -1) },
            Array.Empty<BuildingMaterialRequirement>(),
            requiredWork: 3,
            maximumDurability: 100,
            new BuildingBoxPolicy(boxItemId, packingWork: 2));
    }

    private static WorldState CreateWorld()
    {
        MaterialCatalog materials = new MaterialCatalog(new[]
        {
            new MaterialDefinition(Air, isSolid: false, hardness: 0),
        });
        return WorldState.CreateFilled(
            new WorldSize(8, 8),
            chunkSize: 4,
            materials,
            Air,
            explored: true).Value;
    }

    public static EntityId Id(int value)
    {
        return EntityId.Parse(value.ToString("x32"));
    }
}
}
