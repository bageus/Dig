using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Agents;
using Dig.Application.World;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;

namespace Dig.Unity
{
    internal sealed partial class DigAgentSession
    {
        private readonly Dictionary<EntityId, ManualTunnelMovementOrder> _manualTunnelMovements =
            new Dictionary<EntityId, ManualTunnelMovementOrder>();
        private readonly TunnelTrafficCoordinator _tunnelTraffic =
            new TunnelTrafficCoordinator();
        private TunnelNavigationVolume? _tunnelVolume;
        private PlanAgentTunnelRouteCommandHandler? _tunnelRoutePlanner;
        private PlanAgentsTunnelRoutesCommandHandler? _groupTunnelRoutePlanner;
        private InMemoryExecutionJournal? _tunnelJournal;
        private DomainError? _manualTunnelMovementWarning;

        private Result MoveThroughTunnelTraffic(AgentState agent, CellId destination)
        {
            return MoveThroughAutomaticSurfaceCorridor(agent, destination);
        }

        internal TunnelNavigationVolume TunnelVolume => _tunnelVolume
            ?? throw new InvalidOperationException("Tunnel movement is not initialized.");

        internal IReadOnlyList<string> ActiveManualTunnelResidentIds =>
            _manualTunnelMovements.Keys
                .OrderBy(id => id.ToString(), StringComparer.Ordinal)
                .Select(id => id.ToString())
                .ToArray();

        internal PlanAgentTunnelRouteReport MoveResidentThroughTunnel(
            string residentId,
            CellId destination)
        {
            return MoveResidentThroughTunnel(
                residentId,
                SurfacePose.FloorCentre(destination));
        }

        internal PlanAgentTunnelRouteReport MoveResidentThroughTunnel(
            string residentId,
            SurfacePose destination)
        {
            if (string.IsNullOrWhiteSpace(residentId))
            {
                throw new ArgumentException("Resident id is required.", nameof(residentId));
            }

            if (_tunnelRoutePlanner == null)
            {
                throw new InvalidOperationException("Tunnel movement is not initialized.");
            }

            EntityId id = EntityId.Parse(residentId);
            PlanAgentTunnelRouteReport report = _tunnelRoutePlanner.Handle(
                new PlanAgentTunnelRouteCommand(id, destination.Cell));
            if (report.Result.IsSuccess && report.Path != null)
            {
                RegisterManualMovement(id, report.Path, destination);
            }

            return report;
        }

        internal PlanAgentsTunnelRoutesReport MoveResidentsThroughTunnel(
            IReadOnlyCollection<string> residentIds,
            CellId destination)
        {
            return MoveResidentsThroughTunnel(
                residentIds,
                SurfacePose.FloorCentre(destination));
        }

        internal PlanAgentsTunnelRoutesReport MoveResidentsThroughTunnel(
            IReadOnlyCollection<string> residentIds,
            SurfacePose destination)
        {
            if (residentIds == null)
            {
                throw new ArgumentNullException(nameof(residentIds));
            }

            if (_groupTunnelRoutePlanner == null)
            {
                throw new InvalidOperationException("Tunnel movement is not initialized.");
            }

            List<EntityId> ids = new List<EntityId>(residentIds.Count);
            foreach (string residentId in residentIds)
            {
                if (string.IsNullOrWhiteSpace(residentId))
                {
                    throw new ArgumentException(
                        "Resident ids cannot contain an empty value.",
                        nameof(residentIds));
                }

                ids.Add(EntityId.Parse(residentId));
            }

            PlanAgentsTunnelRoutesReport report = _groupTunnelRoutePlanner.Handle(
                new PlanAgentsTunnelRoutesCommand(ids, destination.Cell));
            if (report.Result.IsSuccess)
            {
                for (int index = 0; index < report.Entries.Count; index++)
                {
                    PlannedAgentTunnelRoute entry = report.Entries[index];
                    SurfacePose assignedPose = new SurfacePose(
                        entry.Path.Cells[entry.Path.Cells.Count - 1],
                        destination.Face,
                        destination.U,
                        destination.V);
                    RegisterManualMovement(entry.AgentId, entry.Path, assignedPose);
                }
            }

            return report;
        }

        internal bool HasManualTunnelMovement(string residentId)
        {
            return !string.IsNullOrWhiteSpace(residentId)
                && _manualTunnelMovements.ContainsKey(EntityId.Parse(residentId));
        }

        internal bool CancelManualTunnelMovement(
            string residentId,
            ResidentMovementInterruptionReason reason =
                ResidentMovementInterruptionReason.HigherPriorityAction)
        {
            if (string.IsNullOrWhiteSpace(residentId))
            {
                throw new ArgumentException("Resident id is required.", nameof(residentId));
            }

            EntityId id = EntityId.Parse(residentId);
            bool removed = _manualTunnelMovements.Remove(id);
            if (removed)
            {
                RecordMovementInterruption(id, reason, reason.ToString());
            }

            return removed;
        }

        internal DomainError? ConsumeManualTunnelMovementWarning()
        {
            DomainError? warning = _manualTunnelMovementWarning;
            _manualTunnelMovementWarning = null;
            return warning;
        }

