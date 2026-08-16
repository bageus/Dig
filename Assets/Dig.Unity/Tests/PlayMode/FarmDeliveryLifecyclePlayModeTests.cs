using System.Linq;
using Dig.Application.Farming;
using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Farming;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using NUnit.Framework;

namespace Dig.Unity.Tests
{

public sealed class FarmDeliveryLifecyclePlayModeTests
{
    [Test]
    public void Seed_delivery_acquires_carries_deposits_and_commits_farm_state()
    {
        Harness harness = new Harness();
        FarmLogisticsJobPlan plan = harness.Plan(tick: 1);
        harness.Assign(plan.JobId, tick: 2);
        AssertSuccess(harness.Jobs.Start(plan.JobId, tick: 3));
        AssertSuccess(new AcquireHaulingItemHandler(
            harness.InventoryRepository,
            harness.JobRepository,
            harness.Journal).Handle(new AcquireHaulingItemCommand(
                plan.JobId,
                Harness.CarriedStackId,
                tick: 4)));
        Assert.That(harness.Jobs.Get(plan.JobId)!.Stage,
            Is.EqualTo(JobStageKind.TravelToDestination));
        AssertSuccess(harness.Jobs.AdvanceStage(plan.JobId, tick: 5));

        AssertSuccess(new CompleteFarmDeliveryHandler(
            harness.Farms,
            harness.InventoryRepository,
            harness.JobRepository,
            FarmItemCatalog.Default,
            harness.Reservations,
            harness.Journal).Handle(new CompleteFarmDeliveryCommand(
                plan.JobId,
                Id(7),
                tick: 6)));

        Assert.That(harness.Farms.Get(Harness.FarmId)!.MushroomSeedEstablished, Is.True);
        Assert.That(harness.Farms.Get(Harness.FarmId)!.MushroomSlotsOccupied, Is.EqualTo(3));
        Assert.That(harness.Jobs.Get(plan.JobId)!.Status, Is.EqualTo(JobStatus.Completed));
        Assert.That(harness.Reservations.GetAll(), Is.Empty);
        Assert.That(harness.Inventory.GetResidentSlotClaims(plan.JobId), Is.Empty);
    }

    [Test]
    public void Mode_change_cancels_carried_seed_and_releases_transport_claims()
    {
        Harness harness = new Harness();
        FarmLogisticsJobPlan plan = harness.Plan(tick: 1);
        harness.Assign(plan.JobId, tick: 2);
        AssertSuccess(harness.Jobs.Start(plan.JobId, tick: 3));
        AssertSuccess(new AcquireHaulingItemHandler(
            harness.InventoryRepository,
            harness.JobRepository,
            harness.Journal).Handle(new AcquireHaulingItemCommand(
                plan.JobId,
                Harness.CarriedStackId,
                tick: 4)));
        harness.Farms.Get(Harness.FarmId)!.SwitchMode(FarmMode.Hamsters, tick: 5);

        Result<FarmLogisticsSynchronizationReport> reconciled =
            harness.Synchronize(tick: 6);

        Assert.That(reconciled.IsSuccess, Is.True, reconciled.Error?.ToString());
        Assert.That(reconciled.Value.ReleasedReservations, Is.EqualTo(1));
        Assert.That(harness.Jobs.Get(plan.JobId)!.Status, Is.EqualTo(JobStatus.Cancelled));
        ItemStackSnapshot carried = harness.Inventory.GetStack(Harness.CarriedStackId)!;
        Assert.That(carried.Location.Kind, Is.EqualTo(ItemLocationKind.AgentInventory));
        Assert.That(carried.Location.OwnerId, Is.EqualTo(Harness.WorkerId));
        Assert.That(carried.ReservedQuantity, Is.Zero);
        Assert.That(harness.Inventory.GetResidentSlotClaims(plan.JobId), Is.Empty);
        Assert.That(harness.Reservations.GetAll(), Is.Empty);
        FarmDeliveryDemand demand = AssertOne(
            harness.Farms.Get(Harness.FarmId)!.GetDeliveryDemands());
        Assert.That(demand.Kind, Is.EqualTo(FarmDeliveryKind.Hamster));
    }

