using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Exploration;
using Dig.Domain.World;
using Dig.Presentation.Agents;
using Dig.Presentation.Buildings;
using Dig.Domain.Buildings;
using Dig.Presentation.Inventory;
using Dig.Presentation.Creatures;
using Dig.Domain.Ecology;

namespace Dig.Unity
{
internal sealed partial class DigWorldSession
{
    internal bool UpdateExploration(
        IReadOnlyList<AgentViewModel> agents,
        IReadOnlyList<BuildingWorldViewModel>? buildings = null)
    {
        List<VisionSourceSnapshot> sources = agents
            .Where(agent => agent.IsAlive)
            .Select(agent => new VisionSourceSnapshot(
                agent.Id,
                VisionSourceKind.Resident,
                new[] { new CellId(agent.CellX, agent.CellY, agent.CellZ) }))
            .ToList();
        foreach (BuildingWorldViewModel building in buildings
            ?? Array.Empty<BuildingWorldViewModel>())
        {
            if (building.Status is not BuildingStatus.Completed and not BuildingStatus.Damaged)
                continue;
            VisionSourceKind kind = ResolveBuildingKind(building);
            sources.Add(new VisionSourceSnapshot(
                building.Id,
                kind,
                ResolveVisionOrigins(building, kind)));
        }
        if (!_exploration.Recalculate(LoadSnapshot(), sources)) return false;
        WorldState world = _repository.Get();
        List<TerrainChange> changes = new List<TerrainChange>();
        foreach (CellId cell in _exploration.Explored)
        {
            Result<CellSnapshot> current = world.GetCell(cell);
            if (current.IsSuccess && !current.Value.State.IsExplored)
                changes.Add(new TerrainChange(cell, current.Value.State.WithExplored(true)));
        }
        _tick++;
        Result<WorldMutationResult> applied = world.ApplyTerrainChanges(changes, _tick);
        if (applied.IsFailure) throw new InvalidOperationException(applied.Error!.ToString());
        _explorationChanged = true;
        return true;
    }

    internal bool ConsumeExplorationChanged()
    {
        bool changed = _explorationChanged;
        _explorationChanged = false;
        return changed;
    }

    internal bool IsCurrentlyVisible(CellId cell) => _exploration.IsVisible(cell);

    internal IReadOnlyList<WorldItemViewModel> FilterCurrentlyVisibleItems(
        IEnumerable<WorldItemViewModel> items) => items
        .Where(item => IsCurrentlyVisible(new CellId(item.CellX, item.CellY, item.CellZ)))
        .OrderBy(item => item.StackId, StringComparer.Ordinal)
        .ToArray();

    internal IReadOnlyList<CreatureVisualSnapshot> FilterCurrentlyVisibleCreatures(
        IEnumerable<CreatureVisualSnapshot> creatures) => creatures
        .Where(creature => IsCurrentlyVisible(
            new CellId(creature.CellX, creature.CellY, creature.CellZ)))
        .OrderBy(creature => creature.CreatureId, StringComparer.Ordinal)
        .ToArray();

    internal IReadOnlyList<MushroomSiteSnapshot> FilterCurrentlyVisibleMushrooms(
        IEnumerable<MushroomSiteSnapshot> mushrooms) => mushrooms
        .Where(mushroom => IsCurrentlyVisible(mushroom.Cell))
        .OrderBy(mushroom => mushroom.SiteId.ToString(), StringComparer.Ordinal)
        .ToArray();

    internal bool ObserveWorldItems(IEnumerable<WorldItemViewModel> items)
    {
        long before = _exploration.Version;
        _exploration.ObserveMarkers(items.Select(item => new LastKnownWorldItemMarker(
            EntityId.Parse(item.StackId), new Dig.Domain.Inventory.ItemId(item.ItemId),
            new CellId(item.CellX, item.CellY, item.CellZ), _tick)));
        if (_exploration.Version == before) return false;
        _explorationChanged = true;
        return true;
    }

    internal IReadOnlyList<WorldItemMemoryViewModel> LoadRememberedItems() =>
        _exploration.Markers
            .Where(marker => _exploration.GetVisibility(marker.Cell)
                == CellVisibility.ExploredNotVisible)
            .Select(marker => new WorldItemMemoryViewModel(
                marker.StackId.ToString(), marker.ItemId.ToString(),
                marker.Cell.X, marker.Cell.Y, marker.Cell.Z, marker.ObservedTick))
            .ToArray();

    private static VisionSourceKind ResolveBuildingKind(BuildingWorldViewModel building)
    {
        if (building.Status == BuildingStatus.Damaged) return VisionSourceKind.DamagedBuilding;
        string id = building.DefinitionId;
        if (id.IndexOf("ladder", StringComparison.OrdinalIgnoreCase) >= 0) return VisionSourceKind.Ladder;
        if (id.IndexOf("lift", StringComparison.OrdinalIgnoreCase) >= 0) return VisionSourceKind.Lift;
        if (id.IndexOf("door", StringComparison.OrdinalIgnoreCase) >= 0) return VisionSourceKind.Door;
        if (id.IndexOf("trap", StringComparison.OrdinalIgnoreCase) >= 0) return VisionSourceKind.Trap;
        if (id.IndexOf("grave", StringComparison.OrdinalIgnoreCase) >= 0) return VisionSourceKind.Grave;
        return VisionSourceKind.Building;
    }

    private static IReadOnlyList<CellId> ResolveVisionOrigins(
        BuildingWorldViewModel building,
        VisionSourceKind kind)
    {
        CellId[] footprint = building.Footprint
            .Select(cell => new CellId(cell.X, cell.Y, cell.Z)).Distinct().ToArray();
        return footprint.OrderBy(cell => cell).ToArray();
    }
}
}
