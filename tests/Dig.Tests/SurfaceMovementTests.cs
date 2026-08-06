using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class SurfaceMovementTests
{
    [Fact]
    public void FloorPointConvertsAFreeformOffsetToDeterministicCoordinates()
    {
        SurfacePose pose = SurfacePose.FloorPoint(
            new CellId(4, 3, 1),
            offsetX: 0.31d,
            offsetZ: -0.2d);

        Assert.Equal(SurfaceFace.Floor, pose.Face);
        Assert.Equal(810, pose.U);
        Assert.Equal(300, pose.V);
    }

    [Theory]
    [InlineData(SurfaceMoverKind.Resident, true)]
    [InlineData(SurfaceMoverKind.CaveMonster, true)]
    [InlineData(SurfaceMoverKind.Spider, true)]
    [InlineData(SurfaceMoverKind.GroundEnemy, false)]
    [InlineData(SurfaceMoverKind.Hamster, false)]
    [InlineData(SurfaceMoverKind.Worm, false)]
    public void Only_approved_movers_can_use_vertical_surfaces(
        SurfaceMoverKind mover,
        bool expected)
    {
        SurfacePose wall = new SurfacePose(
            new CellId(2, 3, 1),
            SurfaceFace.PositiveX,
            123,
            789);

        Assert.Equal(expected, SurfaceTraversalPolicy.CanUse(mover, wall));
    }

    [Fact]
    public void Front_z0_face_is_never_climbable()
    {
        SurfacePose front = new SurfacePose(
            new CellId(2, 3, 0),
            SurfaceFace.NegativeZ,
            500,
            500);

        Assert.False(SurfaceTraversalPolicy.CanUse(
            SurfaceMoverKind.Resident,
            front));
        Assert.False(SurfaceTraversalPolicy.CanUse(
            SurfaceMoverKind.Spider,
            front));
    }

    [Fact]
    public void Resident_can_take_an_authoritative_non_cell_centre_floor_position()
    {
        AgentState agent = CreateAgent(new CellId(1, 2, 0));
        SurfacePose target = new SurfacePose(
            new CellId(1, 2, 0),
            SurfaceFace.Floor,
            137,
            864);

        Result result = agent.MoveOnSurface(target, tick: 1);

        Assert.True(result.IsSuccess);
        Assert.Equal(target, agent.SurfacePose);
        Assert.Equal(new CellId(1, 2, 0), agent.Position);
        Assert.Contains(agent.PeekUncommittedEvents(), value => value is AgentSurfaceMoved);
    }

    [Fact]
    public void Legacy_cell_move_resets_surface_pose_to_floor_centre()
    {
        AgentState agent = CreateAgent(new CellId(1, 2, 0));
        agent.MoveOnSurface(new SurfacePose(
            new CellId(1, 2, 0), SurfaceFace.Floor, 12, 34), tick: 1);

        Result result = agent.MoveTo(new CellId(2, 2, 0), tick: 2);

        Assert.True(result.IsSuccess);
        Assert.Equal(
            SurfacePose.FloorCentre(new CellId(2, 2, 0)),
            agent.SurfacePose);
    }

    [Fact]
    public void Explicit_ground_mover_cannot_bypass_vertical_policy()
    {
        AgentState agent = CreateAgent(new CellId(1, 2, 0));
        SurfacePose wall = new SurfacePose(
            agent.Position, SurfaceFace.PositiveX, 500, 500);

        Result result = agent.MoveOnSurface(
            wall,
            SurfaceMoverKind.GroundEnemy,
            tick: 1);

        Assert.True(result.IsFailure);
        Assert.Equal(SurfacePose.FloorCentre(agent.Position), agent.SurfacePose);
    }

    private static AgentState CreateAgent(CellId position)
    {
        return new AgentState(
            EntityId.Parse("aa000000000000000000000000000099"),
            "Surface tester",
            new AgentNeedsSnapshot(
                new NeedValue(8_000),
                new NeedValue(8_000),
                new NeedValue(8_000),
                new NeedValue(10_000)),
            DailySchedule.CreateBalanced(24),
            skills: null,
            traits: null,
            initialPosition: position);
    }
}

}
