using System;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Navigation;
using Dig.Domain.World;

namespace Dig.Unity
{

internal sealed partial class DigAgentSession
{
    private void RegisterManualMovement(EntityId agentId, TunnelPath path)
    {
        bool repeated = _manualTunnelMovements.TryGetValue(
            agentId,
            out ManualTunnelMovementOrder? previous)
            && previous.Destination == path.Cells[path.Cells.Count - 1];
        if (previous != null)
        {
            RecordMovementInterruption(
                agentId,
                repeated
                    ? ResidentMovementInterruptionReason.RepeatedCommand
                    : ResidentMovementInterruptionReason.ReplacedByCommand,
                repeated
                    ? "Manual movement command repeated."
                    : "Manual movement command replaced.");
        }

        ManualTunnelMovementOrder order =
            new ManualTunnelMovementOrder(path, repeated);
        if (order.IsComplete)
        {
            _manualTunnelMovements.Remove(agentId);
            RecordMovementInterruption(
                agentId,
                ResidentMovementInterruptionReason.Completed,
                "Manual movement destination already reached.");
            return;
        }

        _manualTunnelMovements[agentId] = order;
    }

    private void CancelManualMovementWithWarning(
        EntityId agentId,
        DomainError error,
        ResidentMovementInterruptionReason reason =
            ResidentMovementInterruptionReason.MovementRejected)
    {
        _manualTunnelMovements.Remove(agentId);
        _manualTunnelMovementWarning = error;
        RecordMovementInterruption(agentId, reason, error.ToString());
    }

    private sealed class ManualTunnelMovementOrder
    {
        private int _nextCellIndex;

        internal ManualTunnelMovementOrder(
            TunnelPath path,
            bool isRepeatedCommand)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            IsRepeatedCommand = isRepeatedCommand;
            _nextCellIndex = Math.Min(1, Path.Cells.Count);
        }

        internal TunnelPath Path { get; }

        internal CellId Destination => Path.Cells[Path.Cells.Count - 1];

        internal bool IsRepeatedCommand { get; }

        internal int RemainingPathSteps =>
            Math.Max(0, Path.Cells.Count - _nextCellIndex);

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
