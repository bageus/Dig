using System;
using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    public sealed class DigVisualPrefabRoot : MonoBehaviour
    {
        [SerializeField]
        private Transform? modelRoot;

        [SerializeField]
        private Renderer[] tintRenderers = Array.Empty<Renderer>();

        [SerializeField]
        private Collider[] selectionColliders = Array.Empty<Collider>();

        public Transform ModelRoot => modelRoot == null ? transform : modelRoot;

        public Renderer[] ResolveTintRenderers()
        {
            return tintRenderers.Length > 0
                ? tintRenderers
                : ModelRoot.GetComponentsInChildren<Renderer>(includeInactive: true);
        }

        public Collider[] ResolveSelectionColliders()
        {
            return selectionColliders.Length > 0
                ? selectionColliders
                : ModelRoot.GetComponentsInChildren<Collider>(includeInactive: true);
        }
    }
}
