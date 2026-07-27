using Dig.Application.Inventory;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class BuildingBoxRelocationWorkCellTests
{
    private static readonly CellId Destination = new CellId(5, 4, 0);

    [Fact]
    public void Destination_and_orthogonal_neighbor_are_valid_deposit_positions()
    {
        Assert.True(BuildingBoxRelocationExecutionPolicy.IsDepositPosition(
            Destination,
            Destination));
        Assert.True(BuildingBoxRelocationExecutionPolicy.IsDepositPosition(
            new CellId(4, 4, 0),
            Destination));
    }

    [Fact]
    public void Distant_or_different_depth_cells_are_not_deposit_positions()
    {
        Assert.False(BuildingBoxRelocationExecutionPolicy.IsDepositPosition(
            new CellId(3, 4, 0),
            Destination));
        Assert.False(BuildingBoxRelocationExecutionPolicy.IsDepositPosition(
            new CellId(5, 4, 1),
            Destination));
    }
}

}
