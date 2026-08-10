using System;
using UnityEngine;

namespace Dig.Unity
{
    internal static class DigWorldItemGrounding
    {
        internal static void PlaceOnFloor(Transform root, Vector3 floorAnchor)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            DigWorldItemVisual? worldItem = root.GetComponent<DigWorldItemVisual>();
            worldItem?.ApplyLooseWorldFloorPose();
            root.position = floorAnchor;
            if (!TryResolveVisibleWorldBounds(root, out Bounds bounds))
            {
                return;
            }

            root.position += Vector3.up * (floorAnchor.y - bounds.min.y);
        }

        internal static Bounds ResolveLocalBounds(
            Transform root,
            Vector3 fallbackSize)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            if (!TryResolveVisibleWorldBounds(root, out Bounds worldBounds))
            {
                Vector3 size = PositiveSize(fallbackSize);
                return new Bounds(
                    new Vector3(0f, size.y * 0.5f, 0f),
                    size);
            }

            Vector3 min = worldBounds.min;
            Vector3 max = worldBounds.max;
            Vector3[] corners =
            {
                new Vector3(min.x, min.y, min.z),
                new Vector3(min.x, min.y, max.z),
                new Vector3(min.x, max.y, min.z),
                new Vector3(min.x, max.y, max.z),
                new Vector3(max.x, min.y, min.z),
                new Vector3(max.x, min.y, max.z),
                new Vector3(max.x, max.y, min.z),
                new Vector3(max.x, max.y, max.z),
            };
            Bounds local = new Bounds(root.InverseTransformPoint(corners[0]), Vector3.zero);
            for (int index = 1; index < corners.Length; index++)
            {
                local.Encapsulate(root.InverseTransformPoint(corners[index]));
            }

            return local;
        }

        private static bool TryResolveVisibleWorldBounds(
            Transform root,
            out Bounds bounds)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(
                includeInactive: true);
            bool hasBounds = false;
            bounds = default;
            for (int index = 0; index < renderers.Length; index++)
            {
                Renderer renderer = renderers[index];
                if (!renderer.enabled || !IsActiveForGrounding(renderer.transform, root))
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return hasBounds;
        }

        private static bool IsActiveForGrounding(Transform value, Transform root)
        {
            Transform? current = value;
            while (current != null)
            {
                if (!current.gameObject.activeSelf)
                {
                    return false;
                }

                if (ReferenceEquals(current, root))
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static Vector3 PositiveSize(Vector3 value)
        {
            return new Vector3(
                Mathf.Max(0.01f, value.x),
                Mathf.Max(0.01f, value.y),
                Mathf.Max(0.01f, value.z));
        }
    }
}
