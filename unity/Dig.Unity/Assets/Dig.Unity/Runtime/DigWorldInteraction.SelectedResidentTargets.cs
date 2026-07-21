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
            HashSet<SpatialCellId> seenMovementCells = new HashSet<SpatialCellId>();
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

                if (!TryResolveTunnelDestination(
                        hit,
                        out SpatialCellId destination,
                        out DigTunnelCellVisual? visual)
                    || !seenMovementCells.Add(destination))
                {
                    continue;
                }

                float screenDistance = ResolveMovementPointerDistance(
                    destination,
                    visual,
                    pointer);
                if (screenDistance < bestScreenDistance - 0.01f
                    || (Mathf.Abs(screenDistance - bestScreenDistance) <= 0.01f
                        && hit.distance < bestRayDistance))
                {
                    bestMovement = DigSelectedResidentTarget.Movement(destination, visual);
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
            SpatialCellId destination,
            DigTunnelCellVisual? visual,
            Vector2 pointer)
        {
            Vector3 world = visual == null
                ? DigTunnelProjection.FloorWorldPosition(destination)
                : visual.transform.position;
            Vector3 screen = _camera!.WorldToScreenPoint(world);
            if (screen.z <= 0f)
            {
                return float.PositiveInfinity;
            }

            float distance = Vector2.Distance(pointer, new Vector2(screen.x, screen.y));
            if (visual != null)
            {
                Renderer? renderer = visual.GetComponent<Renderer>();
                if (renderer != null && !renderer.enabled)
                {
                    distance += 0.75f;
                }
            }

            return distance;
        }
    }
}
