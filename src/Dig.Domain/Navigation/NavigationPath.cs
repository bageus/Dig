using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Collections.ObjectModel;
using Dig.Domain.World;

namespace Dig.Domain.Navigation
{

public enum PathFailureReason
{
    None = 0,
    InvalidStart = 1,
    InvalidGoal = 2,
    BlockedStart = 3,
    BlockedGoal = 4,
    Unreachable = 5,
    StaleSnapshot = 6,
    SearchLimitExceeded = 7,
}

public sealed class PathRequest
{
    public PathRequest(
        CellId start,
        CellId goal,
        long expectedNavigationVersion,
        int maxExpandedNodes = 100000)
    {
        if (expectedNavigationVersion < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(expectedNavigationVersion));
        }

        if (maxExpandedNodes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxExpandedNodes));
        }

        Start = start;
        Goal = goal;
        ExpectedNavigationVersion = expectedNavigationVersion;
        MaxExpandedNodes = maxExpandedNodes;
    }

    public CellId Start { get; }

    public CellId Goal { get; }

    public long ExpectedNavigationVersion { get; }

    public int MaxExpandedNodes { get; }
}

public sealed class PathSearchDiagnostics
{
    public PathSearchDiagnostics(
        PathFailureReason failureReason,
        int expandedNodes,
        int? startRegion,
        int? goalRegion,
        long snapshotVersion,
        string detail)
    {
        if (expandedNodes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(expandedNodes));
        }

        if (snapshotVersion < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(snapshotVersion));
        }

        FailureReason = failureReason;
        ExpandedNodes = expandedNodes;
        StartRegion = startRegion;
        GoalRegion = goalRegion;
        SnapshotVersion = snapshotVersion;
        Detail = detail ?? string.Empty;
    }

    public PathFailureReason FailureReason { get; }

    public int ExpandedNodes { get; }

    public int? StartRegion { get; }

    public int? GoalRegion { get; }

    public long SnapshotVersion { get; }

    public string Detail { get; }
}

public sealed class NavigationPath
{
    public NavigationPath(
        TraversalProfileId profileId,
        long worldVersion,
        long navigationVersion,
        long linkVersion,
        int totalCost,
        IReadOnlyCollection<CellId> cells,
        IReadOnlyCollection<NavigationChunkStamp> chunkStamps)
    {
        if (profileId.IsEmpty)
        {
            throw new ArgumentException("Traversal profile id cannot be empty.", nameof(profileId));
        }

        if (worldVersion < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(worldVersion));
        }

        if (navigationVersion < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(navigationVersion));
        }

        if (linkVersion < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(linkVersion));
        }

        if (totalCost < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalCost));
        }

        if (cells is null)
        {
            throw new ArgumentNullException(nameof(cells));
        }

        if (chunkStamps is null)
        {
            throw new ArgumentNullException(nameof(chunkStamps));
        }

        CellId[] pathCells = cells.ToArray();
        if (pathCells.Length == 0)
        {
            throw new ArgumentException("A navigation path must contain cells.", nameof(cells));
        }

        ProfileId = profileId;
        WorldVersion = worldVersion;
        NavigationVersion = navigationVersion;
        LinkVersion = linkVersion;
        TotalCost = totalCost;
        Cells = new ReadOnlyCollection<CellId>(pathCells);
        ChunkStamps = new ReadOnlyCollection<NavigationChunkStamp>(
            chunkStamps.OrderBy(stamp => stamp.ChunkId).ToArray());
    }

    public TraversalProfileId ProfileId { get; }

    public long WorldVersion { get; }

    public long NavigationVersion { get; }

    public long LinkVersion { get; }

    public int TotalCost { get; }

    public IReadOnlyList<CellId> Cells { get; }

    public IReadOnlyList<NavigationChunkStamp> ChunkStamps { get; }

    public bool IsValidFor(NavigationSnapshot snapshot)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        if (ProfileId != snapshot.Profile.Id || LinkVersion != snapshot.LinkVersion)
        {
            return false;
        }

        foreach (NavigationChunkStamp expected in ChunkStamps)
        {
            if (!snapshot.TryGetChunkStamp(expected.ChunkId, out NavigationChunkStamp current)
                || current != expected)
            {
                return false;
            }
        }

        return true;
    }
}

public sealed class PathResult
{
    private PathResult(
        NavigationPath? path,
        PathSearchDiagnostics diagnostics)
    {
        Path = path;
        Diagnostics = diagnostics;
    }

    public bool Succeeded => Path is not null;

    public NavigationPath? Path { get; }

    public PathSearchDiagnostics Diagnostics { get; }

    public static PathResult Success(
        NavigationPath path,
        PathSearchDiagnostics diagnostics)
    {
        return new PathResult(
            path ?? throw new ArgumentNullException(nameof(path)),
            diagnostics ?? throw new ArgumentNullException(nameof(diagnostics)));
    }

    public static PathResult Failure(PathSearchDiagnostics diagnostics)
    {
        return new PathResult(
            null,
            diagnostics ?? throw new ArgumentNullException(nameof(diagnostics)));
    }
}
}
