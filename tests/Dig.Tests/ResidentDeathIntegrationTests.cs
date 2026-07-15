using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Messaging;
using Dig.Application.Society;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Society;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Xunit;

namespace Dig.Tests
{

public sealed class ResidentDeathIntegrationTests
{
    private static readonly EntityId ResidentId = Id("81000000000000000000000000000001");
    private static readonly EntityId OtherResidentId = Id("82000000000000000000000000000002");
    private static readonly EntityId ResidentJobId = Id("83000000000000000000000000000003");
    private static readonly EntityId OtherJobId = Id("84000000000000000000000000000004");
    private static readonly EntityId StackId = Id("85000000000000000000000000000005");
    private static readonly ItemId Ore = new ItemId("ore.test");

    [Fact]
    public void Resident_death_cancels_only_assigned_jobs_and_releases_inventory_reservations()
    {
        JobSystem jobs = CreateClaimedJobs();
        InventoryState inventory = CreateReservedInventory();
        RecordingEventSink events = new RecordingEventSink();
        ResidentDeathCleanupHandler handler = new ResidentDeathCleanupHandler(
            new InMemoryJobRepository(jobs),
            new InMemoryInventoryRepository(inventory),
            events);
        ResidentDied died = new ResidentDied(
            tick: 10,
            ResidentId,
            new ResidentDeathCauseId("accident"),
            new CellId(2, 3));

        ResidentDeathCleanupReport report = handler.Handle(died);

        Assert.Equal(new[] { ResidentJobId }, report.CancelledJobIds);
        Assert.Equal(3, report.ReleasedInventoryQuantity);
        Assert.Equal(JobStatus.Cancelled, jobs.Get(ResidentJobId)!.Status);
        Assert.Equal(JobStatus.Claimed, jobs.Get(OtherJobId)!.Status);
        Assert.DoesNotContain(jobs.GetReservations(), item => item.JobId == ResidentJobId);
        Assert.Contains(jobs.GetReservations(), item => item.JobId == OtherJobId);
        ItemStackSnapshot stack = inventory.GetStack(StackId)!;
        Assert.Equal(2, stack.ReservedQuantity);
        Assert.DoesNotContain(stack.Reservations, item => item.JobId == ResidentJobId);
        Assert.Contains(stack.Reservations, item => item.JobId == OtherJobId);
        Assert.Contains(events.Events, item => item is JobReservationsReleased);
        Assert.Contains(events.Events, item => item is ItemQuantityReservationChanged);
    }

    [Fact]
    public void Replaying_resident_death_cleanup_is_idempotent()
    {
        JobSystem jobs = CreateClaimedJobs();
        InventoryState inventory = CreateReservedInventory();
        RecordingEventSink events = new RecordingEventSink();
        ResidentDeathCleanupHandler handler = new ResidentDeathCleanupHandler(
            new InMemoryJobRepository(jobs),
            new InMemoryInventoryRepository(inventory),
            events);
        ResidentDied died = new ResidentDied(
            tick: 10,
            ResidentId,
            new ResidentDeathCauseId("accident"),
            new CellId(2, 3));

        ResidentDeathCleanupReport first = handler.Handle(died);
        int eventCount = events.Events.Count;
        ResidentDeathCleanupReport replay = handler.Handle(died);

        Assert.Single(first.CancelledJobIds);
        Assert.Empty(replay.CancelledJobIds);
        Assert.Equal(0, replay.ReleasedInventoryQuantity);
        Assert.Equal(eventCount, events.Events.Count);
    }

    [Fact]
    public void Agent_death_creates_lifecycle_death_with_position_and_runs_cleanup()
    {
        AgentState agent = AgentTestFactory.CreateAgent(health: 0, id: ResidentId);
        InMemoryAgentRepository agents = new InMemoryAgentRepository();
        Assert.True(agents.Add(agent).IsSuccess);
        SocietyState society = CreateSociety();
        JobSystem jobs = CreateClaimedJobs();
        InventoryState inventory = CreateReservedInventory();
        RecordingEventSink events = new RecordingEventSink();
        ResidentDeathCleanupHandler cleanup = new ResidentDeathCleanupHandler(
            new InMemoryJobRepository(jobs),
            new InMemoryInventoryRepository(inventory),
            events);
        AgentDeathLifecycleHandler handler = new AgentDeathLifecycleHandler(
            agents,
            new InMemorySocietyRepository(society),
            cleanup,
            events);

        Result<AgentDeathLifecycleReport> result = handler.Handle(
            new AgentDied(tick: 10, ResidentId));

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.LifecycleDeathCreated);
        Assert.NotNull(result.Value.Cleanup);
        ResidentSocietySnapshot resident = society.GetResident(ResidentId)!;
        Assert.False(resident.IsAlive);
        Assert.Equal(new ResidentDeathCauseId("health_depleted"), resident.DeathCause);
        Assert.Equal(new CellId(0, 0), resident.LastKnownPosition);
        Assert.Equal(JobStatus.Cancelled, jobs.Get(ResidentJobId)!.Status);
        ResidentDied lifecycleDeath = Assert.Single(events.Events.OfType<ResidentDied>());
        Assert.Equal(ResidentId, lifecycleDeath.ResidentId);
        Assert.Equal(new CellId(0, 0), lifecycleDeath.LastKnownPosition);
    }

