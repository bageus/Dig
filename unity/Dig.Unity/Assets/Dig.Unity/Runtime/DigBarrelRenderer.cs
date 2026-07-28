using System;
using System.Collections.Generic;
using Dig.Domain.Core;
using Dig.Domain.WorldObjects;
using UnityEngine;

namespace Dig.Unity
{

[DisallowMultipleComponent]
public sealed class DigBarrelRenderer : MonoBehaviour
{
    private readonly Dictionary<string, DigBarrelVisual> _visuals =
        new Dictionary<string, DigBarrelVisual>(StringComparer.Ordinal);
    private Transform? _root;
    private string? _highlightedId;

    internal int ActiveCount => _visuals.Count;

    internal void Render(IReadOnlyList<BarrelSnapshot> barrels)
    {
        if (barrels == null)
        {
            throw new ArgumentNullException(nameof(barrels));
        }

        EnsureRoot();
        HashSet<string> visible = new HashSet<string>(StringComparer.Ordinal);
        for (int index = 0; index < barrels.Count; index++)
        {
            BarrelSnapshot barrel = barrels[index];
            if (barrel.Lifecycle != BarrelLifecycle.Supported)
            {
                continue;
            }

            string id = barrel.BarrelId.ToString();
            visible.Add(id);
            if (!_visuals.TryGetValue(id, out DigBarrelVisual? visual))
            {
                GameObject root = new GameObject("Barrel");
                root.transform.SetParent(_root, worldPositionStays: false);
                visual = root.AddComponent<DigBarrelVisual>();
                _visuals.Add(id, visual);
            }

            visual.transform.rotation = Quaternion.identity;
            visual.Configure(barrel);
            visual.SetHighlighted(
                string.Equals(_highlightedId, id, StringComparison.Ordinal));
            visual.transform.position = DigTunnelProjection.ResidentWorldPosition(
                barrel.Cell.X,
                barrel.Cell.Y,
                barrel.Cell.Z) + new Vector3(
                    0f,
                    DigTunnelProjection.ResidentFootSink,
                    0f);
        }

        RemoveMissing(visible);
    }

    internal bool TryGetBarrel(RaycastHit hit, out DigBarrelVisual visual)
    {
        DigBarrelVisual? candidate = hit.collider == null
            ? null
            : hit.collider.GetComponentInParent<DigBarrelVisual>();
        if (candidate != null
            && candidate.Model.IsAttackable
            && _visuals.TryGetValue(
                candidate.Model.BarrelId.ToString(),
                out DigBarrelVisual? tracked)
            && ReferenceEquals(candidate, tracked))
        {
            visual = candidate;
            return true;
        }

        visual = null!;
        return false;
    }

    internal void SetHighlighted(EntityId? barrelId)
    {
        string? next = barrelId?.ToString();
        if (string.Equals(_highlightedId, next, StringComparison.Ordinal))
        {
            return;
        }

        if (_highlightedId != null
            && _visuals.TryGetValue(
                _highlightedId,
                out DigBarrelVisual? previous))
        {
            previous.SetHighlighted(false);
        }

        _highlightedId = next;
        if (next != null
            && _visuals.TryGetValue(next, out DigBarrelVisual? current))
        {
            current.SetHighlighted(true);
        }
    }

    private void EnsureRoot()
    {
        if (_root != null)
        {
            return;
        }

        GameObject root = new GameObject("Barrels");
        // Runtime renderers receive already projected world positions. Preserve a
        // world-identity root so bootstrap/terrain rotation cannot lay barrels down.
        root.transform.SetParent(transform, worldPositionStays: true);
        _root = root.transform;
    }

    private void RemoveMissing(HashSet<string> visible)
    {
        List<string> removed = new List<string>();
        foreach (KeyValuePair<string, DigBarrelVisual> pair in _visuals)
        {
            if (!visible.Contains(pair.Key))
            {
                removed.Add(pair.Key);
            }
        }

        for (int index = 0; index < removed.Count; index++)
        {
            string id = removed[index];
            Destroy(_visuals[id].gameObject);
            _visuals.Remove(id);
            if (string.Equals(_highlightedId, id, StringComparison.Ordinal))
            {
                _highlightedId = null;
            }
        }
    }
}

}
