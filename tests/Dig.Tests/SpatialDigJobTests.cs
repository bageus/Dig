using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class SpatialDigJobTests
{
    [Fact]
    public void Spatial_reservations_distinguish_equal_projection_at_different_depths()
    {
        ReservationKey front = ReservationKey.ForDesignation(
            new SpatialCellId(4, 5, 0));
        ReservationKey deep = ReservationKey.ForDesignation(
            new SpatialCellId(4, 5, 1));

        Assert.NotEqual(front, deep);
        Assert.Equal("4,5,0", front.Value);
        Assert.Equal("4,5,1", deep.Value);
    }

    [Fact]
    public void Claim_reserves_exact_spatial_target_and_adjacent_work_cell()
    {
        JobSystem jobs = new JobSystem();
        EntityId jobId = Id("40000000000000000000000000000031");
        EntityId agentId = Id("10000000000000000000000000000031");
        SpatialCellId work = new SpatialCellId(4, 5, 1);
        SpatialCellId target = new SpatialCellId(4, 5, 2);
        SpatialDigJobDefinition definition = new SpatialDigJobDefinition(
            jobId,
            new SpatialDigJobTarget(target, work),
            priority: 700,
            createdTick: 1,
            JobRetryPolicy.Default);

        Assert.True(jobs.Add(definition).IsSuccess);
        Assert.True(jobs.MakeAvailable(jobId, tick: 1).IsSuccess);
        Assert.True(jobs.Claim(jobId, agentId, tick: 2).IsSuccess);

        Assert.Contains(jobs.GetReservations(), value =>
            value.JobId == jobId
            && value.Key == ReservationKey.ForPosition(work));
        Assert.Contains(jobs.GetReservations(), value =>
            value.JobId == jobId
            && value.Key == ReservationKey.ForDesignation(target));
    }

    [Fact]
    public void Adjacent_face_is_required_for_spatial_work_position()
    {
        Assert.Throws<System.ArgumentException>(() => new SpatialDigJobTarget(
            new SpatialCellId(4, 5, 2),
            new SpatialCellId(5, 5, 1)));
    }

    private static EntityId Id(string value)
    {
        return EntityId.Parse(value);
    }
}

}