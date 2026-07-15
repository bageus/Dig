using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Runtime;
using Dig.Domain.World;

namespace Dig.Domain.Generation
{

internal static partial class WorldGenerationLayout
{
    public static IReadOnlyList<GeneratedZonePlan> CreateZones(
        WorldGenerationProfile profile,
        DeterministicRandomStream startStream,
        DeterministicRandomStream zoneStream,
        DeterministicRandomStream biomeStream)
    {
        int margin = Math.Max(profile.StartRoomRadius, profile.ZoneRoomRadius) + 3;
        int usableWidth = profile.Size.Width - (margin * 2);
        int usableHeight = profile.Size.Height - (margin * 2);
        List<GeneratedZonePlan> zones = new List<GeneratedZonePlan>(profile.ZoneCount);

        for (int index = 0; index < profile.ZoneCount; index++)
        {
            int bandStart = margin + ((index * usableWidth) / profile.ZoneCount);
            int bandEnd = margin + (((index + 1) * usableWidth) / profile.ZoneCount);
            DeterministicRandomStream positionStream = index == 0 ? startStream : zoneStream;
            int x = bandEnd > bandStart
                ? positionStream.NextInt(bandStart, bandEnd)
                : bandStart;
            int y = positionStream.NextInt(margin, margin + usableHeight);
            WorldGenerationBiomeDefinition biome = profile.Biomes[
                biomeStream.NextInt(profile.Biomes.Count)];
            int layer = Math.Min(
                profile.LayerCount - 1,
                ((y - margin) * profile.LayerCount) / usableHeight);
            zones.Add(new GeneratedZonePlan(index, new CellId(x, y), biome, layer));
        }

        return new ReadOnlyCollection<GeneratedZonePlan>(zones);
    }

    public static GenerationCellBuffer CreateSolidBuffer(
        WorldGenerationProfile profile,
        IReadOnlyList<GeneratedZonePlan> zones)
    {
        CellState[] cells = new CellState[checked(profile.Size.Width * profile.Size.Height)];
        for (int y = 0; y < profile.Size.Height; y++)
        {
            for (int x = 0; x < profile.Size.Width; x++)
            {
                CellId cellId = new CellId(x, y);
                GeneratedZonePlan nearest = FindNearestZone(cellId, zones);
                cells[(y * profile.Size.Width) + x] = new CellState(
                    nearest.Biome.SolidMaterialId,
                    CellDesignation.None,
                    isExplored: false,
                    damage: 0,
                    temperature: checked((short)(20 - (nearest.LayerIndex * 3))));
            }
        }

        return new GenerationCellBuffer(profile.Size, cells);
    }

    public static IReadOnlyList<GeneratedZoneConnection> CarveConnectedZones(
        GenerationCellBuffer buffer,
        WorldGenerationProfile profile,
        IReadOnlyList<GeneratedZonePlan> zones,
        DeterministicRandomStream connectionStream)
    {
        for (int index = 0; index < zones.Count; index++)
        {
            int radius = index == 0 ? profile.StartRoomRadius : profile.ZoneRoomRadius;
            CarveRoom(
                buffer,
                zones[index].Center,
                radius,
                profile.EmptyMaterialId,
                explored: index == 0);
        }

        List<GeneratedZoneConnection> connections = new List<GeneratedZoneConnection>();
        for (int index = 1; index < zones.Count; index++)
        {
            GeneratedZonePlan source = zones[index];
            GeneratedZonePlan target = FindNearestPreviousZone(source, zones);
            CarveCorridor(
                buffer,
                source.Center,
                target.Center,
                profile.EmptyMaterialId,
                profile.CorridorHalfWidth,
                horizontalFirst: connectionStream.NextInt(2) == 0);
            connections.Add(new GeneratedZoneConnection(source.Index, target.Index));
        }

        return new ReadOnlyCollection<GeneratedZoneConnection>(connections);
    }

