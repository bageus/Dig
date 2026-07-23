using System.Collections.Generic;
using Dig.Domain.World;
using Dig.Presentation.Rendering;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigWorldRenderer
    {
        private static readonly Color DepthDesignationColor =
            new Color(0.74f, 0.62f, 0.90f, 0.72f);
        private readonly Dictionary<CellId, GameObject> _depthDesignationCells =
            new Dictionary<CellId, GameObject>();
        private DigRenderMaterialLibrary? _depthMaterialLibrary;

        internal void SetDepthDesignationTint(CellId target)
        {
            if (_depthDesignationCells.ContainsKey(target))
            {
                return;
            }

            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = $"Depth designation cell {target}";
            marker.transform.SetParent(transform, worldPositionStays: true);
            Vector3 position = DigTunnelProjection.CellWorldPosition(target);
            position.z = DigTunnelProjection.CellWorldPosition(
                new CellId(target.X, target.Y, 0)).z
                + DigTunnelProjection.RockCellHalfExtent
                + 0.025f;
            marker.transform.position = position;
            marker.transform.rotation = Quaternion.identity;
            marker.transform.localScale = new Vector3(0.94f, 0.94f, 0.025f);

            Collider? collider = marker.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            _depthMaterialLibrary ??= GetComponent<DigRenderMaterialLibrary>()
                ?? gameObject.AddComponent<DigRenderMaterialLibrary>();
            Renderer renderer = marker.GetComponent<Renderer>();
            renderer.sharedMaterial = _depthMaterialLibrary.Resolve(
                RenderMaterialSemantic.Overlay,
                RenderSurfaceKind.Overlay,
                Color.white);
            renderer.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sortingOrder = 21;

            MaterialPropertyBlock properties = new MaterialPropertyBlock();
            properties.SetColor("_BaseColor", DepthDesignationColor);
            properties.SetColor("_Color", DepthDesignationColor);
            renderer.SetPropertyBlock(properties);
            _depthDesignationCells.Add(target, marker);
        }

        internal void RemoveDepthDesignationTint(CellId target)
        {
            if (!_depthDesignationCells.TryGetValue(target, out GameObject? marker))
            {
                return;
            }

            Destroy(marker);
            _depthDesignationCells.Remove(target);
        }
    }
}