    private sealed class Harness
    {
        internal static readonly EntityId FarmId = Id(1);
        internal static readonly EntityId SourceStackId = Id(2);
        internal static readonly EntityId JobId = Id(3);
        internal static readonly EntityId WorkerId = Id(4);
        internal static readonly EntityId CarriedStackId = Id(5);
        private readonly InMemoryJobCandidateProvider _candidates =
            new InMemoryJobCandidateProvider();
        private readonly HaulingResidentSlotClaimService _slotClaims;

        internal Harness()
        {
            Farms = new InMemoryFarmRepository();
            Farms.Save(FarmId, new FarmState());
            Inventory = new InventoryState(new ItemCatalog(new[]
            {
                new ItemDefinition(
                    CampfireProductionContent.MushroomCapItemId,
                    "Mushroom cap",
                    maximumStackSize: 100,
                    isTool: false),
            }));
            AssertSuccess(Inventory.AddUnit(
                SourceStackId,
                CampfireProductionContent.MushroomCapItemId,
                ItemLocation.InWorld(new CellId(2, 2)),
                tick: 0));
            Jobs = new JobSystem();
            InventoryRepository = new InMemoryInventoryRepository(Inventory);
            JobRepository = new InMemoryJobRepository(Jobs);
            Reservations = new FarmLogisticsReservations();
            Journal = new InMemoryExecutionJournal();
            _slotClaims = new HaulingResidentSlotClaimService(
                InventoryRepository,
                Journal);
        }

        internal InMemoryFarmRepository Farms { get; }
        internal InventoryState Inventory { get; }
        internal JobSystem Jobs { get; }
        internal InMemoryInventoryRepository InventoryRepository { get; }
        internal InMemoryJobRepository JobRepository { get; }
        internal FarmLogisticsReservations Reservations { get; }
        internal InMemoryExecutionJournal Journal { get; }

        internal FarmLogisticsJobPlan Plan(long tick)
        {
            Result<FarmLogisticsSynchronizationReport> result = Synchronize(tick);
            Assert.That(result.IsSuccess, Is.True, result.Error?.ToString());
            return AssertOne(result.Value.Created);
        }

        internal Result<FarmLogisticsSynchronizationReport> Synchronize(long tick)
        {
            return new SynchronizeFarmLogisticsHandler(
                Farms,
                InventoryRepository,
                JobRepository,
                FarmItemCatalog.Default,
                Reservations,
                new FixedIds(JobId),
                Journal).Handle(new SynchronizeFarmLogisticsCommand(
                    new[] { new CellId(2, 2) },
                    priority: 650,
                    maximumJobs: 8,
                    tick));
        }

        internal void Assign(EntityId jobId, long tick)
        {
            _candidates.SetCandidates(jobId, new[]
            {
                new JobCandidate(WorkerId, 5_000, 1, isAvailable: true),
            });
            JobAssignmentReport report = new AssignAvailableJobsHandler(
                JobRepository,
                _candidates,
                Journal,
                haulingResidentSlotClaims: _slotClaims).Handle(
                    new AssignAvailableJobsCommand(tick));
            Assert.That(report.Assignments, Has.Count.EqualTo(1));
        }
    }

    private sealed class FixedIds : IFarmLogisticsJobIdSource
    {
        private readonly EntityId _jobId;

        internal FixedIds(EntityId jobId) => _jobId = jobId;

        public EntityId NextJobId() => _jobId;

        public EntityId NextStackId() => Id(6);
    }

    private static T AssertOne<T>(System.Collections.Generic.IEnumerable<T> values) =>
        values.Single();

    private static void AssertSuccess(Result result) =>
        Assert.That(result.IsSuccess, Is.True, result.Error?.ToString());

    private static EntityId Id(int value) => EntityId.Parse(value.ToString("x32"));
}

}
