using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigCaveRoomPreviewRenderer
    {
        private static readonly int[] FillTriangles =
        {
            0, 2, 1, 0, 3, 2,
            1, 2, 0, 2, 3, 0,
            4, 5, 6, 4, 6, 7,
            6, 5, 4, 7, 6, 4,
            0, 1, 5, 0, 5, 4,
            5, 1, 0, 4, 5, 0,
            3, 7, 6, 3, 6, 2,
            6, 7, 3, 2, 6, 3,
            0, 4, 7, 0, 7, 3,
            7, 4, 0, 3, 7, 0,
            1, 2, 6, 1, 6, 5,
            6, 2, 1, 5, 6, 1,
        };

        private void UpdateFill(Vector3[] corners)
        {
            if (_fillMesh == null || _fillRenderer == null)
            {
                return;
            }

            _fillMesh.Clear();
            _fillMesh.vertices = corners;
            _fillMesh.triangles = FillTriangles;
            _fillMesh.RecalculateNormals();
            _fillMesh.RecalculateBounds();
            _fillRenderer.enabled = true;
        }
    }
}