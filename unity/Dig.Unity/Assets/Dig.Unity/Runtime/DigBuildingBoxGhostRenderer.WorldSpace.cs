using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigBuildingBoxGhostRenderer
    {
        private void LateUpdate()
        {
            if (_root == null)
            {
                return;
            }

            // CellWorldPosition already returns world coordinates. Keep the ghost root
            // outside the bootstrap's rotated local frame so the preview stays under
            // the pointer instead of appearing above the map.
            _root.position = Vector3.zero;
            _root.rotation = Quaternion.identity;
            _root.localScale = Vector3.one;
        }
    }
}
