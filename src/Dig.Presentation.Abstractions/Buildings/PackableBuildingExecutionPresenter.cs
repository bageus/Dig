using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Application.Buildings;
using Dig.Domain.Buildings;
using Dig.Domain.Content;

namespace Dig.Presentation.Buildings
{

public sealed class PackableBuildingExecutionPresenter
{
    private readonly PackableBuildingContentCatalog _content;
    private readonly PackableBuildingVisualCatalog _visuals;

    public PackableBuildingExecutionPresenter(
        PackableBuildingContentCatalog content,
        PackableBuildingVisualCatalog visuals)
    {
        _content = content ?? throw new ArgumentNullException(nameof(content));
        _visuals = visuals ?? throw new ArgumentNullException(nameof(visuals));
    }

    public IReadOnlyList<PackableBuildingExecutionViewModel> Load(
        PackableBuildingExecutionRegistry executions,
        long tick)
    {
        if (executions == null)
        {
            throw new ArgumentNullException(nameof(executions));
        }

        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        PackableBuildingExecutionViewModel[] values = executions.CreateSnapshot()
            .Select(snapshot => Project(snapshot, tick))
            .OrderBy(value => value.PackageId, StringComparer.Ordinal)
            .ThenBy(value => value.OperationId, StringComparer.Ordinal)
            .ToArray();
        return new ReadOnlyCollection<PackableBuildingExecutionViewModel>(values);
    }

    private PackableBuildingExecutionViewModel Project(
        PackableBuildingExecutionSnapshot snapshot,
        long tick)
    {
        PackableBuildingContentDefinition content = _content.Get(snapshot.DefinitionId);
        PackableBuildingVisualProfile visuals = _visuals.Get(snapshot.DefinitionId);
        ResolveIterationProgress(
            snapshot.IterationClock,
            tick,
            out int elapsedSeconds,
            out int durationSeconds);
        return new PackableBuildingExecutionViewModel(
            snapshot.OperationId.ToString(),
            snapshot.PackageId.ToString(),
            snapshot.DefinitionId.ToString(),
            content.BoxItem.Id.ToString(),
            snapshot.Operation,
            snapshot.Status,
            snapshot.CompletedIterations,
            snapshot.TotalIterations,
            snapshot.ActiveWorkerId?.ToString(),
            elapsedSeconds,
            durationSeconds,
            ResolveVisualId(snapshot, visuals),
            visuals.IterationEffectId,
            ResolveStatusLabel(snapshot));
    }

    private static void ResolveIterationProgress(
        PackableBuildingIterationClockSnapshot? clock,
        long tick,
        out int elapsedSeconds,
        out int durationSeconds)
    {
        if (clock == null)
        {
            elapsedSeconds = 0;
            durationSeconds = 0;
            return;
        }

        durationSeconds = clock.DurationSeconds;
        long elapsed = Math.Max(0L, tick - clock.StartTick);
        elapsedSeconds = (int)Math.Min(durationSeconds, elapsed);
    }

    private static string ResolveVisualId(
        PackableBuildingExecutionSnapshot snapshot,
        PackableBuildingVisualProfile profile)
    {
        if (snapshot.Status == PackableBuildingExecutionStatus.Completed)
        {
            return snapshot.Operation == PackableBuildingOperationKind.Unpack
                ? profile.ActiveBuildingVisualId
                : profile.WorldBoxVisualId;
        }

        if (snapshot.Status == PackableBuildingExecutionStatus.Cancelled)
        {
            return snapshot.Operation == PackableBuildingOperationKind.Unpack
                ? profile.WorldBoxVisualId
                : profile.ActiveBuildingVisualId;
        }

        if (snapshot.Status == PackableBuildingExecutionStatus.Planned)
        {
            return snapshot.Operation == PackableBuildingOperationKind.Unpack
                ? profile.PlannedSiteVisualId
                : profile.ActiveBuildingVisualId;
        }

        return snapshot.Operation == PackableBuildingOperationKind.Unpack
            ? profile.PartialUnpackVisualId
            : profile.PartialPackVisualId;
    }

    private static string ResolveStatusLabel(
        PackableBuildingExecutionSnapshot snapshot)
    {
        string direction = snapshot.Operation == PackableBuildingOperationKind.Unpack
            ? "unpack"
            : "pack";
        string status = snapshot.Status switch
        {
            PackableBuildingExecutionStatus.Planned => "planned",
            PackableBuildingExecutionStatus.Active => "active",
            PackableBuildingExecutionStatus.Interrupted => "interrupted",
            PackableBuildingExecutionStatus.Completed => "completed",
            PackableBuildingExecutionStatus.Cancelled => "cancelled",
            _ => throw new ArgumentOutOfRangeException(nameof(snapshot)),
        };
        return $"building.packable.{direction}.{status}";
    }
}

}