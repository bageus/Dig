using System;
using System.Collections.Generic;
using Dig.Domain.World;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigWorldInteraction
    {
        private bool TryResolveBuildingPlacementOrigin(
            RaycastHit[] hits,
            out CellId origin)
        {
            if (hits == null)
            {
                throw new ArgumentNullException(nameof(hits));
            }

            if (TryResolveBuildingPlacementMovementSurface(hits, out origin))
            {
                return true;
            }

            for (int index = 0; index < hits.Length; index++)
            {
                if (TryResolveTunnelDestination(hits[index], out origin, out _))
                {
                    return true;
                }
            }

            for (int index = 0; index < hits.Length; index++)
            {
                if (_renderer!.TryGetCell(hits[index], out DigCellVisual cell))
                {
                    origin = new CellId(cell.Model.X, cell.Model.Y, cell.Model.Z);
                    return true;
                }
            }

            origin = default;
            return false;
        }

        private bool TryResolveBuildingPlacementMovementSurface(
            RaycastHit[] hits,
            out CellId origin)
        {
            return TryResolveBuildingPlacementMovementSurface(
                hits,
                Input.mousePosition,
                out origin);
        }

        private bool TryResolveBuildingPlacementMovementSurface(
            RaycastHit[] hits,
            Vector2 pointer,
            out CellId origin)
        {
            origin = default;
            if (_tunnelRenderer == null)
            {
                return false;
            }

            bool found = false;
            float bestScreenDistance = float.PositiveInfinity;
            float bestRayDistance = float.PositiveInfinity;
            HashSet<CellId> seen = new HashSet<CellId>();
            for (int index = 0; index < hits.Length; index++)
            {
                RaycastHit hit = hits[index];
                if (!_tunnelRenderer.TryGetMovementTarget(
                        hit,
                        out DigTunnelMovementDestination target)
                    || !seen.Add(target.Cell))
                {
                    continue;
                }

                float screenDistance = ResolveMovementPointerDistance(
                    target.WorldPosition,
                    pointer);
                if (screenDistance < bestScreenDistance - 0.01f
                    || (Mathf.Abs(screenDistance - bestScreenDistance) <= 0.01f
                        && hit.distance < bestRayDistance))
                {
                    origin = target.Cell;
                    bestScreenDistance = screenDistance;
                    bestRayDistance = hit.distance;
                    found = true;
                }
            }

            return found;
        }
    }
}
