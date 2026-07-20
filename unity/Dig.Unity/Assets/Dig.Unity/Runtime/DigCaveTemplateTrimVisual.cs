using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    internal sealed class DigCaveTemplateTrimVisual : MonoBehaviour
    {
        private Mesh? _mesh;
        private MeshFilter? _filter;
        private MeshRenderer? _renderer;

        internal bool IsInitialized { get; private set; }

        internal ulong Signature { get; private set; } = ulong.MaxValue;

        internal void Apply(
            ulong signature,
            DigCaveTemplateTrimMeshData data,
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

            if (materials.Length != data.Triangles.Length)
            {
                throw new ArgumentException(
                    "Cave template trim materials must match submeshes.",
                    nameof(materials));
            }

            EnsureComponents();
            _mesh!.Clear();
            _mesh.indexFormat = data.Vertices.Length > ushort.MaxValue
                ? IndexFormat.UInt32
                : IndexFormat.UInt16;
            _mesh.vertices = data.Vertices;
            _mesh.normals = data.Normals;
            _mesh.subMeshCount = data.Triangles.Length;
            for (int index = 0; index < data.Triangles.Length; index++)
            {
                _mesh.SetTriangles(data.Triangles[index], index, calculateBounds: false);
            }

            _mesh.RecalculateBounds();
            _renderer!.sharedMaterials = materials;
            Signature = signature;
            IsInitialized = true;
        }

        internal void Invalidate()
        {
            Signature = ulong.MaxValue;
        }

        internal Mesh? ResolveMesh()
        {
            return _mesh;
        }

        private void EnsureComponents()
        {
            if (_mesh != null)
            {
                return;
            }

            _filter = gameObject.GetComponent<MeshFilter>();
            if (_filter == null)
            {
                _filter = gameObject.AddComponent<MeshFilter>();
            }

            _renderer = gameObject.GetComponent<MeshRenderer>();
            if (_renderer == null)
            {
                _renderer = gameObject.AddComponent<MeshRenderer>();
            }

            _mesh = new Mesh
            {
                name = $"{gameObject.name} mesh",
            };
            _mesh.MarkDynamic();
            _filter.sharedMesh = _mesh;
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
