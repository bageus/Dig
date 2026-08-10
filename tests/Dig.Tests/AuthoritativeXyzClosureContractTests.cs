using System;
using System.IO;
using Xunit;

namespace Dig.Tests
{
public sealed class AuthoritativeXyzClosureContractTests
{
    [Fact]
    public void Unity_routes_overlays_and_stockpiles_project_authoritative_depth()
    {
        string runtime = Path.Combine(
            FindRepositoryRoot(),
            "Assets",
            "Dig.Unity",
            "Runtime");
        string routeRenderer = File.ReadAllText(Path.Combine(
            runtime,
            "DigNavigationRouteRenderer.cs"));
        string overlay = File.ReadAllText(Path.Combine(
            runtime,
            "DigWorldOverlayRenderer.cs"));
        string overlayRender = File.ReadAllText(Path.Combine(
            runtime,
            "DigWorldOverlayRenderer.Render.cs"));
        string stockpile = File.ReadAllText(Path.Combine(
            runtime,
            "DigStockpileRenderer.cs"));
        string packing = File.ReadAllText(Path.Combine(
            runtime,
            "DigBuildingPackingExecution.cs"));
        string production = File.ReadAllText(Path.Combine(
            runtime,
            "DigBuildingProductionRuntime.cs"));

        Assert.Contains("DigTunnelProjection.RouteWorldPosition", routeRenderer);
        Assert.Contains("new CellId(cell.X, cell.Y, cell.Z)", routeRenderer);
        Assert.Contains("Dictionary<(int X, int Y, int Z), long>", overlay);
        Assert.Contains("new CellId(x, y, z)", overlay);
        Assert.Contains("center.X, center.Y, center.Z", overlayRender);
        Assert.Contains("cell.X, cell.Y, cell.Z", overlayRender);
        Assert.Contains("status.Cell.Z", stockpile);
        Assert.Contains("pair.Value.Target.Z", packing);
        Assert.Contains("pair.Value.Target.Z", production);
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
