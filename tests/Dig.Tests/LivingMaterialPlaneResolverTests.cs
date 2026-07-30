using Dig.Application.Ecology;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class LivingMaterialPlaneResolverTests
{
    [Fact]
    public void WallSplitsSameLayerIntoDifferentPlaneComponents()
    {
        WorldState world = NavigationTestFactory.CreateGroundedCorridor(
            width: 12,
            height: 6,
            chunkSize: 4,
            corridorY: 2,
            blockedX: 6);
        NavigationMap map = NavigationTestFactory.BuildMap(
            world,
            TraversalProfile.CreateFreeMover());
        LivingMaterialPlaneResolver resolver = new LivingMaterialPlaneResolver(
            NavigationTestFactory.GetSnapshot(map));

        Assert.True(resolver.TryResolve(new CellId(2, 2, 0), out LivingMaterialPlane left));
        Assert.True(resolver.TryResolve(new CellId(9, 2, 0), out LivingMaterialPlane right));
        Assert.NotEqual(left.Key, right.Key);
    }

    [Fact]
    public void MovementCandidatesContainOnlySameYAndDepthSupportedWalk()
    {
        WorldState world = NavigationTestFactory.CreateGroundedCorridor(
            width: 12,
            height: 6,
            chunkSize: 4,
            corridorY: 2);
        NavigationMap map = NavigationTestFactory.BuildMap(
            world,
            TraversalProfile.CreateFreeMover());
        LivingMaterialPlaneResolver resolver = new LivingMaterialPlaneResolver(
            NavigationTestFactory.GetSnapshot(map));
        Assert.True(resolver.TryResolve(new CellId(5, 2, 0), out LivingMaterialPlane plane));
        Dig.Domain.Core.EntityId id = Dig.Domain.Core.EntityId.Parse(
            "30000000000000000000000000000001");
        Dig.Domain.Ecology.LivingMaterialEcologyState state =
            new Dig.Domain.Ecology.LivingMaterialEcologyState(1);
        Assert.True(state.Register(
            id,
            id,
            Dig.Domain.Ecology.LivingMaterialSpecies.Grub,
            new CellId(5, 2, 0),
            plane.Key,
            0).IsSuccess);

        foreach (CellId candidate in resolver.GetMovementCandidates(state.Get(id)!))
        {
            Assert.Equal(2, candidate.Y);
            Assert.Equal(0, candidate.Z);
            Assert.Equal(1, System.Math.Abs(candidate.X - 5));
        }
    }
}

}
