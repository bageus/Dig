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
            CellId? excavationCandidate = null;
            DigSelectedResidentTarget visibleMovement = default;
            bool hasVisibleMovement = false;
            DigSelectedResidentTarget hiddenMovement = default;
            bool hasHiddenMovement = false;
            for (int index = 0; index < hits.Length; index++)
            {
                RaycastHit hit = hits[index];
                DigAgentVisual? resident = hit.collider == null
                    ? null
                    : hit.collider.GetComponentInParent<DigAgentVisual>();
                if (resident != null)
                {
                    continue;
                }

                if (_buildingRenderer!.TryGetBuilding(hit, out _)
                    || _itemRenderer!.TryGetItem(hit, out _))
                {
                    break;
                }

                if (!excavationCandidate.HasValue)
                {
                    excavationCandidate = ResolveExcavationTarget(hit);
                }

                if (!TryResolveTunnelDestination(
                    hit,
                    out SpatialCellId destination,
                    out DigTunnelCellVisual? visual))
                {
                    continue;
                }

                DigSelectedResidentTarget movement =
                    DigSelectedResidentTarget.Movement(destination, visual);
                bool visible = visual == null
                    || visual.GetComponent<Renderer>().enabled;
                if (visible && !hasVisibleMovement)
                {
                    visibleMovement = movement;
                    hasVisibleMovement = true;
                }
                else if (!visible && !hasHiddenMovement)
                {
                    hiddenMovement = movement;
                    hasHiddenMovement = true;
                }
            }

            if (hasVisibleMovement)
            {
                return visibleMovement;
            }

            if (hasHiddenMovement)
            {
                return hiddenMovement;
            }

            return excavationCandidate.HasValue
                ? DigSelectedResidentTarget.Excavation(excavationCandidate.Value)
                : default;
        }
    }
}
