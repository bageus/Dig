using Dig.Application.World;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class ExcavationBoundaryPolicyTests
{
    [Fact]
    public void Edges_and_first_rock_row_are_protected()
    {
        ExcavationBoundaryPolicy policy = new ExcavationBoundaryPolicy(
            width: 20,
            height: 14,
            topRockY: 3);

        Assert.True(policy.IsProtected(new CellId(0, 8)));
        Assert.True(policy.IsProtected(new CellId(19, 8)));
        Assert.True(policy.IsProtected(new CellId(8, 13)));
        Assert.True(policy.IsProtected(new CellId(8, 3)));
    }

    [Fact]
    public void Interior_rock_below_protected_row_remains_excavatable()
    {
        ExcavationBoundaryPolicy policy = new ExcavationBoundaryPolicy(
            width: 20,
            height: 14,
            topRockY: 3);

        Assert.False(policy.IsProtected(new CellId(8, 4)));
        Assert.False(policy.IsProtected(new CellId(8, 9)));
    }
}

}
