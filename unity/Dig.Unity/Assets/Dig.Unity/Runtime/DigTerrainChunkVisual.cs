using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    internal sealed class DigTerrainChunkVisual : MonoBehaviour
    {
        private MeshFilter? _filter;
        private MeshRenderer? _renderer;
        private Mesh? _mesh;

        internal ulong Signature { get; private set; }

        internal void Invalidate()
        {
            Signature = ulong.MaxValue;
        }

        internal void Apply(
            Vector2Int chunk,
            ulong signature,
            DigTerrainChunkMeshData data,
            Material[] materials)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (materials == null)
            {
                throw new ArgumentNullException(nameof(materials));
            }

            EnsureComponents();
            name = $"Terrain chunk {chunk.x},{chunk.y}";
            Signature = signature;
            _mesh!.Clear();
            _mesh.indexFormat = data.Vertices.Length > ushort.MaxValue
                ? IndexFormat.UInt32
                : IndexFormat.UInt16;
            _mesh.vertices = data.Vertices;
            _mesh.normals = data.Normals;
            _mesh.subMeshCount = data.Triangles.Length;
            for (int index = 0; index < data.Triangles.Length; index++)
            {
                _mesh.SetTriangles(
                    data.Triangles[index],
                    index,
                    calculateBounds: false);
            }

            _mesh.RecalculateBounds();
            _renderer!.sharedMaterials = materials;
            gameObject.SetActive(data.Vertices.Length > 0);
        }

        private void EnsureComponents()
        {
            if (_filter == null)
            {
                _filter = GetComponent<MeshFilter>();
                if (_filter == null)
                {
                    _filter = gameObject.AddComponent<MeshFilter>();
                }
            }

            if (_renderer == null)
            {
                _renderer = GetComponent<MeshRenderer>();
                if (_renderer == null)
                {
                    _renderer = gameObject.AddComponent<MeshRenderer>();
                }
            }

            if (_mesh == null)
            {
                _mesh = new Mesh
                {
                    name = $"{name} mesh",
                };
                _mesh.MarkDynamic();
                _filter.sharedMesh = _mesh;
            }
        }

        private void OnDestroy()
        {
            if (_mesh != null)
            {
                Destroy(_mesh);
            }
        }
    }
}
