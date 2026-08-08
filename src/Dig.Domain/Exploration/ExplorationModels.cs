using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;

namespace Dig.Domain.Exploration
{
public enum CellVisibility { Unexplored = 0, ExploredNotVisible = 1, Visible = 2 }

public enum VisionSourceKind
{
    Resident = 0, Building = 1, DamagedBuilding = 2, Ladder = 3,
    Lift = 4, Door = 5, Trap = 6, Grave = 7,
}

public sealed class VisionSourceSnapshot
{
    public VisionSourceSnapshot(
        string id,
        VisionSourceKind kind,
        IEnumerable<CellId> origins,
        int? radius = null)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Source id is required.", nameof(id));
        if (origins is null) throw new ArgumentNullException(nameof(origins));
        CellId[] cells = origins.Distinct().OrderBy(value => value).ToArray();
        if (cells.Length == 0) throw new ArgumentException("A source requires an origin.", nameof(origins));
        int resolved = radius ?? ExplorationRadii.For(kind);
        if (resolved < 0) throw new ArgumentOutOfRangeException(nameof(radius));
        Id = id.Trim(); Kind = kind; Origins = new ReadOnlyCollection<CellId>(cells); Radius = resolved;
    }
    public string Id { get; }
    public VisionSourceKind Kind { get; }
    public IReadOnlyList<CellId> Origins { get; }
    public int Radius { get; }
}

public static class ExplorationRadii
{
    public static int For(VisionSourceKind kind) => kind switch
    {
        VisionSourceKind.Resident => 10,
        VisionSourceKind.Building => 10,
        VisionSourceKind.Grave => 5,
        VisionSourceKind.DamagedBuilding => 2,
        VisionSourceKind.Ladder => 2,
        VisionSourceKind.Lift => 2,
        VisionSourceKind.Door => 2,
        VisionSourceKind.Trap => 2,
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };
}

public readonly struct LastKnownWorldItemMarker
{
    public LastKnownWorldItemMarker(EntityId stackId, ItemId itemId, CellId cell, long observedTick)
    {
        if (stackId.IsEmpty) throw new ArgumentException("Stack id is required.", nameof(stackId));
        if (itemId.IsEmpty) throw new ArgumentException("Item id is required.", nameof(itemId));
        if (observedTick < 0) throw new ArgumentOutOfRangeException(nameof(observedTick));
        StackId = stackId; ItemId = itemId; Cell = cell; ObservedTick = observedTick;
    }
    public EntityId StackId { get; }
    public ItemId ItemId { get; }
    public CellId Cell { get; }
    public long ObservedTick { get; }
}
}
