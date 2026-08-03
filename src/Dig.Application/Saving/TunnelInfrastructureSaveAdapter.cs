using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Tunnels;
using System.Globalization;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.World;

namespace Dig.Application.Saving
{

public static class TunnelInfrastructureSaveErrors
{
    public static readonly DomainError InvalidSnapshot = new DomainError(
        "save.tunnel_infrastructure.invalid",
        "Tunnel infrastructure save data is malformed or inconsistent.");
}

public static class TunnelInfrastructureSaveAdapter
{
    public static TunnelInfrastructureSaveData Encode(
        TunnelInfrastructureRuntimeSnapshot runtime,
        JobSystem jobs)
    {
        if (runtime == null)
        {
            throw new ArgumentNullException(nameof(runtime));
        }

        if (jobs == null)
        {
            throw new ArgumentNullException(nameof(jobs));
        }

        Result<TunnelInfrastructureState> validated =
            TunnelInfrastructureState.Restore(runtime.Infrastructure);
        if (validated.IsFailure
            || !IsNextSequenceValid(runtime.NextAutomaticJobSequence, jobs))
        {
            throw new InvalidOperationException(
                TunnelInfrastructureSaveErrors.InvalidSnapshot.ToString());
        }

        TunnelInfrastructureSnapshot snapshot =
            validated.Value.CaptureSnapshot();
        TunnelInfrastructureSaveData data = new TunnelInfrastructureSaveData
        {
            Version = snapshot.Version,
            NextAutomaticJobSequence = runtime.NextAutomaticJobSequence,
        };
        foreach (HorizontalTunnelSegmentSnapshot segment in snapshot.Segments)
        {
            data.Segments.Add(EncodeSegment(segment));
        }

        foreach (CellId cell in snapshot.CompletedJunctionStoneTrimCells)
        {
            data.CompletedJunctionStoneTrimCells.Add(EncodeCell(cell));
        }

        foreach (TunnelJunctionStoneTrimTargetSnapshot target in
            snapshot.PendingJunctionStoneTrimTargets)
        {
            data.PendingJunctionStoneTrimTargets.Add(
                new TunnelJunctionStoneTrimTargetSaveData
                {
                    OwnerSegmentId = target.OwnerSegmentId.ToString(),
                    X = target.Cell.X,
                    Y = target.Cell.Y,
                    Z = target.Cell.Z,
                });
        }

        return data;
    }

    public static Result<TunnelInfrastructureRuntimeSnapshot> Decode(
        TunnelInfrastructureSaveData? data,
        JobSystem jobs)
    {
        if (jobs == null)
        {
            throw new ArgumentNullException(nameof(jobs));
        }

        data ??= new TunnelInfrastructureSaveData();
        try
        {
            if (data.Segments == null
                || data.CompletedJunctionStoneTrimCells == null
                || data.PendingJunctionStoneTrimTargets == null
                || data.Segments.Any(value => value == null)
                || data.CompletedJunctionStoneTrimCells.Any(value => value == null)
                || data.PendingJunctionStoneTrimTargets.Any(value => value == null)
                || !IsNextSequenceValid(data.NextAutomaticJobSequence, jobs))
            {
                return Failure();
            }

            HorizontalTunnelSegmentSnapshot[] segments = data.Segments
                .OrderBy(value => value.SegmentId, StringComparer.Ordinal)
                .Select(DecodeSegment)
                .ToArray();
            CellId[] completedTrim = data.CompletedJunctionStoneTrimCells
                .Select(DecodeCell)
                .ToArray();
            TunnelInfrastructureSnapshot savedSnapshot =
                new TunnelInfrastructureSnapshot(
                    data.Version,
                    segments,
                    completedTrim);
            Result<TunnelInfrastructureState> restored =
                TunnelInfrastructureState.Restore(savedSnapshot);
            if (restored.IsFailure)
            {
                return Failure();
            }

            TunnelInfrastructureSnapshot derived =
                restored.Value.CaptureSnapshot();
            TunnelJunctionStoneTrimTargetSnapshot[] savedPending =
                data.PendingJunctionStoneTrimTargets
                    .Select(DecodePendingTarget)
                    .OrderBy(value => value.Cell)
                    .ThenBy(
                        value => value.OwnerSegmentId.ToString(),
                        StringComparer.Ordinal)
                    .ToArray();
            if (!derived.PendingJunctionStoneTrimTargets.SequenceEqual(savedPending))
            {
                return Failure();
            }

            return Result<TunnelInfrastructureRuntimeSnapshot>.Success(
                new TunnelInfrastructureRuntimeSnapshot(
                    derived,
                    data.NextAutomaticJobSequence));
        }
        catch (Exception exception) when (
            exception is ArgumentException
            || exception is InvalidOperationException
            || exception is FormatException
            || exception is OverflowException)
        {
            return Failure();
        }
    }

    public static ulong ResolveLegacyNextSequence(JobsSaveData? jobs)
    {
        ulong next = 1UL;
        IEnumerable<JobSaveData> savedJobs = jobs == null
            ? Array.Empty<JobSaveData>()
            : jobs.Jobs;
        foreach (JobSaveData job in savedJobs)
        {
            JobDefinitionSaveData? definition = job?.Definition;
            if (definition == null
                || !string.Equals(
                    definition.TypeId,
                    new TunnelAutomaticWorkJobSaveCodec().TypeId,
                    StringComparison.Ordinal)
                || !TryParseAutomaticJobSequence(
                    definition.JobId,
                    out ulong sequence))
            {
                continue;
            }

            next = Math.Max(next, checked(sequence + 1UL));
        }

        return next;
    }

