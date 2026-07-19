using System;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigWorldInteraction
    {
        private RaycastHit[] GetPointerHits()
        {
            Ray ray = _camera!.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, 500f);
            Array.Sort(hits, ComparePointerHits);
            return hits;
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
