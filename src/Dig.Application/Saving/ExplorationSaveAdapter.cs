using System;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Exploration;
using Dig.Domain.Inventory;
using Dig.Domain.World;

namespace Dig.Application.Saving
{
public static class ExplorationSaveAdapter
{
    public static ExplorationSaveData Encode(ExplorationState state)
    {
        if (state is null) throw new ArgumentNullException(nameof(state));
        ExplorationSaveSnapshot snapshot = state.CreateSaveSnapshot();
        ExplorationSaveData data = new ExplorationSaveData
        {
            SchemaVersion = snapshot.SchemaVersion,
        };
        data.Explored.AddRange(snapshot.Explored.Select(cell => new ExploredCellSaveData
        {
            X = cell.X, Y = cell.Y, Z = cell.Z,
        }));
        data.ItemMarkers.AddRange(snapshot.Markers.Select(marker => new WorldItemMemorySaveData
        {
            StackId = marker.StackId.ToString(), ItemId = marker.ItemId.ToString(),
            X = marker.Cell.X, Y = marker.Cell.Y, Z = marker.Cell.Z,
            ObservedTick = marker.ObservedTick,
        }));
        return data;
    }

    public static ExplorationState Decode(ExplorationSaveData? data, WorldSize size)
    {
        if (data is null) return new ExplorationState();
        if (data.SchemaVersion != 1 || data.Explored is null || data.ItemMarkers is null)
            throw new InvalidOperationException("Exploration save data is invalid.");
        CellId[] explored = data.Explored.Select(cell => Cell(cell.X, cell.Y, cell.Z, size)).ToArray();
        LastKnownWorldItemMarker[] markers = data.ItemMarkers.Select(marker =>
            new LastKnownWorldItemMarker(
                EntityId.Parse(marker.StackId), new ItemId(marker.ItemId),
                Cell(marker.X, marker.Y, marker.Z, size), marker.ObservedTick)).ToArray();
        return ExplorationState.Restore(new ExplorationSaveSnapshot(
            data.SchemaVersion, explored, markers));
    }

    private static CellId Cell(int x, int y, int z, WorldSize size)
    {
        CellId cell = new CellId(x, y, z);
        if (!size.Contains(cell)) throw new InvalidOperationException("Exploration cell is outside the world.");
        return cell;
    }
}
}
