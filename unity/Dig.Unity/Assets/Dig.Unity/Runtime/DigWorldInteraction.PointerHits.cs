using System;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigWorldInteraction
    {
        private const float ResidentScreenPickRadius = 18f;

        private RaycastHit[] GetPointerHits()
        {
            Ray ray = _camera!.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, 500f);
            Array.Sort(hits, ComparePointerHits);
            return hits;
        }

        private bool TryResolveAgentHit(
            RaycastHit[] hits,
            out DigAgentVisual agent)
        {
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
            }

            return TryResolveAgentNearPointer(out agent);
        }

        private bool TryResolveAgentNearPointer(out DigAgentVisual agent)
        {
            DigAgentVisual[] candidates =
                _agentRenderer!.GetComponentsInChildren<DigAgentVisual>();
            float bestDistance = ResidentScreenPickRadius;
            DigAgentVisual? best = null;
            Vector3 pointer = Input.mousePosition;
            for (int index = 0; index < candidates.Length; index++)
            {
                Vector3 screen = _camera!.WorldToScreenPoint(candidates[index].transform.position);
                if (screen.z <= 0f)
                {
                    continue;
                }

                float distance = Vector2.Distance(
                    new Vector2(pointer.x, pointer.y),
                    new Vector2(screen.x, screen.y));
                if (distance <= bestDistance)
                {
                    bestDistance = distance;
                    best = candidates[index];
                }
            }

            agent = best!;
            return best != null;
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
