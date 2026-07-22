using System;
using System.Collections.Generic;
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
            CellId movementCell,
            float movementOffsetX,
            Vector3 movementWorldPosition)
        {
            Kind = kind;
            ExcavationCell = excavationCell;
            MovementCell = movementCell;
            MovementOffsetX = movementOffsetX;
            MovementWorldPosition = movementWorldPosition;
        }

        internal DigSelectedResidentTargetKind Kind { get; }
        internal CellId ExcavationCell { get; }
        internal CellId MovementCell { get; }
        internal float MovementOffsetX { get; }
        internal Vector3 MovementWorldPosition { get; }

        internal static DigSelectedResidentTarget Excavation(CellId cell)
        {
            return new DigSelectedResidentTarget(
                DigSelectedResidentTargetKind.Excavation,
                cell,
                default,
                0f,
                default);
        }

        internal static DigSelectedResidentTarget Movement(
            DigTunnelMovementDestination destination)
        {
            return new DigSelectedResidentTarget(
                DigSelectedResidentTargetKind.Movement,
                default,
                destination.Cell,
                destination.OffsetX,
                destination.WorldPosition);
        }
    }

    public sealed partial class DigWorldInteraction
    {
        private DigSelectedResidentTarget ResolveSelectedResidentTarget()
        {
            return ResolveSelectedResidentTarget(GetPointerHits());
        }

        private DigSelectedResidentTarget ResolveSelectedResidentTarget(
            RaycastHit[] hits)
        {
            if (hits == null)
            {
                throw new ArgumentNullException(nameof(hits));
            }

            CellId? excavationCandidate = null;
            DigSelectedResidentTarget bestMovement = default;
            bool hasMovement = false;
            float bestScreenDistance = float.PositiveInfinity;
            float bestRayDistance = float.PositiveInfinity;
            HashSet<CellId> seenMovementCells = new HashSet<CellId>();
            Vector2 pointer = Input.mousePosition;
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

                if (!_tunnelRenderer!.TryGetMovementTarget(
                        hit,
                        out DigTunnelMovementDestination destination)
                    || !seenMovementCells.Add(destination.Cell))
                {
                    continue;
                }

                float screenDistance = ResolveMovementPointerDistance(
                    destination.WorldPosition,
                    pointer);
                if (screenDistance < bestScreenDistance - 0.01f
                    || (Mathf.Abs(screenDistance - bestScreenDistance) <= 0.01f
                        && hit.distance < bestRayDistance))
                {
                    bestMovement = DigSelectedResidentTarget.Movement(destination);
                    bestScreenDistance = screenDistance;
                    bestRayDistance = hit.distance;
                    hasMovement = true;
                }
            }

            if (hasMovement)
            {
                return bestMovement;
            }

            return excavationCandidate.HasValue
                ? DigSelectedResidentTarget.Excavation(excavationCandidate.Value)
                : default;
        }

        private bool TryResolveSelectedResidentMovementTarget(
            RaycastHit[] hits,
            out DigSelectedResidentTarget target)
        {
            target = ResolveSelectedResidentTarget(hits);
            return target.Kind == DigSelectedResidentTargetKind.Movement;
        }

        private float ResolveMovementPointerDistance(
            Vector3 world,
            Vector2 pointer)
        {
            Vector3 screen = _camera!.WorldToScreenPoint(world);
            if (screen.z <= 0f)
            {
                return float.PositiveInfinity;
            }

            return Vector2.Distance(pointer, new Vector2(screen.x, screen.y));
        }
    }
}
