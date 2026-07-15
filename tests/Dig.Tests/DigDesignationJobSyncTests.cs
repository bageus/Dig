using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Jobs;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Xunit;

namespace Dig.Tests
{

public sealed class DigDesignationJobSyncTests
{
    private static readonly CellId First = new CellId(2, 1);
    private static readonly CellId Second = new CellId(3, 1);

    [Fact]
    public void Sync_creates_available_jobs_once_in_cell_order()
    {
        WorldState world = CreateWorld();
        Assert.True(world.SetDigDesignation(Second, true, 1).IsSuccess);
        Assert.True(world.SetDigDesignation(First, true, 2).IsSuccess);
        InMemoryJobRepository jobs = new InMemoryJobRepository();
        SyncDigDesignationJobsHandler handler = CreateHandler(
            world,
            jobs,
            Id("40000000000000000000000000000001"),
            Id("40000000000000000000000000000002"));

        DigDesignationJobSyncReport created = handler.Handle(
            new SyncDigDesignationJobsCommand(700, 3));
        DigDesignationJobSyncReport repeated = handler.Handle(
            new SyncDigDesignationJobsCommand(700, 4));

        Assert.Equal(new[] { First, Second }, created.Created.Select(value => value.CellId));
        Assert.Empty(repeated.Created);
        Assert.Equal(2, jobs.Get().GetAll().Count);
        Assert.All(jobs.Get().GetAll(), job => Assert.Equal(JobStatus.Available, job.Status));
    }

    [Fact]
    public void Removing_designation_cancels_job_and_releases_reservations()
    {
        WorldState world = CreateWorld();
        Assert.True(world.SetDigDesignation(First, true, 1).IsSuccess);
        InMemoryJobRepository jobs = new InMemoryJobRepository();
        EntityId jobId = Id("40000000000000000000000000000003");
        SyncDigDesignationJobsHandler handler = CreateHandler(world, jobs, jobId);
        handler.Handle(new SyncDigDesignationJobsCommand(500, 2));
        Assert.True(jobs.Get().Claim(
            jobId,
            Id("10000000000000000000000000000001"),
            3).IsSuccess);
        Assert.True(world.SetDigDesignation(First, false, 4).IsSuccess);

        DigDesignationJobSyncReport report = handler.Handle(
            new SyncDigDesignationJobsCommand(500, 5));

        Assert.Equal(new[] { jobId }, report.Cancelled);
        Assert.Equal(JobStatus.Cancelled, jobs.Get().Get(jobId)!.Status);
        Assert.Empty(jobs.Get().GetReservations());
    }

    private static SyncDigDesignationJobsHandler CreateHandler(
        WorldState world,
        InMemoryJobRepository jobs,
        params EntityId[] ids)
    {
        return new SyncDigDesignationJobsHandler(
            new InMemoryWorldRepository(world),
            jobs,
            new QueueIdSource(ids),
            new InMemoryExecutionJournal());
    }

    private static WorldState CreateWorld()
    {
        MaterialId rock = new MaterialId("test.rock");
        MaterialId air = new MaterialId("test.air");
        MaterialCatalog materials = new MaterialCatalog(new[]
        {
            new MaterialDefinition(rock, true, 100),
            new MaterialDefinition(air, false, 0),
        });
        return WorldState.CreateFilled(
            new WorldSize(6, 4),
            2,
            materials,
            rock,
            true).Value;
    }

    private static EntityId Id(string value)
    {
        return EntityId.Parse(value);
    }

    private sealed class QueueIdSource : IDigJobIdSource
    {
        private readonly Queue<EntityId> _values;

        public QueueIdSource(IEnumerable<EntityId> values)
        {
            _values = new Queue<EntityId>(values);
        }

        public EntityId Next()
        {
            if (_values.Count == 0)
            {
                throw new InvalidOperationException("No job ids remain.");
            }

            return _values.Dequeue();
        }
    }
}
}