        private bool TryAdvanceManualTunnelMovement(
            AgentState agent,
            out Result result)
        {
            if (!_manualTunnelMovements.TryGetValue(
                agent.Id,
                out ManualTunnelMovementOrder? order))
            {
                result = Result.Success();
                return false;
            }

            if (!agent.IsAlive)
            {
                CancelManualMovementWithWarning(
                    agent.Id,
                    AgentErrors.AgentDead,
                    ResidentMovementInterruptionReason.AgentDead);
                result = Result.Success();
                return true;
            }

            if (order.IsComplete)
            {
                result = CompleteManualMovement(agent, order);
                return true;
            }

            if (agent.Position != order.ExpectedCurrent
                || !TunnelVolume.CanTraverseStep(
                    agent.Position,
                    order.NextCell))
            {
                if (!TryReplanManualMovement(agent, order.Destination, out order))
                {
                    result = Result.Success();
                    return true;
                }

                if (order.IsComplete)
                {
                    result = CompleteManualMovement(agent, order);
                    return true;
                }
            }

            CellId current = agent.Position;
            CellId next = order.NextCell;
            if (!IsMovementStepDue(
                agent,
                next,
                ResidentMovementCommandSource.Manual,
                order.IsRepeatedCommand,
                order.RemainingPathSteps))
            {
                result = Result.Success();
                return true;
            }

            if (TryAdvanceManualSurfaceStep(agent, order, next, out result))
            {
                return true;
            }

            if (!_tunnelTraffic.CanMove(agent.Id, current, next, _tick))
            {
                result = Result.Success();
                return true;
            }
            SurfacePose nextPose = SurfacePose.FloorCentre(next);
            if (!_surfaceTraffic.CanOccupy(agent.Id, nextPose, _tick))
            {
                result = Result.Success();
                return true;
            }

            Result moved = agent.MoveTo(next, _tick);
            if (moved.IsFailure)
            {
                CancelManualMovementWithWarning(
                    agent.Id,
                    moved.Error!,
                    ResidentMovementInterruptionReason.MovementRejected);
                result = Result.Success();
                return true;
            }

            _tunnelTraffic.RecordMove(agent.Id, current, next, _tick);
            RecordCellTrafficPose(agent);
            order.ConfirmStep(next);
            if (order.IsComplete)
            {
                result = CompleteManualMovement(agent, order);
                return true;
            }

            _repository.Save(agent);
            _tunnelJournal!.Append(agent.DequeueUncommittedEvents());
            result = Result.Success();
            return true;
        }

        private bool TryReplanManualMovement(
            AgentState agent,
            CellId destination,
            out ManualTunnelMovementOrder order)
        {
            TunnelPathResult path = TunnelVolume.FindPath(
                agent.Position,
                destination);
            if (!path.Succeeded || path.Path == null)
            {
                DomainError error = new DomainError(
                    $"agents.tunnel.{path.FailureReason.ToString().ToLowerInvariant()}",
                    path.Detail);
                CancelManualMovementWithWarning(
                    agent.Id,
                    error,
                    ResidentMovementInterruptionReason.RouteUnavailable);
                order = null!;
                return false;
            }

            bool repeated = _manualTunnelMovements.TryGetValue(
                agent.Id,
                out ManualTunnelMovementOrder? previous)
                && previous.IsRepeatedCommand;
            order = new ManualTunnelMovementOrder(
                path.Path,
                previous?.TargetPose ?? SurfacePose.FloorCentre(destination),
                repeated);
            _manualTunnelMovements[agent.Id] = order;
            return true;
        }

        private Result CompleteManualMovement(
            AgentState agent,
            ManualTunnelMovementOrder order)
        {
            if (agent.SurfacePose.IsVertical
                && VerticalSurfaceSteering.TryDetachToFloor(
                    agent.SurfacePose,
                    out SurfacePose floorPose))
            {
                if (!_surfaceTraffic.CanOccupy(agent.Id, floorPose, _tick))
                {
                    return Result.Success();
                }
                Result detached = MoveOnReservedSurface(agent, floorPose);
                if (detached.IsSuccess)
                {
                    SaveManualMovementProgress(agent);
                }
                return detached;
            }

            SurfacePose nextPose = SurfacePoseSteering.MoveTowards(
                agent.SurfacePose, order.TargetPose);
            if (!_surfaceTraffic.CanOccupy(agent.Id, nextPose, _tick))
            {
                return Result.Success();
            }
            Result positioned = MoveOnReservedSurface(agent, nextPose);
            if (positioned.IsFailure)
            {
                CancelManualMovementWithWarning(
                    agent.Id,
                    positioned.Error!,
                    ResidentMovementInterruptionReason.MovementRejected);
                return Result.Success();
            }

            _repository.Save(agent);
            _tunnelJournal!.Append(agent.DequeueUncommittedEvents());
            if (nextPose != order.TargetPose)
            {
                return Result.Success();
            }
            _manualTunnelMovements.Remove(agent.Id);
            RecordMovementInterruption(
                agent.Id,
                ResidentMovementInterruptionReason.Completed,
                "Manual movement completed at the selected surface point.");
            return RecordResidentTaskCompletion(
                agent, "manual_movement_completed", _tick);
        }
    }
}
