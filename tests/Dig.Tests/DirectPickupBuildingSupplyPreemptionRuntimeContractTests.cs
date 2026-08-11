using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{

public sealed class DirectPickupBuildingSupplyPreemptionRuntimeContractTests
{
    [Fact]
    public void Direct_world_pickup_preempts_building_supply_reservation_first()
    {
        string source = File.ReadAllText(Path.Combine(
            ResolveRepositoryRoot(),
            "Assets",
            "Dig.Unity",
            "Runtime",
            "DigWorldItemPickupSession.cs"));

        Assert.Contains("PreemptBuildingSupplyReservations(", source,
            StringComparison.Ordinal);
        Assert.Contains("stack.Reservations", source, StringComparison.Ordinal);
        Assert.Contains("job.Definition is BuildingSupplyJobDefinition", source,
            StringComparison.Ordinal);
        Assert.Contains("new CancelBuildingSupplyCommand(", source,
            StringComparison.Ordinal);
        Assert.Contains("building_supply_preempted_by_direct_pickup", source,
            StringComparison.Ordinal);
        Assert.True(
            source.IndexOf("Result preempted = PreemptBuildingSupplyReservations(",
                StringComparison.Ordinal)
            < source.IndexOf("Result prepared = PrepareResidentsForDirectCommand(",
                StringComparison.Ordinal));
    }

    private static string ResolveRepositoryRoot()
    {
        DirectoryInfo? current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, "Assets", "Dig.Unity")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Repository root was not found.");
    }
}

}
