using System;
using System.Collections.Generic;
using Dig.Application.Navigation;
using Dig.Domain.Navigation;

namespace Dig.Infrastructure.InMemory
{

public sealed class InMemoryNavigationRepository : INavigationRepository
{
    private readonly object _gate = new object();
    private readonly Dictionary<TraversalProfileId, NavigationMap> _maps =
        new Dictionary<TraversalProfileId, NavigationMap>();

    public NavigationMap? Get(TraversalProfileId profileId)
    {
        lock (_gate)
        {
            return _maps.TryGetValue(profileId, out NavigationMap? map)
                ? map
                : null;
        }
    }

    public void Save(NavigationMap map)
    {
        if (map is null)
        {
            throw new ArgumentNullException(nameof(map));
        }

        lock (_gate)
        {
            _maps[map.Profile.Id] = map;
        }
    }
}
}
