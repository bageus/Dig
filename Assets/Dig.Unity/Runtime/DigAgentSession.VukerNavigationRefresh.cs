using System;
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

        _vukerRegions = new VukerCaveRegionResolver(tunnelVolume);
        _vukerBirthPlanner = new VukerBirthPlanner(_vukerRegions);
    }
}

}
