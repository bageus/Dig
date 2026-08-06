using System;
using System.Linq;
using Dig.Application.Messaging;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.World;

namespace Dig.Application.Tunnels
{

public static class TunnelManualPlacementErrors
{
    public static readonly DomainError UnsupportedMaterial = new DomainError(
        "tunnel.manual.unsupported_material",
        "Manual tunnel placement requires a mushroom leg or stone.");
    public static readonly DomainError SourceUnavailable = new DomainError(
        "tunnel.manual.source_unavailable",
        "The exact resident inventory stack is unavailable.");
    public static readonly DomainError TargetUnavailable = new DomainError(
        "tunnel.manual.target_unavailable",
        "The selected cell is not a legal target for this material.");
    public static readonly DomainError AmbiguousTarget = new DomainError(
        "tunnel.manual.ambiguous_target",
        "The selected cell belongs to more than one horizontal segment.");
    public static readonly DomainError AlreadyCompleted = new DomainError(
        "tunnel.manual.already_completed",
        "The selected infrastructure element is already completed.");
    public static readonly DomainError JobMismatch = new DomainError(
        "tunnel.manual.job_mismatch",
        "The requested job is not owner-locked manual tunnel work.");
    public static readonly DomainError OwnerMismatch = new DomainError(
        "tunnel.manual.owner_mismatch",
        "Only the resident who owns the exact source stack may perform this job.");
    public static readonly DomainError InvalidJobStage = new DomainError(
        "tunnel.manual.invalid_job_stage",
        "Manual tunnel work is not in its finalization stage.");
}

public sealed class TunnelManualPlacementPlan
{
    public TunnelManualPlacementPlan(
        EntityId residentId,
        EntityId sourceStackId,
        EntityId segmentId,
        TunnelManualWorkKind kind,
        CellId targetCell)
    {
        ResidentId = residentId;
        SourceStackId = sourceStackId;
        SegmentId = segmentId;
        Kind = kind;
        TargetCell = targetCell;
    }

    public EntityId ResidentId { get; }
    public EntityId SourceStackId { get; }
    public EntityId SegmentId { get; }
    public TunnelManualWorkKind Kind { get; }
    public CellId TargetCell { get; }
}

public sealed class ValidateTunnelManualPlacementQuery
    : IQuery<Result<TunnelManualPlacementPlan>>
{
    public ValidateTunnelManualPlacementQuery(
        EntityId residentId,
        EntityId sourceStackId,
        CellId targetCell)
    {
        ResidentId = residentId;
        SourceStackId = sourceStackId;
        TargetCell = targetCell;
    }

    public EntityId ResidentId { get; }
    public EntityId SourceStackId { get; }
    public CellId TargetCell { get; }
}

public sealed class CreateTunnelManualWorkCommand : ICommand<Result<EntityId>>
{
    public CreateTunnelManualWorkCommand(
        EntityId jobId,
        EntityId residentId,
        EntityId sourceStackId,
        CellId targetCell,
        long tick)
    {
        JobId = jobId;
        ResidentId = residentId;
        SourceStackId = sourceStackId;
        TargetCell = targetCell;
        Tick = tick;
    }

    public EntityId JobId { get; }
    public EntityId ResidentId { get; }
    public EntityId SourceStackId { get; }
    public CellId TargetCell { get; }
    public long Tick { get; }
}

public sealed class CancelTunnelManualWorkCommand : ICommand<Result>
{
    public CancelTunnelManualWorkCommand(EntityId jobId, long tick)
    {
        JobId = jobId;
        Tick = tick;
    }

    public EntityId JobId { get; }
    public long Tick { get; }
}

public sealed class CompleteTunnelManualWorkCommand : ICommand<Result>
{
    public CompleteTunnelManualWorkCommand(EntityId jobId, long tick)
    {
        JobId = jobId;
        Tick = tick;
    }

    public EntityId JobId { get; }
    public long Tick { get; }
}

internal static class TunnelManualTargetResolver
{
    public static Result<TunnelManualPlacementPlan> Resolve(
        TunnelInfrastructureSnapshot snapshot,
        EntityId residentId,
        EntityId sourceStackId,
        string itemId,
        CellId targetCell)
    {
        bool leg = string.Equals(
            itemId, "material.mushroom_leg", StringComparison.Ordinal);
        bool stone = string.Equals(itemId, "material.stone", StringComparison.Ordinal);
        if (!leg && !stone)
        {
            return Result<TunnelManualPlacementPlan>.Failure(
                TunnelManualPlacementErrors.UnsupportedMaterial);
        }

        if (stone)
        {
            HorizontalTunnelSegmentSnapshot? junctionOwner = snapshot.Segments
                .Where(segment => segment.OriginKind == TunnelSegmentOriginKind.VerticalJunction
                    && segment.OriginCell == targetCell)
                .OrderBy(segment => segment.SegmentId.ToString(), StringComparer.Ordinal)
                .FirstOrDefault();
            if (junctionOwner != null)
            {
                if (snapshot.CompletedJunctionStoneTrimCells.Contains(targetCell))
                {
                    return Result<TunnelManualPlacementPlan>.Failure(
                        TunnelManualPlacementErrors.AlreadyCompleted);
                }

                return Success(
                    residentId,
                    sourceStackId,
                    junctionOwner.SegmentId,
                    TunnelManualWorkKind.JunctionStoneTrim,
                    targetCell);
            }
        }

        HorizontalTunnelSegmentSnapshot[] owners = snapshot.Segments
            .Where(segment => segment.OrderedHorizontalCells.Contains(targetCell))
            .ToArray();
        if (owners.Length == 0)
        {
            return Result<TunnelManualPlacementPlan>.Failure(
                TunnelManualPlacementErrors.TargetUnavailable);
        }

        if (owners.Length > 1)
        {
            return Result<TunnelManualPlacementPlan>.Failure(
                TunnelManualPlacementErrors.AmbiguousTarget);
        }

        HorizontalTunnelSegmentSnapshot owner = owners[0];
        if (leg && owner.StructuralAnchors.Any(anchor =>
            anchor.Cell == targetCell
            && anchor.Kind == TunnelStructuralAnchorKind.WoodenSupport))
        {
            return Result<TunnelManualPlacementPlan>.Failure(
                TunnelManualPlacementErrors.AlreadyCompleted);
        }

        if (stone && snapshot.CompletedStoneFloorTrimCells.Contains(targetCell))
        {
            return Result<TunnelManualPlacementPlan>.Failure(
                TunnelManualPlacementErrors.AlreadyCompleted);
        }

        return Success(
            residentId,
            sourceStackId,
            owner.SegmentId,
            leg ? TunnelManualWorkKind.WoodenSupport : TunnelManualWorkKind.StoneFloorTrim,
            targetCell);
    }

    private static Result<TunnelManualPlacementPlan> Success(
        EntityId residentId,
        EntityId sourceStackId,
        EntityId segmentId,
        TunnelManualWorkKind kind,
        CellId targetCell)
    {
        return Result<TunnelManualPlacementPlan>.Success(
            new TunnelManualPlacementPlan(
                residentId,
                sourceStackId,
                segmentId,
                kind,
                targetCell));
    }
}

}
