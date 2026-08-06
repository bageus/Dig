using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Agents;
using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Application.Messaging;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;

namespace Dig.Application.Tunnels
{
public static class TunnelManualReinforcementErrors
{
    public static readonly DomainError UnsupportedMaterial = new DomainError(
        "tunnel.manual_reinforcement.unsupported_material",
        "Only a mushroom leg or stone can start tunnel reinforcement placement.");

    public static readonly DomainError SourceUnavailable = new DomainError(
        "tunnel.manual_reinforcement.source_unavailable",
        "The exact selected material is not available in the selected resident inventory.");

    public static readonly DomainError TargetUnavailable = new DomainError(
        "tunnel.manual_reinforcement.target_unavailable",
        "The hovered cell is not a valid tunnel reinforcement target.");

    public static readonly DomainError DuplicateTarget = new DomainError(
        "tunnel.manual_reinforcement.duplicate_target",
        "The hovered tunnel cell is already reinforced for this material.");

    public static readonly DomainError TargetReserved = new DomainError(
        "tunnel.manual_reinforcement.target_reserved",
        "Another resident already owns reinforcement work at this tunnel cell.");

    public static readonly DomainError JobMismatch = new DomainError(
        "tunnel.manual_reinforcement.job_mismatch",
        "The requested job is not manual tunnel reinforcement work.");

    public static readonly DomainError InvalidJobStage = new DomainError(
        "tunnel.manual_reinforcement.invalid_job_stage",
        "Manual tunnel reinforcement must be in its finalization stage.");
}

public sealed class TunnelManualReinforcementPlan
{
    public TunnelManualReinforcementPlan(
        EntityId segmentId,
        TunnelManualReinforcementKind kind,
        CellId targetCell)
    {
        SegmentId = segmentId;
        Kind = kind;
        TargetCell = targetCell;
    }

    public EntityId SegmentId { get; }
    public TunnelManualReinforcementKind Kind { get; }
    public CellId TargetCell { get; }
}

public static class TunnelManualReinforcementPlanner
{
    private static readonly ItemId MushroomLeg = new ItemId("material.mushroom_leg");
    private static readonly ItemId Stone = new ItemId("material.stone");

    public static Result<TunnelManualReinforcementPlan> Resolve(
        TunnelInfrastructureSnapshot tunnels,
        ItemId itemId,
        CellId targetCell)
    {
        if (tunnels is null)
        {
            throw new ArgumentNullException(nameof(tunnels));
        }

        if (itemId != MushroomLeg && itemId != Stone)
        {
            return Result<TunnelManualReinforcementPlan>.Failure(
                TunnelManualReinforcementErrors.UnsupportedMaterial);
        }

        if (itemId == Stone)
        {
            TunnelJunctionStoneTrimTargetSnapshot? junction = tunnels
                .PendingJunctionStoneTrimTargets
                .Where(value => value.Cell == targetCell)
                .Select(value => (TunnelJunctionStoneTrimTargetSnapshot?)value)
                .FirstOrDefault();
            if (junction.HasValue)
            {
                return Result<TunnelManualReinforcementPlan>.Success(
                    new TunnelManualReinforcementPlan(
                        junction.Value.OwnerSegmentId,
                        TunnelManualReinforcementKind.JunctionStoneTrim,
                        targetCell));
            }

            if (tunnels.CompletedJunctionStoneTrimCells.Contains(targetCell)
                || tunnels.CompletedStoneFloorTrimCells.Contains(targetCell))
            {
                return Result<TunnelManualReinforcementPlan>.Failure(
                    TunnelManualReinforcementErrors.DuplicateTarget);
            }
        }

        HorizontalTunnelSegmentSnapshot[] segments = tunnels.Segments
            .Where(segment => segment.OrderedHorizontalCells.Contains(targetCell))
            .OrderBy(segment => segment.SegmentId.ToString(), StringComparer.Ordinal)
            .ToArray();
        if (segments.Length == 0)
        {
            return Result<TunnelManualReinforcementPlan>.Failure(
                TunnelManualReinforcementErrors.TargetUnavailable);
        }

        HorizontalTunnelSegmentSnapshot segment = segments[0];
        if (itemId == MushroomLeg)
        {
            bool duplicate = segment.StructuralAnchors.Any(anchor =>
                anchor.Cell == targetCell);
            return duplicate
                ? Result<TunnelManualReinforcementPlan>.Failure(
                    TunnelManualReinforcementErrors.DuplicateTarget)
                : Result<TunnelManualReinforcementPlan>.Success(
                    new TunnelManualReinforcementPlan(
                        segment.SegmentId,
                        TunnelManualReinforcementKind.WoodenSupport,
                        targetCell));
        }

        return Result<TunnelManualReinforcementPlan>.Success(
            new TunnelManualReinforcementPlan(
                segment.SegmentId,
                TunnelManualReinforcementKind.StoneFloorTrim,
                targetCell));
    }
}

public sealed class CreateTunnelManualReinforcementCommand : ICommand<Result>
{
    public CreateTunnelManualReinforcementCommand(
        EntityId jobId,
        EntityId residentId,
        EntityId sourceStackId,
        TunnelManualReinforcementPlan plan,
        long tick)
    {
        JobId = jobId;
        ResidentId = residentId;
        SourceStackId = sourceStackId;
        Plan = plan ?? throw new ArgumentNullException(nameof(plan));
        Tick = tick;
    }

    public EntityId JobId { get; }
    public EntityId ResidentId { get; }
    public EntityId SourceStackId { get; }
    public TunnelManualReinforcementPlan Plan { get; }
    public long Tick { get; }
}

public sealed class CompleteTunnelManualReinforcementCommand : ICommand<Result>
{
    public CompleteTunnelManualReinforcementCommand(EntityId jobId, long tick)
    {
        JobId = jobId;
        Tick = tick;
    }

    public EntityId JobId { get; }
    public long Tick { get; }
}

public sealed class CancelTunnelManualReinforcementCommand : ICommand<Result>
{
    public CancelTunnelManualReinforcementCommand(
        EntityId jobId,
        string reasonCode,
        long tick)
    {
        JobId = jobId;
        ReasonCode = reasonCode;
        Tick = tick;
    }

    public EntityId JobId { get; }
    public string ReasonCode { get; }
    public long Tick { get; }
}

}
