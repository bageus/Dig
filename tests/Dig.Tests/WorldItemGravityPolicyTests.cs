using Dig.Application.Inventory;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class WorldItemGravityPolicyTests
{
    [Fact]
    public void Item_falls_through_open_vertical_cells_until_solid_floor()
    {
        CellId source = new CellId(4, 2, 1);

        CellId result = WorldItemGravityPolicy.ResolveLandingCell(
            source,
            worldHeight: 10,
            cell => cell.Y >= 7);

        Assert.Equal(new CellId(4, 6, 1), result);
    }

    [Fact]
    public void Item_stays_on_existing_flat_surface()
    {
        CellId source = new CellId(4, 6, 1);

        CellId result = WorldItemGravityPolicy.ResolveLandingCell(
            source,
            worldHeight: 10,
            cell => cell.Y >= 7);

        Assert.Equal(source, result);
    }
}

}
