using System;
using System.Linq;
using Dig.Application.Ecology;
using Dig.Domain.Navigation;

namespace Dig.Unity
{

internal sealed partial class DigAgentSession
{
    private void RefreshVukerEcologyNavigation(
        TunnelNavigationVolume tunnelVolume)
    {
        if (tunnelVolume == null)
        {
            throw new ArgumentNullException(nameof(tunnelVolume));
        }

        TunnelNavigationVolume ecologyNavigation =
            ResolveVukerEcologyNavigation(tunnelVolume);
        _vukerRegions = new VukerCaveRegionResolver(ecologyNavigation);
        _vukerBirthPlanner = new VukerBirthPlanner(_vukerRegions);
    }

    private static TunnelNavigationVolume ResolveVukerEcologyNavigation(
        TunnelNavigationVolume navigation)
    {
        TunnelDemoLayout? layout = navigation.DemoLayout;
        if (layout == null)
        {
            return navigation;
        }

        TunnelNavigationVolume demo = TunnelNavigationVolume.CreateDemo(
            navigation.Width,
            navigation.Height,
            navigation.Depth,
            layout.CaveHasBackWall);
        return new TunnelNavigationVolume(
            navigation.Width,
            navigation.Height,
            navigation.Depth,
            demo.Cells.Concat(navigation.Cells).Distinct().ToArray(),
            demo.VerticalCells.Concat(navigation.VerticalCells).Distinct().ToArray(),
            demo.SupportedCells.Concat(navigation.SupportedCells).Distinct().ToArray(),
            layout);
    }
}

}
