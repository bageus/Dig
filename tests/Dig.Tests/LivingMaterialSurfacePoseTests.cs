using Dig.Domain.Core;
using Dig.Domain.Ecology;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Dig.Presentation.Creatures;
using Xunit;

namespace Dig.Tests
{

public sealed class LivingMaterialSurfacePoseTests
{
    [Fact]
    public void Projector_preserves_authoritative_non_centre_floor_position()
    {
        CellId cell = new CellId(4, 2, 1);
        LivingMaterialSnapshot creature = new LivingMaterialSnapshot(
            EntityId.Parse("00000000000000000000000000000001"),
            EntityId.Parse("00000000000000000000000000000002"),
            LivingMaterialSpecies.Hamster, LivingMaterialContainment.Free,
            cell, cell, new LivingMaterialPlaneKey(cell), 1,
            LivingMaterialActivity.Moving, 0, 0, 0, 4, 16, 0, 100, 0,
            null, 1, new SurfacePose(cell, SurfaceFace.Floor, 173, 824));

        CreatureVisualSnapshot projected = Assert.Single(
            new LivingMaterialCreatureVisualProjector().Project(new[] { creature }));

        Assert.Equal(173, projected.SurfaceU);
        Assert.Equal(824, projected.SurfaceV);
    }

    [Fact]
    public void Legacy_snapshot_defaults_to_floor_centre()
    {
        CellId cell = new CellId(1, 1, 0);
        LivingMaterialSnapshot creature = new LivingMaterialSnapshot(
            EntityId.Parse("00000000000000000000000000000003"),
            EntityId.Parse("00000000000000000000000000000004"),
            LivingMaterialSpecies.Grub, LivingMaterialContainment.Free,
            cell, cell, new LivingMaterialPlaneKey(cell), 1,
            LivingMaterialActivity.Moving, 0, 0, 0, int.MaxValue,
            int.MaxValue, 0, 100, 0, null, 0);

        Assert.Equal(SurfacePose.FloorCentre(cell), creature.SurfacePose);
    }
}

}