    private static TunnelSegmentSaveData EncodeSegment(
        HorizontalTunnelSegmentSnapshot segment)
    {
        TunnelSegmentSaveData data = new TunnelSegmentSaveData
        {
            SegmentId = segment.SegmentId.ToString(),
            OriginKind = (int)segment.OriginKind,
            OriginX = segment.OriginCell.X,
            OriginY = segment.OriginCell.Y,
            OriginZ = segment.OriginCell.Z,
            Version = segment.Version,
            NextAutomaticSupportTarget = EncodeTarget(
                segment.NextAutomaticSupportTarget),
        };
        foreach (CellId cell in segment.OrderedHorizontalCells)
        {
            data.OrderedHorizontalCells.Add(EncodeCell(cell));
        }

        foreach (TunnelStructuralAnchorSnapshot anchor in segment.StructuralAnchors)
        {
            data.StructuralAnchors.Add(new TunnelStructuralAnchorSaveData
            {
                X = anchor.Cell.X,
                Y = anchor.Cell.Y,
                Z = anchor.Cell.Z,
                Kind = (int)anchor.Kind,
                DistanceFromOrigin = anchor.DistanceFromOrigin,
            });
        }

        return data;
    }

    private static HorizontalTunnelSegmentSnapshot DecodeSegment(
        TunnelSegmentSaveData data)
    {
        if (data == null
            || data.OrderedHorizontalCells == null
            || data.StructuralAnchors == null
            || !Enum.IsDefined(typeof(TunnelSegmentOriginKind), data.OriginKind))
        {
            throw new InvalidOperationException("Invalid tunnel segment save data.");
        }

        return new HorizontalTunnelSegmentSnapshot(
            EntityId.Parse(data.SegmentId),
            (TunnelSegmentOriginKind)data.OriginKind,
            new CellId(data.OriginX, data.OriginY, data.OriginZ),
            data.OrderedHorizontalCells.Select(DecodeCell),
            data.StructuralAnchors.Select(DecodeAnchor),
            DecodeTarget(data.NextAutomaticSupportTarget),
            data.Version);
    }

    private static TunnelStructuralAnchorSnapshot DecodeAnchor(
        TunnelStructuralAnchorSaveData data)
    {
        if (data == null
            || !Enum.IsDefined(typeof(TunnelStructuralAnchorKind), data.Kind))
        {
            throw new InvalidOperationException("Invalid tunnel anchor save data.");
        }

        return new TunnelStructuralAnchorSnapshot(
            new CellId(data.X, data.Y, data.Z),
            (TunnelStructuralAnchorKind)data.Kind,
            data.DistanceFromOrigin);
    }

    private static TunnelAutomaticSupportTargetSaveData? EncodeTarget(
        TunnelAutomaticSupportTargetSnapshot? target)
    {
        if (!target.HasValue)
        {
            return null;
        }

        TunnelAutomaticSupportTargetSnapshot value = target.Value;
        return new TunnelAutomaticSupportTargetSaveData
        {
            SegmentId = value.SegmentId.ToString(),
            AnchorX = value.AnchorCell.X,
            AnchorY = value.AnchorCell.Y,
            AnchorZ = value.AnchorCell.Z,
            TargetX = value.TargetCell.X,
            TargetY = value.TargetCell.Y,
            TargetZ = value.TargetCell.Z,
            DistanceFromAnchor = value.DistanceFromAnchor,
        };
    }

    private static TunnelAutomaticSupportTargetSnapshot? DecodeTarget(
        TunnelAutomaticSupportTargetSaveData? data)
    {
        return data == null
            ? (TunnelAutomaticSupportTargetSnapshot?)null
            : new TunnelAutomaticSupportTargetSnapshot(
                EntityId.Parse(data.SegmentId),
                new CellId(data.AnchorX, data.AnchorY, data.AnchorZ),
                new CellId(data.TargetX, data.TargetY, data.TargetZ),
                data.DistanceFromAnchor);
    }

    private static TunnelJunctionStoneTrimTargetSnapshot DecodePendingTarget(
        TunnelJunctionStoneTrimTargetSaveData data)
    {
        if (data == null)
        {
            throw new InvalidOperationException("Invalid junction target save data.");
        }

        return new TunnelJunctionStoneTrimTargetSnapshot(
            EntityId.Parse(data.OwnerSegmentId),
            new CellId(data.X, data.Y, data.Z));
    }

    private static TunnelCellSaveData EncodeCell(CellId cell)
    {
        return new TunnelCellSaveData { X = cell.X, Y = cell.Y, Z = cell.Z };
    }

    private static CellId DecodeCell(TunnelCellSaveData data)
    {
        if (data == null)
        {
            throw new InvalidOperationException("Invalid tunnel cell save data.");
        }

        return new CellId(data.X, data.Y, data.Z);
    }

    private static bool IsNextSequenceValid(ulong next, JobSystem jobs)
    {
        if (next == 0)
        {
            return false;
        }

        return !jobs.GetAll().Any(job =>
            job.Definition is TunnelAutomaticWorkJobDefinition
            && TryParseAutomaticJobSequence(job.Id.ToString(), out ulong sequence)
            && sequence >= next);
    }

    private static bool TryParseAutomaticJobSequence(
        string? value,
        out ulong sequence)
    {
        sequence = 0;
        return value != null
            && value.Length == 32
            && value[0] == 'a'
            && ulong.TryParse(
                value.Substring(1),
                NumberStyles.HexNumber,
                CultureInfo.InvariantCulture,
                out sequence)
            && sequence > 0;
    }

    private static Result<TunnelInfrastructureRuntimeSnapshot> Failure()
    {
        return Result<TunnelInfrastructureRuntimeSnapshot>.Failure(
            TunnelInfrastructureSaveErrors.InvalidSnapshot);
    }
}

}
