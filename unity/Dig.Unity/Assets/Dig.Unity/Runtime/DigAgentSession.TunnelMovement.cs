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
        private TunnelNavigationVolume? _tunnelVolume;
        private PlanAgentTunnelRouteCommandHandler? _tunnelRoutePlanner;
        private PlanAgentsTunnelRoutesCommandHandler? _groupTunnelRoutePlanner;
        private InMemoryExecutionJournal? _tunnelJournal;
        private DomainError? _manualTunnelMovementWarning;

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
                new PlanAgentTunnelRouteCommand(id, destination));
            if (report.Result.IsSuccess && report.Path != null)
            {
                RegisterManualMovement(id, report.Path);
            }

            return report;
        }

        internal PlanAgentsTunnelRoutesReport MoveResidentsThroughTunnel(
            IReadOnlyCollection<string> residentIds,
            CellId destination)
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
                new PlanAgentsTunnelRoutesCommand(ids, destination));
            if (report.Result.IsSuccess)
            {
                for (int index = 0; index < report.Entries.Count; index++)
                {
                    PlannedAgentTunnelRoute entry = report.Entries[index];
                    RegisterManualMovement(entry.AgentId, entry.Path);
                }
            }

            return report;
        }

        internal bool HasManualTunnelMovement(string residentId)
        {
            return !string.IsNullOrWhiteSpace(residentId)
                && _manualTunnelMovements.ContainsKey(EntityId.Parse(residentId));
        }

        internal bool CancelManualTunnelMovement(string residentId)
        {
            if (string.IsNullOrWhiteSpace(residentId))
            {
                throw new ArgumentException("Resident id is required.", nameof(residentId));
            }

            return _manualTunnelMovements.Remove(EntityId.Parse(residentId));
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
                CancelManualMovementWithWarning(agent.Id, AgentErrors.AgentDead);
                result = Result.Success();
                return true;
            }

            if (order.IsComplete)
            {
                _manualTunnelMovements.Remove(agent.Id);
                result = Result.Success();
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
                    _manualTunnelMovements.Remove(agent.Id);
                    result = Result.Success();
                    return true;
                }
            }

            CellId next = order.NextCell;
            Result moved = agent.MoveTo(next, _tick);
            if (moved.IsFailure)
            {
                CancelManualMovementWithWarning(agent.Id, moved.Error!);
                result = Result.Success();
                return true;
            }

            _repository.Save(agent);
            _tunnelJournal!.Append(agent.DequeueUncommittedEvents());
            order.ConfirmStep(next);
            if (order.IsComplete)
            {
                _manualTunnelMovements.Remove(agent.Id);
            }

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
                CancelManualMovementWithWarning(agent.Id, error);
                order = null!;
                return false;
            }

            order = new ManualTunnelMovementOrder(path.Path);
            _manualTunnelMovements[agent.Id] = order;
            return true;
        }

        private void RegisterManualMovement(EntityId agentId, TunnelPath path)
        {
            ManualTunnelMovementOrder order = new ManualTunnelMovementOrder(path);
            if (order.IsComplete)
            {
                _manualTunnelMovements.Remove(agentId);
                return;
            }

            _manualTunnelMovements[agentId] = order;
        }

        private void CancelManualMovementWithWarning(EntityId agentId, DomainError error)
        {
            _manualTunnelMovements.Remove(agentId);
            _manualTunnelMovementWarning = error;
        }

        private sealed class ManualTunnelMovementOrder
        {
            private int _nextCellIndex;

            internal ManualTunnelMovementOrder(TunnelPath path)
            {
                Path = path ?? throw new ArgumentNullException(nameof(path));
                _nextCellIndex = Math.Min(1, Path.Cells.Count);
            }

            internal TunnelPath Path { get; }

            internal CellId Destination => Path.Cells[Path.Cells.Count - 1];

            internal bool IsComplete => _nextCellIndex >= Path.Cells.Count;

            internal CellId ExpectedCurrent =>
                Path.Cells[Math.Max(0, _nextCellIndex - 1)];

            internal CellId NextCell => Path.Cells[_nextCellIndex];

            internal void ConfirmStep(CellId arrived)
            {
                if (IsComplete || arrived != NextCell)
                {
                    throw new InvalidOperationException(
                        "Manual tunnel movement can confirm only its next authoritative cell.");
                }

                _nextCellIndex++;
            }
        }
    }
}
