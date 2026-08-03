using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Application.World;
using Dig.Domain.Core;
using Dig.Domain.World;

namespace Dig.Application.Tunnels
{

public sealed class TunnelRuntimeTopologyProjector
{
    public IReadOnlyList<TunnelTopologySegmentProvenance> Project(
        WorldSnapshot world,
        IEnumerable<CaveRoomPlan> completedRooms,
        IEnumerable<CellId> plannedTunnelCells,
        IEnumerable<CellId> plannedVerticalCells)
    {
        if (world is null)
        {
            throw new ArgumentNullException(nameof(world));
        }

        if (completedRooms is null)
        {
            throw new ArgumentNullException(nameof(completedRooms));
        }

        if (plannedTunnelCells is null)
        {
            throw new ArgumentNullException(nameof(plannedTunnelCells));
        }

        if (plannedVerticalCells is null)
        {
            throw new ArgumentNullException(nameof(plannedVerticalCells));
        }

        Dictionary<CellId, CellSnapshot> cells = world.Chunks
            .SelectMany(chunk => chunk.Cells)
            .ToDictionary(cell => cell.Id);
        HashSet<CellId> horizontal = Completed(plannedTunnelCells, cells);
        HashSet<CellId> vertical = Completed(plannedVerticalCells, cells);
        CaveRoomPlan[] rooms = completedRooms
            .OrderBy(room => room.Entrance)
            .ThenBy(room => room.Preset.Id, StringComparer.Ordinal)
            .ToArray();

        Origin[] roomOrigins = ResolveRoomOrigins(rooms);
        HashSet<CellId> roomExitCells = roomOrigins
            .Select(origin => origin.Cell)
            .ToHashSet();
        HashSet<CellId> junctions = vertical
            .Where(horizontal.Contains)
            .Where(cell => HasHorizontalNeighbour(cell, horizontal))
            .ToHashSet();
        HashSet<CellId> resetOrigins = roomExitCells
            .Concat(junctions)
            .ToHashSet();

        List<TunnelTopologySegmentProvenance> projected =
            new List<TunnelTopologySegmentProvenance>();
        foreach (Origin origin in roomOrigins
            .Concat(junctions
                .OrderBy(cell => cell)
                .SelectMany(cell => new[]
                {
                    new Origin(TunnelSegmentOriginKind.VerticalJunction, cell, -1),
                    new Origin(TunnelSegmentOriginKind.VerticalJunction, cell, 1),
                }))
            .OrderBy(value => value.Cell)
            .ThenBy(value => value.Kind)
            .ThenBy(value => value.Direction))
        {
            TraceResult trace = Trace(
                origin.Cell,
                origin.Direction,
                horizontal,
                resetOrigins);
            if (trace.Cells.Count == 0
                || !OwnsTrace(origin, trace.TerminalOrigin, roomExitCells, junctions))
            {
                continue;
            }

            projected.Add(new TunnelTopologySegmentProvenance(
                CreateSegmentId(origin),
                origin.Kind,
                origin.Cell,
                trace.Cells));
        }

        return new ReadOnlyCollection<TunnelTopologySegmentProvenance>(
            projected
                .OrderBy(value => value.Key)
                .ThenBy(value => value.SegmentId.ToString(), StringComparer.Ordinal)
                .ToArray());
    }

    private static HashSet<CellId> Completed(
        IEnumerable<CellId> planned,
        IReadOnlyDictionary<CellId, CellSnapshot> world)
    {
        return planned
            .Distinct()
            .Where(cell => world.TryGetValue(cell, out CellSnapshot snapshot)
                && (!snapshot.IsSolid || snapshot.State.IsExcavationOpen))
            .ToHashSet();
    }

    private static Origin[] ResolveRoomOrigins(
        IReadOnlyList<CaveRoomPlan> rooms)
    {
        List<Origin> origins = new List<Origin>(rooms.Count * 2);
        for (int index = 0; index < rooms.Count; index++)
        {
            CaveRoomPlan room = rooms[index];
            CellId[] baseCells = room.BaseTunnelCells
                .OrderBy(cell => cell.X)
                .ThenBy(cell => cell.Z)
                .ToArray();
            if (baseCells.Length < 2)
            {
                throw new ArgumentException(
                    "A completed cave room requires left and right base-tunnel provenance.",
                    nameof(rooms));
            }

            origins.Add(new Origin(
                TunnelSegmentOriginKind.RoomExit,
                baseCells[0],
                -1));
            origins.Add(new Origin(
                TunnelSegmentOriginKind.RoomExit,
                baseCells[baseCells.Length - 1],
                1));
        }

        return origins.ToArray();
    }

    private static bool HasHorizontalNeighbour(
        CellId cell,
        HashSet<CellId> horizontal)
    {
        return horizontal.Contains(new CellId(cell.X - 1, cell.Y, cell.Z))
            || horizontal.Contains(new CellId(cell.X + 1, cell.Y, cell.Z));
    }

    private static TraceResult Trace(
        CellId origin,
        int direction,
        HashSet<CellId> horizontal,
        HashSet<CellId> resetOrigins)
    {
        List<CellId> cells = new List<CellId>();
        CellId current = new CellId(origin.X + direction, origin.Y, origin.Z);
        while (horizontal.Contains(current))
        {
            if (resetOrigins.Contains(current))
            {
                return new TraceResult(cells, current);
            }

            cells.Add(current);
            current = new CellId(current.X + direction, current.Y, current.Z);
        }

        return new TraceResult(cells, terminalOrigin: null);
    }

    private static bool OwnsTrace(
        Origin origin,
        CellId? terminal,
        HashSet<CellId> roomExits,
        HashSet<CellId> junctions)
    {
        if (origin.Kind == TunnelSegmentOriginKind.RoomExit || !terminal.HasValue)
        {
            return true;
        }

        if (roomExits.Contains(terminal.Value))
        {
            return false;
        }

        return !junctions.Contains(terminal.Value)
            || origin.Cell.CompareTo(terminal.Value) < 0;
    }

    private static EntityId CreateSegmentId(Origin origin)
    {
        string value = string.Concat(
            ((int)origin.Kind).ToString(), ":",
            origin.Cell.X.ToString(), ":",
            origin.Cell.Y.ToString(), ":",
            origin.Cell.Z.ToString(), ":",
            origin.Direction.ToString());
        ulong first = Hash(value, 14695981039346656037UL);
        ulong second = Hash(value, 7809847782465536322UL);
        if (first == 0UL && second == 0UL)
        {
            second = 1UL;
        }

        return EntityId.Parse(first.ToString("x16") + second.ToString("x16"));
    }

    private static ulong Hash(string value, ulong seed)
    {
        const ulong prime = 1099511628211UL;
        ulong hash = seed;
        for (int index = 0; index < value.Length; index++)
        {
            char character = value[index];
            hash ^= (byte)character;
            hash *= prime;
            hash ^= (byte)(character >> 8);
            hash *= prime;
        }

        return hash;
    }

    private readonly struct Origin
    {
        public Origin(TunnelSegmentOriginKind kind, CellId cell, int direction)
        {
            Kind = kind;
            Cell = cell;
            Direction = direction;
        }

        public TunnelSegmentOriginKind Kind { get; }
        public CellId Cell { get; }
        public int Direction { get; }
    }

    private sealed class TraceResult
    {
        public TraceResult(IReadOnlyList<CellId> cells, CellId? terminalOrigin)
        {
            Cells = cells;
            TerminalOrigin = terminalOrigin;
        }

        public IReadOnlyList<CellId> Cells { get; }
        public CellId? TerminalOrigin { get; }
    }
}
}
