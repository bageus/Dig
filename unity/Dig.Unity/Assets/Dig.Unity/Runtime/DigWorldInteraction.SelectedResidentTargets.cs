using Dig.Domain.World;
using UnityEngine;

namespace Dig.Unity
{
    internal enum DigSelectedResidentTargetKind
    {
        None = 0,
        Excavation = 1,
        Movement = 2,
    }

    internal readonly struct DigSelectedResidentTarget
    {
        private DigSelectedResidentTarget(
            DigSelectedResidentTargetKind kind,
            CellId excavationCell,
            SpatialCellId movementCell,
            DigTunnelCellVisual? visual)
        {
            Kind = kind;
            ExcavationCell = excavationCell;
            MovementCell = movementCell;
            Visual = visual;
        }

        internal DigSelectedResidentTargetKind Kind { get; }
        internal CellId ExcavationCell { get; }
        internal SpatialCellId MovementCell { get; }
        internal DigTunnelCellVisual? Visual { get; }

        internal static DigSelectedResidentTarget Excavation(CellId cell)
        {
            return new DigSelectedResidentTarget(
                DigSelectedResidentTargetKind.Excavation,
                cell,
                default,
                null);
        }

        internal static DigSelectedResidentTarget Movement(
            SpatialCellId cell,
            DigTunnelCellVisual? visual)
        {
            return new DigSelectedResidentTarget(
                DigSelectedResidentTargetKind.Movement,
                default,
                cell,
                visual);
        }
    }

    public sealed partial class DigWorldInteraction
    {
        private DigSelectedResidentTarget ResolveSelectedResidentTarget()
        {
            RaycastHit[] hits = GetPointerHits();
            for (int index = 0; index < hits.Length; index++)
            {
                RaycastHit hit = hits[index];
                if (_agentRenderer!.TryGetAgent(hit, out _)
                    || _buildingRenderer!.TryGetBuilding(hit, out _)
                    || _itemRenderer!.TryGetItem(hit, out _))
                {
                    return default;
                }

                CellId? excavation = ResolveExcavationTarget(hit);
                if (excavation.HasValue)
                {
                    return DigSelectedResidentTarget.Excavation(excavation.Value);
                }

                if (TryResolveTunnelDestination(
                    hit,
                    out SpatialCellId destination,
                    out DigTunnelCellVisual? visual))
                {
                    return DigSelectedResidentTarget.Movement(destination, visual);
                }
            }

            return default;
        }
    }
}