    public static IReadOnlyList<CellId> PlaceResources(
        GenerationCellBuffer buffer,
        WorldGenerationProfile profile,
        IReadOnlyList<GeneratedZonePlan> zones,
        DeterministicRandomStream resourceStream)
    {
        GeneratedZonePlan start = zones[0];
        List<CellId> startCandidates = CollectSolidRing(
            buffer,
            start.Center,
            profile.StartRoomRadius + 1,
            profile.StartRoomRadius + 4,
            profile.EmptyMaterialId);
        Shuffle(startCandidates, resourceStream);

        int startCount = Math.Min(profile.MinimumStartingResources, startCandidates.Count);
        List<CellId> startingResources = new List<CellId>(startCount);
        HashSet<CellId> occupied = new HashSet<CellId>();
        for (int index = 0; index < startCount; index++)
        {
            CellId cellId = startCandidates[index];
            SetResource(buffer, cellId, start.Biome.ResourceMaterialId);
            startingResources.Add(cellId);
            occupied.Add(cellId);
        }

        for (int zoneIndex = 1; zoneIndex < zones.Count; zoneIndex++)
        {
            GeneratedZonePlan zone = zones[zoneIndex];
            List<CellId> candidates = CollectSolidRing(
                buffer,
                zone.Center,
                profile.ZoneRoomRadius + 1,
                profile.ZoneRoomRadius + 4,
                profile.EmptyMaterialId);
            candidates.RemoveAll(cell => occupied.Contains(cell));
            Shuffle(candidates, resourceStream);
            int count = Math.Min(2, candidates.Count);
            for (int index = 0; index < count; index++)
            {
                CellId cellId = candidates[index];
                SetResource(buffer, cellId, zone.Biome.ResourceMaterialId);
                occupied.Add(cellId);
            }
        }

        startingResources.Sort();
        return new ReadOnlyCollection<CellId>(startingResources);
    }

    public static IReadOnlyList<CellId> SelectPointsOfInterest(
        GenerationCellBuffer buffer,
        WorldGenerationProfile profile,
        IReadOnlyList<GeneratedZonePlan> zones,
        DeterministicRandomStream pointStream)
    {
        CellId start = zones[0].Center;
        int exclusionRadius = profile.StartRoomRadius + 2;
        List<CellId> candidates = new List<CellId>();
        for (int y = 0; y < buffer.Size.Height; y++)
        {
            for (int x = 0; x < buffer.Size.Width; x++)
            {
                CellId cellId = new CellId(x, y);
                if (buffer.Get(cellId).MaterialId == profile.EmptyMaterialId
                    && ManhattanDistance(cellId, start) > exclusionRadius)
                {
                    candidates.Add(cellId);
                }
            }
        }

        Shuffle(candidates, pointStream);
        int count = Math.Min(profile.PointOfInterestCount, candidates.Count);
        CellId[] selected = candidates.Take(count).OrderBy(cell => cell).ToArray();
        return new ReadOnlyCollection<CellId>(selected);
    }

    private static GeneratedZonePlan FindNearestZone(
        CellId cellId,
        IReadOnlyList<GeneratedZonePlan> zones)
    {
        GeneratedZonePlan nearest = zones[0];
        int nearestDistance = SquaredDistance(cellId, nearest.Center);
        for (int index = 1; index < zones.Count; index++)
        {
            int distance = SquaredDistance(cellId, zones[index].Center);
            if (distance < nearestDistance)
            {
                nearest = zones[index];
                nearestDistance = distance;
            }
        }

        return nearest;
    }

    private static GeneratedZonePlan FindNearestPreviousZone(
        GeneratedZonePlan source,
        IReadOnlyList<GeneratedZonePlan> zones)
    {
        GeneratedZonePlan nearest = zones[0];
        int nearestDistance = ManhattanDistance(source.Center, nearest.Center);
        for (int index = 1; index < source.Index; index++)
        {
            int distance = ManhattanDistance(source.Center, zones[index].Center);
            if (distance < nearestDistance)
            {
                nearest = zones[index];
                nearestDistance = distance;
            }
        }

        return nearest;
    }
}

}
