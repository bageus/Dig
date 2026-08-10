using System;
using System.Linq;
using Dig.Domain.World;
using Dig.Presentation.Agents;
using Dig.Presentation.Creatures;
using NUnit.Framework;

namespace Dig.Unity.Tests
{
public sealed class FogOfWarVisibilityPlayModeTests
{
    [Test]
    public void Dynamic_creatures_are_removed_after_they_leave_current_vision()
    {
        DigWorldSession world = DigWorldSession.CreateDemo(24, 16, 5);
        CellId origin = world.LoadSnapshot().Chunks.SelectMany(chunk => chunk.Cells)
            .First(cell => !cell.IsSolid).Id;
        AgentViewModel resident = CreateResident(origin);
        world.UpdateExploration(new[] { resident });

        CreatureVisualSnapshot visible = CreateCreature("visible", origin);
        CellId hiddenCell = world.LoadSnapshot().Chunks.SelectMany(chunk => chunk.Cells)
            .Select(cell => cell.Id)
            .First(cell => Math.Abs(cell.X - origin.X)
                + Math.Abs(cell.Y - origin.Y)
                + Math.Abs(cell.Z - origin.Z) > 8);
        CreatureVisualSnapshot hidden = CreateCreature("hidden", hiddenCell);

        var filtered = world.FilterCurrentlyVisibleCreatures(new[] { visible, hidden });
        Assert.That(filtered, Has.Count.EqualTo(1));
        CreatureVisualSnapshot kept = filtered[0];
        Assert.That(kept.CreatureId, Is.EqualTo("visible"));

        world.UpdateExploration(Array.Empty<AgentViewModel>());
        Assert.That(world.FilterCurrentlyVisibleCreatures(new[] { visible }), Is.Empty);
        Assert.That(world.LoadView().Chunks.SelectMany(chunk => chunk.Cells)
            .Single(cell => cell.X == origin.X && cell.Y == origin.Y && cell.Z == origin.Z)
            .Visibility, Is.EqualTo(Dig.Domain.Exploration.CellVisibility.ExploredNotVisible));
    }

    private static AgentViewModel CreateResident(CellId cell) => new AgentViewModel(
        "resident", "Dwarf", 0, true, cell.X, cell.Y, 100, 100, 100, 100,
        "Work", "Idle", 0, 0, "test", "test",
        Array.Empty<AgentUtilityOptionViewModel>(), cell.Z);

    private static CreatureVisualSnapshot CreateCreature(string id, CellId cell) =>
        new CreatureVisualSnapshot(
            id, "creature.test", CreatureLifecycleVisualStage.Adult,
            CreatureDisposition.Hostile, true, cell.X, cell.Y, cell.Z,
            false, false, false, false, false, 0d, 0);
}
}
