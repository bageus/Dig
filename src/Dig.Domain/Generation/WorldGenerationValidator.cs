using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.World;

namespace Dig.Domain.Generation
{

public sealed class WorldGenerationValidationReport
{
    public WorldGenerationValidationReport(IEnumerable<string> failures)
    {
        if (failures is null)
        {
            throw new ArgumentNullException(nameof(failures));
        }

        Failures = new ReadOnlyCollection<string>(failures.ToArray());
    }

    public bool IsValid => Failures.Count == 0;

    public IReadOnlyList<string> Failures { get; }
}

public sealed class WorldGenerationValidator
{
    public WorldGenerationValidationReport Validate(
        GeneratedWorld generated,
        WorldGenerationProfile profile)
    {
        if (generated is null)
        {
            throw new ArgumentNullException(nameof(generated));
        }

        if (profile is null)
        {
            throw new ArgumentNullException(nameof(profile));
        }

        List<string> failures = new List<string>();
        WorldState world = generated.World;
        GeneratedWorldMetadata metadata = generated.Metadata;

        ValidateIdentity(world, metadata, profile, failures);
        HashSet<CellId> reachable = FindReachableCells(world, metadata.StartCell, failures);
        ValidateCriticalCells(world, metadata, reachable, failures);
        ValidateStartingResources(world, metadata, profile, failures);
        return new WorldGenerationValidationReport(failures);
    }

    private static void ValidateIdentity(
        WorldState world,
        GeneratedWorldMetadata metadata,
        WorldGenerationProfile profile,
        List<string> failures)
    {
        if (world.Size.Width != profile.Size.Width
            || world.Size.Height != profile.Size.Height
            || world.Layout.ChunkSize != profile.ChunkSize)
        {
            failures.Add("Generated world dimensions do not match the profile.");
        }

        if (metadata.GeneratorVersion != profile.GeneratorVersion
            || !string.Equals(metadata.ProfileId, profile.Id, StringComparison.Ordinal))
        {
            failures.Add("Generated metadata does not match the generation profile.");
        }

        if (metadata.Zones.Count != profile.ZoneCount)
        {
            failures.Add("Generated zone count does not match the profile.");
        }

        if (metadata.Connections.Count != Math.Max(0, profile.ZoneCount - 1))
        {
            failures.Add("Generated macro graph does not connect every non-start zone.");
        }

        if (metadata.PointsOfInterest.Count != profile.PointOfInterestCount)
        {
            failures.Add("Generated point-of-interest count does not match the profile.");
        }

        if (world.Version != 0
            || world.PeekDirtyChunks().Count != 0
            || world.PeekUncommittedEvents().Count != 0)
        {
            failures.Add("Initial generation must not look like post-creation world mutations.");
        }
    }

    private static HashSet<CellId> FindReachableCells(
        WorldState world,
        CellId start,
        List<string> failures)
    {
        HashSet<CellId> visited = new HashSet<CellId>();
        if (!world.Size.Contains(start) || world.GetCell(start).Value.IsSolid)
        {
            failures.Add("Start cell must be an in-bounds empty cell.");
            return visited;
        }

        Queue<CellId> frontier = new Queue<CellId>();
        frontier.Enqueue(start);
        visited.Add(start);
        while (frontier.Count > 0)
        {
            CellId current = frontier.Dequeue();
            Visit(world, new CellId(current.X - 1, current.Y), visited, frontier);
            Visit(world, new CellId(current.X + 1, current.Y), visited, frontier);
            Visit(world, new CellId(current.X, current.Y - 1), visited, frontier);
            Visit(world, new CellId(current.X, current.Y + 1), visited, frontier);
        }

        return visited;
    }

    private static void Visit(
        WorldState world,
        CellId cellId,
        HashSet<CellId> visited,
        Queue<CellId> frontier)
    {
        if (!world.Size.Contains(cellId) || visited.Contains(cellId))
        {
            return;
        }

        CellSnapshot cell = world.GetCell(cellId).Value;
        if (cell.IsSolid)
        {
            return;
        }

        visited.Add(cellId);
        frontier.Enqueue(cellId);
    }

    private static void ValidateCriticalCells(
        WorldState world,
        GeneratedWorldMetadata metadata,
        HashSet<CellId> reachable,
        List<string> failures)
    {
        foreach (GeneratedZone zone in metadata.Zones)
        {
            if (!world.Size.Contains(zone.Center) || !reachable.Contains(zone.Center))
            {
                failures.Add($"Zone {zone.Index} is not reachable from the start cell.");
            }
        }

        foreach (CellId point in metadata.PointsOfInterest)
        {
            if (!world.Size.Contains(point) || !reachable.Contains(point))
            {
                failures.Add($"Point of interest {point} is not reachable from the start cell.");
            }
        }
    }

    private static void ValidateStartingResources(
        WorldState world,
        GeneratedWorldMetadata metadata,
        WorldGenerationProfile profile,
        List<string> failures)
    {
        if (metadata.StartingResourceCells.Count < profile.MinimumStartingResources)
        {
            failures.Add("The guaranteed starting resource count is below the profile minimum.");
        }

        HashSet<MaterialId> resourceMaterials = new HashSet<MaterialId>(
            profile.Biomes.Select(biome => biome.ResourceMaterialId));
        foreach (CellId cellId in metadata.StartingResourceCells)
        {
            if (!world.Size.Contains(cellId))
            {
                failures.Add($"Starting resource {cellId} is outside world bounds.");
                continue;
            }

            CellSnapshot cell = world.GetCell(cellId).Value;
            if (!cell.IsSolid || !resourceMaterials.Contains(cell.State.MaterialId))
            {
                failures.Add($"Starting resource {cellId} is not a valid resource cell.");
            }
        }
    }
}

}
