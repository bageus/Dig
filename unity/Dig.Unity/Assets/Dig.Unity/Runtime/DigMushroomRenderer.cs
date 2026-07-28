using System;
using System.Collections.Generic;
using Dig.Domain.Ecology;
using UnityEngine;

namespace Dig.Unity
{
[DisallowMultipleComponent]
public sealed class DigMushroomRenderer : MonoBehaviour
{
    private readonly Dictionary<string, DigMushroomVisual> _visuals =
        new Dictionary<string, DigMushroomVisual>(StringComparer.Ordinal);
    private Transform? _root;

    internal int ActiveCount => _visuals.Count;

    internal void Render(IReadOnlyList<MushroomSiteSnapshot> sites)
    {
        if (sites == null)
        {
            throw new ArgumentNullException(nameof(sites));
        }

        EnsureRoot();
        HashSet<string> visible = new HashSet<string>(StringComparer.Ordinal);
        for (int index = 0; index < sites.Count; index++)
        {
            MushroomSiteSnapshot site = sites[index];
            if (!site.IsVisible)
            {
                continue;
            }

            string id = site.SiteId.ToString();
            visible.Add(id);
            if (!_visuals.TryGetValue(id, out DigMushroomVisual? visual))
            {
                GameObject root = new GameObject("Mushroom");
                root.transform.SetParent(_root, worldPositionStays: false);
                visual = root.AddComponent<DigMushroomVisual>();
                _visuals.Add(id, visual);
            }

            visual.Configure(site);
            visual.transform.position = DigTunnelProjection.ResidentWorldPosition(
                site.Cell.X,
                site.Cell.Y,
                site.Cell.Z) + new Vector3(
                    0f,
                    DigTunnelProjection.ResidentFootSink,
                    0f);
        }

        RemoveMissing(visible);
    }

    internal bool TryGetMushroom(RaycastHit hit, out DigMushroomVisual visual)
    {
        DigMushroomVisual? candidate = hit.collider == null
            ? null
            : hit.collider.GetComponentInParent<DigMushroomVisual>();
        if (candidate != null
            && candidate.Model.IsVisible
            && _visuals.TryGetValue(candidate.Model.SiteId.ToString(), out DigMushroomVisual? tracked)
            && ReferenceEquals(candidate, tracked))
        {
            visual = candidate;
            return true;
        }

        visual = null!;
        return false;
    }

    private void EnsureRoot()
    {
        if (_root != null)
        {
            return;
        }

        GameObject root = new GameObject("Mushrooms");
        _root = root.transform;
        // Mushroom roots use world-space tunnel projection and must not inherit the
        // side-view bootstrap rotation. The stem/cap Y axis must remain world-up.
        _root.SetParent(transform, worldPositionStays: true);
    }

    private void RemoveMissing(HashSet<string> visible)
    {
        List<string> removed = new List<string>();
        foreach (KeyValuePair<string, DigMushroomVisual> pair in _visuals)
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
        }
    }
}
}
