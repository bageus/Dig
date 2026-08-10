using System;
using System.Collections.Generic;
using Dig.Domain.Core;
using Dig.Domain.Production;
using Dig.Infrastructure.InMemory;
using Dig.Presentation.Agents;
using Dig.Presentation.Buildings;
using Dig.Presentation.Creatures;
using Dig.Presentation.Rendering;
using Dig.Presentation.World;
using UnityEngine;

namespace Dig.Unity
{
[DisallowMultipleComponent]
public sealed class DigPresentationEffectRuntime : MonoBehaviour
{
    // AmbientDustIntervalTicks / PresentationEffectKind.AmbientDust are intentionally
    // retained only as contract names; runtime no longer emits ambient sky particles.
    private readonly PresentationDomainEffectProjector _projector =
        new PresentationDomainEffectProjector();
    private readonly Dictionary<string, PresentationEffectFact> _queued =
        new Dictionary<string, PresentationEffectFact>(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _activeProductionOrders =
        new Dictionary<string, string>(StringComparer.Ordinal);
    private readonly Dictionary<string, PresentationEffectLocation> _locations =
        new Dictionary<string, PresentationEffectLocation>(StringComparer.Ordinal);
    private readonly HashSet<string> _campfireBuildingIds =
        new HashSet<string>(StringComparer.Ordinal);
    private DigWorldSession? _world;
    private DigAgentSession? _agents;
    private DigTerrainWorkSession? _terrain;
    private InMemoryExecutionJournal? _journal;
    private DigPresentationEffectBridge? _bridge;
    private Camera? _camera;
    private long _nextEventOrdinal;

    public int QueuedFactCount => _queued.Count;
    public int ActiveProductionEmitterCount => _activeProductionOrders.Count;

    internal void Initialize(
        DigWorldSession world,
        DigAgentSession agents,
        DigTerrainWorkSession terrain,
        DigPresentationEffectBridge bridge,
        Camera camera)
    {
        _world = world ?? throw new ArgumentNullException(nameof(world));
        _agents = agents ?? throw new ArgumentNullException(nameof(agents));
        _terrain = terrain ?? throw new ArgumentNullException(nameof(terrain));
        _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
        _camera = camera ?? throw new ArgumentNullException(nameof(camera));
        _journal = world.Journal;
        _nextEventOrdinal = _journal.DroppedEventCount;
    }

    public void Publish(IReadOnlyList<PresentationEffectFact> facts)
    {
        if (facts == null) throw new ArgumentNullException(nameof(facts));
        for (int index = 0; index < facts.Count; index++)
        {
            PresentationEffectFact fact = facts[index]
                ?? throw new ArgumentException("Presentation fact cannot be null.", nameof(facts));
            if (!_queued.TryGetValue(fact.EventId, out PresentationEffectFact? existing)
                || fact.Version >= existing.Version)
            {
                _queued[fact.EventId] = fact;
            }
        }
    }

    internal void Flush(long tick)
    {
        EnsureInitialized();
        IReadOnlyList<IDomainEvent> events = ReadNewEvents();
        TrackProductionEmitters(events);
        RebuildLocations();
        IReadOnlyList<IDomainEvent> visibleEvents =
            FilterCampfireProductionParticleEvents(events);
        Publish(_projector.Project(visibleEvents, ResolveLocation));

        Dictionary<string, PresentationEffectFact> frame =
            new Dictionary<string, PresentationEffectFact>(_queued, StringComparer.Ordinal);
        AddPersistentEmitters(frame);
        _bridge!.Present(new List<PresentationEffectFact>(frame.Values), _camera);
        _queued.Clear();
    }

    private IReadOnlyList<IDomainEvent> ReadNewEvents()
    {
        IReadOnlyList<IDomainEvent> events = _journal!.Events;
        long firstOrdinal = _journal.DroppedEventCount;
        if (_nextEventOrdinal < firstOrdinal) _nextEventOrdinal = firstOrdinal;
        int start = (int)Math.Min(events.Count, _nextEventOrdinal - firstOrdinal);
        IDomainEvent[] result = new IDomainEvent[events.Count - start];
        for (int index = start; index < events.Count; index++)
            result[index - start] = events[index];
        _nextEventOrdinal = checked(firstOrdinal + events.Count);
        return result;
    }

    private void TrackProductionEmitters(IReadOnlyList<IDomainEvent> events)
    {
        for (int index = 0; index < events.Count; index++)
        {
            if (events[index] is ProductionWorkApplied work)
            {
                _activeProductionOrders[work.OrderId.ToString()] =
                    work.BuildingId.ToString();
            }
            else if (events[index] is ProductionOrderStatusChanged status)
            {
                string orderId = status.OrderId.ToString();
                if (status.Current is ProductionOrderStatus.InProgress
                    or ProductionOrderStatus.ReadyToComplete)
                {
                    _activeProductionOrders[orderId] = status.BuildingId.ToString();
                }
                else
                {
                    _activeProductionOrders.Remove(orderId);
                }
            }
        }
    }

    private void RebuildLocations()
    {
        _locations.Clear();
        _campfireBuildingIds.Clear();
        IReadOnlyList<AgentViewModel> agents = _agents!.LoadView();
        for (int index = 0; index < agents.Count; index++)
        {
            AgentViewModel agent = agents[index];
            Vector3 position = DigTunnelProjection.ResidentWorldPosition(
                agent.CellX,
                agent.CellY,
                agent.CellZ);
            _locations[agent.Id] = Location(position);
        }

        IReadOnlyList<CreatureVisualSnapshot> enemies = _agents.LoadEnemyCreatures();
        for (int index = 0; index < enemies.Count; index++)
        {
            CreatureVisualSnapshot enemy = enemies[index];
            Vector3 position = DigTunnelProjection.ResidentWorldPosition(
                enemy.CellX,
                enemy.CellY,
                enemy.CellZ);
            _locations[enemy.CreatureId] = Location(position);
        }

        IReadOnlyList<BuildingWorldViewModel> buildings = _terrain!.LoadBuildings();
        for (int index = 0; index < buildings.Count; index++)
        {
            BuildingWorldViewModel building = buildings[index];
            _locations[building.Id] = new PresentationEffectLocation(
                building.OriginX,
                0d,
                building.OriginY);
            if (IsStableKind(building.DefinitionId, "campfire"))
            {
                _campfireBuildingIds.Add(building.Id);
            }
        }
    }

    private IReadOnlyList<IDomainEvent> FilterCampfireProductionParticleEvents(
        IReadOnlyList<IDomainEvent> events)
    {
        List<IDomainEvent>? filtered = null;
        for (int index = 0; index < events.Count; index++)
        {
            IDomainEvent item = events[index];
            bool suppress = item is ProductionWorkApplied production
                && _campfireBuildingIds.Contains(production.BuildingId.ToString());
            if (suppress)
            {
                if (filtered == null)
                {
                    filtered = new List<IDomainEvent>(events.Count);
                    for (int prior = 0; prior < index; prior++)
                    {
                        filtered.Add(events[prior]);
                    }
                }

                continue;
            }

            filtered?.Add(item);
        }

        return filtered ?? events;
    }

    private PresentationEffectLocation? ResolveLocation(EntityId id)
    {
        return _locations.TryGetValue(id.ToString(), out PresentationEffectLocation value)
            ? value
            : (PresentationEffectLocation?)null;
    }

    private void AddPersistentEmitters(
        IDictionary<string, PresentationEffectFact> frame)
    {
        AddTerrainEmitters(frame);
        AddBuildingEmitters(frame);
    }

    private void AddTerrainEmitters(IDictionary<string, PresentationEffectFact> frame)
    {
        WorldViewModel world = _world!.LoadView();
        for (int chunkIndex = 0; chunkIndex < world.Chunks.Count; chunkIndex++)
        {
            IReadOnlyList<WorldCellViewModel> cells = world.Chunks[chunkIndex].Cells;
            for (int cellIndex = 0; cellIndex < cells.Count; cellIndex++)
            {
                WorldCellViewModel cell = cells[cellIndex];
                if (!cell.IsExplored || !IsStableKind(cell.MaterialId, "lava")) continue;
                Add(frame, new PresentationEffectFact(
                    $"lava:{cell.X}:{cell.Y}",
                    PresentationEffectKind.LavaGlow,
                    cell.X,
                    0d,
                    cell.Y,
                    0.8d,
                    cell.WorldVersion));
            }
        }

        TerrainDepositVolumeViewModel deposits = _world.LoadTerrainDeposits();
        for (int index = 0; index < deposits.Cells.Count; index++)
        {
            TerrainDepositCellViewModel deposit = deposits.Cells[index];
            if (!deposit.IsVisible
                || !IsStableKind(deposit.VisibleDepositId, "crystal")) continue;
            Add(frame, new PresentationEffectFact(
                "crystal:" + deposit.Cell,
                PresentationEffectKind.CrystalGlow,
                deposit.Cell.X,
                0d,
                deposit.Cell.Y,
                0.65d,
                deposit.SourceVersion));
        }
    }

    private void AddBuildingEmitters(IDictionary<string, PresentationEffectFact> frame)
    {
        HashSet<string> activeBuildings = new HashSet<string>(
            _activeProductionOrders.Values,
            StringComparer.Ordinal);
        IReadOnlyList<BuildingWorldViewModel> buildings = _terrain!.LoadBuildings();
        for (int index = 0; index < buildings.Count; index++)
        {
            BuildingWorldViewModel building = buildings[index];
            if (!building.IsSelectable) continue;
            PresentationEffectKind? kind = IsStableKind(building.DefinitionId, "campfire")
                ? PresentationEffectKind.CampfireGlow
                : activeBuildings.Contains(building.Id)
                    ? PresentationEffectKind.ProductionBuildingGlow
                    : (PresentationEffectKind?)null;
            if (!kind.HasValue) continue;
            Add(frame, new PresentationEffectFact(
                "building-emitter:" + building.Id,
                kind.Value,
                building.OriginX,
                0d,
                building.OriginY,
                0.75d,
                building.Version));
        }
    }

    private static void Add(
        IDictionary<string, PresentationEffectFact> frame,
        PresentationEffectFact fact)
    {
        frame[fact.EventId] = fact;
    }

    private static bool IsStableKind(string stableId, string kind)
    {
        return stableId.Equals(kind, StringComparison.OrdinalIgnoreCase)
            || stableId.EndsWith("." + kind, StringComparison.OrdinalIgnoreCase)
            || stableId.IndexOf("." + kind + ".", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static PresentationEffectLocation Location(Vector3 value)
    {
        return new PresentationEffectLocation(value.x, value.y, value.z);
    }

    private void EnsureInitialized()
    {
        if (_world == null || _agents == null || _terrain == null
            || _journal == null || _bridge == null || _camera == null)
        {
            throw new InvalidOperationException("Presentation effect runtime is not initialized.");
        }
    }
}
}
