using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{
public sealed class ResidentInventoryGridSourceContractTests
{
    [Fact]
    public void Inventory_hides_weapon_title_and_fills_two_rows_by_column()
    {
        string runtime = Path.Combine(
            FindRepositoryRoot(),
            "unity",
            "Dig.Unity",
            "Assets",
            "Dig.Unity",
            "Runtime");
        string inventory = File.ReadAllText(
            Path.Combine(runtime, "DigGameHudCanvas.Inventory.cs"));
        string layout = File.ReadAllText(
            Path.Combine(runtime, "DigGameHudCanvas.InventoryLayout.cs"));

        Assert.DoesNotContain("\"WEAPON\"", inventory);
        Assert.Contains(
            "ResidentInventoryCompartment.Weapon,\n            string.Empty,",
            inventory);
        Assert.Contains("ConfigureInventoryGrid(grid, columns, cellWidth);", inventory);
        Assert.Contains("grid.startAxis = GridLayoutGroup.Axis.Vertical;", layout);
        Assert.Contains(
            "grid.constraint = GridLayoutGroup.Constraint.FixedRowCount;",
            layout);
        Assert.Contains("grid.constraintCount = InventoryRows;", layout);
        Assert.DoesNotContain("GridLayoutGroup.Axis.Horizontal", inventory + layout);
        Assert.DoesNotContain("GridLayoutGroup.Constraint.FixedColumnCount", inventory + layout);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Dig.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
}
