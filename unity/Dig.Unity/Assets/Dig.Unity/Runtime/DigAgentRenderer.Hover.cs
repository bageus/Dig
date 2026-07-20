using System;
using UnityEngine;

namespace Dig.Unity
{

public sealed partial class DigAgentRenderer
{
    private const float ResidentHoverRadiusPixels = 22f;
    private DigAgentVisual? _hoveredAgent;

    internal void SetHovered(DigAgentVisual? agent)
    {
        if (_hoveredAgent == agent)
        {
            return;
        }

        _hoveredAgent?.SetHovered(false);
        _hoveredAgent = agent != null && agent.Model.IsAlive ? agent : null;
        _hoveredAgent?.SetHovered(true);
    }

    internal bool TryResolveHoveredAgent(
        Camera camera,
        Vector2 screenPoint,
        out string residentId)
    {
        if (camera == null)
        {
            throw new ArgumentNullException(nameof(camera));
        }

        Ray ray = camera.ScreenPointToRay(screenPoint);
        RaycastHit[] hits = Physics.RaycastAll(ray, 500f);
        Array.Sort(hits, CompareHoverHits);
        for (int index = 0; index < hits.Length; index++)
        {
            Collider collider = hits[index].collider;
            DigAgentVisual? agent = collider == null
                ? null
                : collider.GetComponentInParent<DigAgentVisual>();
            if (agent != null)
            {
                residentId = agent.Model.Id;
                return true;
            }
        }

        float bestDistance = ResidentHoverRadiusPixels * ResidentHoverRadiusPixels;
        string? bestId = null;
        foreach (DigAgentVisual agent in _agents.Values)
        {
            Vector3 projected = camera.WorldToScreenPoint(agent.transform.position);
            if (projected.z <= 0f)
            {
                continue;
            }

            Vector2 delta = new Vector2(projected.x, projected.y) - screenPoint;
            float distance = delta.sqrMagnitude;
            if (distance <= bestDistance)
            {
                bestDistance = distance;
                bestId = agent.Model.Id;
            }
        }

        residentId = bestId ?? string.Empty;
        return bestId != null;
    }

    private static int CompareHoverHits(RaycastHit left, RaycastHit right)
    {
        int distance = left.distance.CompareTo(right.distance);
        if (distance != 0)
        {
            return distance;
        }

        int leftId = left.collider == null
            ? int.MinValue
            : left.collider.GetInstanceID();
        int rightId = right.collider == null
            ? int.MinValue
            : right.collider.GetInstanceID();
        return leftId.CompareTo(rightId);
    }
}

}
