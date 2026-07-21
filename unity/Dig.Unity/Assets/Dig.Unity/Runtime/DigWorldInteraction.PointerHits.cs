using System;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigWorldInteraction
    {
        private const float ResidentScreenPickPadding = 8f;
        private DigAgentVisual? _hoveredResident;

        private RaycastHit[] GetPointerHits()
        {
            Ray ray = _camera!.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, 500f);
            Array.Sort(hits, ComparePointerHits);
            return hits;
        }

        private void UpdateResidentHover()
        {
            if (_agentRenderer == null || _camera == null || _hud == null)
            {
                return;
            }

            DigAgentVisual? next = null;
            if (!_hud.ContainsScreenPoint(Input.mousePosition))
            {
                RaycastHit[] hits = GetPointerHits();
                if (TryResolveAgentHit(hits, out DigAgentVisual candidate))
                {
                    next = candidate;
                }
            }

            if (ReferenceEquals(next, _hoveredResident))
            {
                return;
            }

            _hoveredResident?.SetHovered(false);
            _hoveredResident = next;
            _hoveredResident?.SetHovered(true);
        }

        private bool TryResolveAgentHit(
            RaycastHit[] hits,
            out DigAgentVisual agent)
        {
            bool blockedByForegroundObject = false;
            for (int index = 0; index < hits.Length; index++)
            {
                Collider? collider = hits[index].collider;
                if (collider == null)
                {
                    continue;
                }

                DigAgentVisual? candidate = collider.GetComponentInParent<DigAgentVisual>();
                if (candidate != null)
                {
                    agent = candidate;
                    return true;
                }

                if ((_buildingRenderer != null
                        && _buildingRenderer.TryGetBuilding(hits[index], out _))
                    || (_itemRenderer != null
                        && _itemRenderer.TryGetItem(hits[index], out _)))
                {
                    blockedByForegroundObject = true;
                    break;
                }
            }

            if (!blockedByForegroundObject && TryResolveAgentNearPointer(out agent))
            {
                return true;
            }

            agent = null!;
            return false;
        }

        private bool TryResolveAgentNearPointer(out DigAgentVisual agent)
        {
            DigAgentVisual[] candidates =
                _agentRenderer!.GetComponentsInChildren<DigAgentVisual>();
            Vector2 pointer = Input.mousePosition;
            float bestDistance = float.PositiveInfinity;
            float bestDepth = float.PositiveInfinity;
            DigAgentVisual? best = null;
            for (int index = 0; index < candidates.Length; index++)
            {
                DigAgentVisual candidate = candidates[index];
                if (!candidate.Model.IsAlive
                    || !TryProjectResidentBounds(candidate, out Rect screenBounds, out float depth))
                {
                    continue;
                }

                float distance = DistanceToRect(pointer, screenBounds);
                if (distance > ResidentScreenPickPadding)
                {
                    continue;
                }

                if (distance < bestDistance - 0.01f
                    || (Mathf.Abs(distance - bestDistance) <= 0.01f && depth < bestDepth))
                {
                    bestDistance = distance;
                    bestDepth = depth;
                    best = candidate;
                }
            }

            agent = best!;
            return best != null;
        }

        private bool TryProjectResidentBounds(
            DigAgentVisual resident,
            out Rect screenBounds,
            out float depth)
        {
            Collider? collider = resident.GetComponent<Collider>();
            Bounds bounds = collider == null
                ? new Bounds(resident.transform.position, Vector3.one)
                : collider.bounds;
            Vector3 minimum = bounds.min;
            Vector3 maximum = bounds.max;
            float minX = float.PositiveInfinity;
            float minY = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float maxY = float.NegativeInfinity;
            depth = float.PositiveInfinity;
            bool projected = false;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 world = new Vector3(
                    (corner & 1) == 0 ? minimum.x : maximum.x,
                    (corner & 2) == 0 ? minimum.y : maximum.y,
                    (corner & 4) == 0 ? minimum.z : maximum.z);
                Vector3 screen = _camera!.WorldToScreenPoint(world);
                if (screen.z <= 0f)
                {
                    continue;
                }

                projected = true;
                minX = Mathf.Min(minX, screen.x);
                minY = Mathf.Min(minY, screen.y);
                maxX = Mathf.Max(maxX, screen.x);
                maxY = Mathf.Max(maxY, screen.y);
                depth = Mathf.Min(depth, screen.z);
            }

            screenBounds = projected
                ? Rect.MinMaxRect(minX, minY, maxX, maxY)
                : default;
            return projected;
        }

        private static float DistanceToRect(Vector2 point, Rect rect)
        {
            float dx = point.x < rect.xMin
                ? rect.xMin - point.x
                : point.x > rect.xMax
                    ? point.x - rect.xMax
                    : 0f;
            float dy = point.y < rect.yMin
                ? rect.yMin - point.y
                : point.y > rect.yMax
                    ? point.y - rect.yMax
                    : 0f;
            return Mathf.Sqrt((dx * dx) + (dy * dy));
        }

        private static int ComparePointerHits(RaycastHit left, RaycastHit right)
        {
            int distance = left.distance.CompareTo(right.distance);
            if (distance != 0)
            {
                return distance;
            }

            int leftId = left.collider == null ? int.MinValue : left.collider.GetInstanceID();
            int rightId = right.collider == null ? int.MinValue : right.collider.GetInstanceID();
            return leftId.CompareTo(rightId);
        }
    }
}
