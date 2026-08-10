using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Dig.Presentation.Agents;

namespace Dig.Unity
{
    internal sealed partial class DigTerrainWorkSession
    {
        private IReadOnlyCollection<CellId> GetBuildingPlacementReachableCells()
        {
            if (!TryLoadBuildingPlacementNavigation(out NavigationSnapshot navigation))
            {
                return Array.Empty<CellId>();
            }

            return navigation.Chunks
                .SelectMany(chunk => chunk.WalkableCells)
                .Where(HasFullStandingSupport)
                .Distinct()
                .OrderBy(cell => cell)
                .ToArray();
        }

        private IReadOnlyCollection<CellId> GetBuildingPlacementReachableCells(
            EntityId sourceStackId,
            IReadOnlyList<AgentViewModel> agents)
        {
            IReadOnlyCollection<CellId> supported =
                GetBuildingPlacementReachableCells();
            if (agents.Count == 0
                || !TryLoadBuildingPlacementNavigation(out NavigationSnapshot navigation))
            {
                return supported;
            }

            ItemStackSnapshot? source = _buildingInventoryRepository!.Get().GetStack(
                sourceStackId);
            if (source == null)
            {
                return Array.Empty<CellId>();
            }

            HashSet<int> reachableRegions = new HashSet<int>();
            if (source.Location.Kind == ItemLocationKind.AgentInventory
                && source.Location.HasOwner)
            {
                AgentViewModel? holder = agents.FirstOrDefault(agent => agent.IsAlive
                    && string.Equals(
                        agent.Id,
                        source.Location.OwnerId.ToString(),
                        StringComparison.Ordinal));
                if (holder != null)
                {
                    AddNavigationRegion(
                        navigation,
                        new CellId(holder.CellX, holder.CellY, holder.CellZ),
                        reachableRegions);
                }
            }
            else if (source.Location.Kind == ItemLocationKind.World
                && source.Location.HasCell
                && navigation.TryGetRegion(
                    source.Location.CellId,
                    out int sourceRegion)
                && agents.Any(agent => agent.IsAlive
                    && navigation.TryGetRegion(
                        new CellId(agent.CellX, agent.CellY, agent.CellZ),
                        out int agentRegion)
                    && agentRegion == sourceRegion))
            {
                reachableRegions.Add(sourceRegion);
            }

            if (reachableRegions.Count == 0)
            {
                return Array.Empty<CellId>();
            }

            return supported
                .Where(cell => navigation.TryGetRegion(cell, out int region)
                    && reachableRegions.Contains(region))
                .OrderBy(cell => cell)
                .ToArray();
        }

        private bool TryLoadBuildingPlacementNavigation(
            out NavigationSnapshot navigation)
        {
            navigation = null!;
            if (RefreshNavigation().IsFailure)
            {
                return false;
            }

            NavigationMap? map = _navigationRepository.Get(_profile.Id);
            if (map == null)
            {
                return false;
            }

            Result<NavigationSnapshot> snapshot = map.GetSnapshot();
            if (snapshot.IsFailure)
            {
                return false;
            }

            navigation = snapshot.Value;
            return true;
        }

        private static void AddNavigationRegion(
            NavigationSnapshot navigation,
            CellId cell,
            ISet<int> regions)
        {
            if (navigation.TryGetRegion(cell, out int region))
            {
                regions.Add(region);
            }
        }

    }
}
