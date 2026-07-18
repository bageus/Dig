using Dig.Application.Jobs;
using Dig.Domain.World;
using Xunit;

namespace Dig.Tests
{

public sealed class ExcavationStrokePlannerTests
{
    [Fact]
    public void Dominant_horizontal_motion_locks_horizontal_axis()
    {
        ExcavationStrokeDecision decision = new ExcavationStrokePlanner().Resolve(
            new CellId(4, 5),
            new CellId(8, 6));

        Assert.Equal(ExcavationStrokeAxis.Horizontal, decision.Axis);
        Assert.Equal(new CellId(8, 5), decision.Cell);
    }

    [Fact]
    public void Dominant_vertical_motion_locks_vertical_axis()
    {
        ExcavationStrokeDecision decision = new ExcavationStrokePlanner().Resolve(
            new CellId(4, 5),
            new CellId(5, 9));

        Assert.Equal(ExcavationStrokeAxis.Vertical, decision.Axis);
        Assert.Equal(new CellId(4, 9), decision.Cell);
    }

    [Fact]
    public void Locked_axis_is_preserved_when_pointer_crosses_the_other_axis()
    {
        ExcavationStrokeDecision decision = new ExcavationStrokePlanner().Resolve(
            new CellId(4, 5),
            new CellId(5, 10),
            ExcavationStrokeAxis.Horizontal);

        Assert.Equal(ExcavationStrokeAxis.Horizontal, decision.Axis);
        Assert.Equal(new CellId(5, 5), decision.Cell);
    }

    [Fact]
    public void Initial_pointer_keeps_anchor_without_selecting_an_axis()
    {
        CellId anchor = new CellId(4, 5);

        ExcavationStrokeDecision decision = new ExcavationStrokePlanner().Resolve(
            anchor,
            anchor);

        Assert.Equal(ExcavationStrokeAxis.None, decision.Axis);
        Assert.Equal(anchor, decision.Cell);
    }
}

}
