using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.World;

namespace Dig.Domain.Generation
{

public readonly struct GeneratedZone
{
    public GeneratedZone(
        int index,
        CellId center,
        string biomeId,
        int layerIndex)
    {
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (string.IsNullOrWhiteSpace(biomeId))
        {
            throw new ArgumentException("Biome id is required.", nameof(biomeId));
        }

        if (layerIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(layerIndex));
        }

        Index = index;
        Center = center;
        BiomeId = biomeId;
        LayerIndex = layerIndex;
    }

    public int Index { get; }

    public CellId Center { get; }

    public string BiomeId { get; }

    public int LayerIndex { get; }
}

public readonly struct GeneratedZoneConnection
{
    public GeneratedZoneConnection(int fromZoneIndex, int toZoneIndex)
    {
        if (fromZoneIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fromZoneIndex));
        }

        if (toZoneIndex < 0 || toZoneIndex >= fromZoneIndex)
        {
            throw new ArgumentOutOfRangeException(nameof(toZoneIndex));
        }

        FromZoneIndex = fromZoneIndex;
        ToZoneIndex = toZoneIndex;
    }

    public int FromZoneIndex { get; }

    public int ToZoneIndex { get; }
}

public sealed class GeneratedWorldMetadata
{
    public GeneratedWorldMetadata(
        ulong worldSeed,
        int generatorVersion,
        string profileId,
        CellId startCell,
        IEnumerable<GeneratedZone> zones,
        IEnumerable<GeneratedZoneConnection> connections,
        IEnumerable<CellId> startingResourceCells,
        IEnumerable<CellId> pointsOfInterest,
        ulong fingerprint)
    {
        if (generatorVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(generatorVersion));
        }

        if (string.IsNullOrWhiteSpace(profileId))
        {
            throw new ArgumentException("Profile id is required.", nameof(profileId));
        }

        if (zones is null)
        {
            throw new ArgumentNullException(nameof(zones));
        }

        if (connections is null)
        {
            throw new ArgumentNullException(nameof(connections));
        }

        if (startingResourceCells is null)
        {
            throw new ArgumentNullException(nameof(startingResourceCells));
        }

        if (pointsOfInterest is null)
        {
            throw new ArgumentNullException(nameof(pointsOfInterest));
        }

        WorldSeed = worldSeed;
        GeneratorVersion = generatorVersion;
        ProfileId = profileId.Trim();
        StartCell = startCell;
        Zones = new ReadOnlyCollection<GeneratedZone>(zones.OrderBy(zone => zone.Index).ToArray());
        Connections = new ReadOnlyCollection<GeneratedZoneConnection>(
            connections.OrderBy(connection => connection.FromZoneIndex).ToArray());
        StartingResourceCells = new ReadOnlyCollection<CellId>(
            startingResourceCells.OrderBy(cell => cell).ToArray());
        PointsOfInterest = new ReadOnlyCollection<CellId>(
            pointsOfInterest.OrderBy(cell => cell).ToArray());
        Fingerprint = fingerprint;
    }

    public ulong WorldSeed { get; }

    public int GeneratorVersion { get; }

    public string ProfileId { get; }

    public CellId StartCell { get; }

    public IReadOnlyList<GeneratedZone> Zones { get; }

    public IReadOnlyList<GeneratedZoneConnection> Connections { get; }

    public IReadOnlyList<CellId> StartingResourceCells { get; }

    public IReadOnlyList<CellId> PointsOfInterest { get; }

    public ulong Fingerprint { get; }
}

public sealed class GeneratedWorld
{
    public GeneratedWorld(WorldState world, GeneratedWorldMetadata metadata)
    {
        World = world ?? throw new ArgumentNullException(nameof(world));
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
    }

    public WorldState World { get; }

    public GeneratedWorldMetadata Metadata { get; }
}

}