    [Fact]
    public void Agent_death_synchronization_rejects_living_agent()
    {
        AgentState agent = AgentTestFactory.CreateAgent(health: 10_000, id: ResidentId);
        InMemoryAgentRepository agents = new InMemoryAgentRepository();
        Assert.True(agents.Add(agent).IsSuccess);
        RecordingEventSink events = new RecordingEventSink();
        AgentDeathLifecycleHandler handler = new AgentDeathLifecycleHandler(
            agents,
            new InMemorySocietyRepository(CreateSociety()),
            new ResidentDeathCleanupHandler(
                new InMemoryJobRepository(new JobSystem()),
                new InMemoryInventoryRepository(CreateEmptyInventory()),
                events),
            events);

        Result<AgentDeathLifecycleReport> result = handler.Handle(
            new AgentDied(tick: 10, ResidentId));

        Assert.True(result.IsFailure);
        Assert.Equal(SocietyApplicationErrors.AgentStillAlive, result.Error);
        Assert.Empty(events.Events);
    }

    private static SocietyState CreateSociety()
    {
        SocietyPolicy policy = new SocietyPolicy(
            adultAgeTicks: 5,
            oldAgeTicks: 100,
            maximumAgeTicks: 200,
            gestationTicks: 10,
            closeKinshipDepth: 3,
            minimumPartnershipSympathy: 6_000,
            minimumPartnershipTrust: 6_000,
            minimumReproductionMood: 7_600,
            minimumReproductionHealth: 5_000);
        SocietyState society = new SocietyState(policy);
        Result registration = society.RegisterFounder(
            new ResidentRegistration(
                ResidentId,
                "Test Dwarf",
                ResidentSex.Female,
                birthTick: 0,
                new CellId(0, 0),
                new ResidentHeritage(8_000)),
            tick: 10);
        Assert.True(registration.IsSuccess);
        return society;
    }

    private static JobSystem CreateClaimedJobs()
    {
        JobSystem jobs = new JobSystem();
        AddClaimedJob(jobs, ResidentJobId, ResidentId, new CellId(1, 1));
        AddClaimedJob(jobs, OtherJobId, OtherResidentId, new CellId(2, 1));
        return jobs;
    }

    private static void AddClaimedJob(
        JobSystem jobs,
        EntityId jobId,
        EntityId residentId,
        CellId target)
    {
        DigJobDefinition definition = new DigJobDefinition(
            jobId,
            new DigJobTarget(target),
            priority: 10,
            createdTick: 0,
            JobRetryPolicy.Default);
        Assert.True(jobs.Add(definition).IsSuccess);
        Assert.True(jobs.MakeAvailable(jobId, tick: 0).IsSuccess);
        Assert.True(jobs.Claim(jobId, residentId, tick: 1).IsSuccess);
    }

    private static InventoryState CreateReservedInventory()
    {
        InventoryState inventory = CreateEmptyInventory();
        Assert.True(inventory.AddStack(
            StackId,
            Ore,
            quantity: 10,
            ItemLocation.InWorld(new CellId(1, 1)),
            tick: 0).IsSuccess);
        Assert.True(inventory.ReserveQuantity(StackId, ResidentJobId, quantity: 3, tick: 1).IsSuccess);
        Assert.True(inventory.ReserveQuantity(StackId, OtherJobId, quantity: 2, tick: 1).IsSuccess);
        return inventory;
    }

    private static InventoryState CreateEmptyInventory()
    {
        return new InventoryState(new ItemCatalog(new[]
        {
            new ItemDefinition(Ore, "Test Ore", maximumStackSize: 100, isTool: false),
        }));
    }

    private static EntityId Id(string value)
    {
        return EntityId.Parse(value);
    }

    private sealed class RecordingEventSink : IEventSink
    {
        private readonly List<IDomainEvent> _events = new List<IDomainEvent>();

        public IReadOnlyList<IDomainEvent> Events => _events;

        public void Append(IReadOnlyCollection<IDomainEvent> events)
        {
            if (events is null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            _events.AddRange(events);
        }
    }
}
}
