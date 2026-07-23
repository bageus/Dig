using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    internal sealed class DigTerrainChunkVisual : MonoBehaviour
    {
        private static readonly Color DesignatedColor =
            new Color(0.95f, 0.47f, 0.12f, 1f);
        private MeshFilter? _filter;
        private MeshRenderer? _renderer;
        private Mesh? _mesh;
        private MaterialPropertyBlock? _propertyBlock;

        internal ulong Signature { get; private set; }
        internal bool IsInitialized => _mesh != null;

        internal void Invalidate()
        {
            Signature = ulong.MaxValue;
        }

        internal void Apply(
            DigTerrainChunkKey chunk,
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
            name = $"Terrain chunk {chunk}";
            Signature = signature;
            _mesh!.Clear();
            _mesh.indexFormat = data.Vertices.Length > ushort.MaxValue
                ? IndexFormat.UInt32
                : IndexFormat.UInt16;
            _mesh.vertices = TransformPositions(data.Vertices);
            _mesh.normals = TransformDirections(data.Normals);
            _mesh.colors = data.Colors;
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
            ApplySurfaceStateTints(data);
            gameObject.SetActive(data.Vertices.Length > 0);
        }

        internal Mesh? ResolveMesh()
        {
            return _mesh;
        }

        internal static Vector3 TransformBuilderPosition(Vector3 position)
        {
            // The chunk builder stores coordinates as (logical X, depth Z,
            // vertical Y). Unity runtime projection uses (X, -Y, depth Z).
            return new Vector3(position.x, -position.z, position.y);
        }

        private static Vector3[] TransformPositions(Vector3[] source)
        {
            Vector3[] result = new Vector3[source.Length];
            for (int index = 0; index < source.Length; index++)
            {
                result[index] = TransformBuilderPosition(source[index]);
            }

            return result;
        }

        private static Vector3[] TransformDirections(Vector3[] source)
        {
            Vector3[] result = new Vector3[source.Length];
            for (int index = 0; index < source.Length; index++)
            {
                result[index] = TransformBuilderPosition(source[index]);
            }

            return result;
        }

        private void ApplySurfaceStateTints(DigTerrainChunkMeshData data)
        {
            _propertyBlock ??= new MaterialPropertyBlock();
            for (int index = 0; index < data.MaterialKeys.Length; index++)
            {
                if (data.MaterialKeys[index].State != DigTerrainSurfaceState.Designated)
                {
                    _renderer!.SetPropertyBlock(null, index);
                    continue;
                }

                _propertyBlock.Clear();
                _propertyBlock.SetColor("_BaseColor", DesignatedColor);
                _propertyBlock.SetColor("_Color", DesignatedColor);
                _renderer!.SetPropertyBlock(_propertyBlock, index);
            }
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
