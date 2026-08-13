using System;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    internal sealed class DigTunnelManualPlacementGhostRenderer : MonoBehaviour
    {
        private static readonly Color ValidTint =
            new Color(0.34f, 0.92f, 0.48f, 0.52f);
        private static readonly Color InvalidTint =
            new Color(0.96f, 0.24f, 0.20f, 0.52f);

        private Transform? _root;
        private TunnelManualWorkKind? _kind;
        private Material? _material;

        internal void Render(
            TunnelManualWorkKind kind,
            CellId cell,
            bool valid)
        {
            EnsureRoot();
            if (_kind != kind)
            {
                Rebuild(kind);
            }

            _root!.position = new Vector3(
                cell.X,
                DigTunnelProjection.WalkSurfaceY(cell.Y),
                DigTunnelProjection.DepthOrigin
                    + (cell.Z * DigTunnelProjection.DepthSpacing));
            _root.gameObject.SetActive(true);
            _material!.color = valid ? ValidTint : InvalidTint;
        }

        internal void Clear()
        {
            if (_root != null)
            {
                _root.gameObject.SetActive(false);
            }
        }

        private void EnsureRoot()
        {
            if (_root != null)
            {
                return;
            }

            _root = new GameObject("Tunnel manual placement ghost").transform;
            _root.SetParent(transform, worldPositionStays: true);
            Shader shader = Shader.Find("Dig/Stylized Unlit")
                ?? Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Standard")
                ?? throw new InvalidOperationException(
                    "Manual tunnel preview requires a compatible shader.");
            _material = new Material(shader)
            {
                name = "Tunnel manual placement ghost material",
                color = ValidTint,
            };
        }

        private void Rebuild(TunnelManualWorkKind kind)
        {
            for (int index = _root!.childCount - 1; index >= 0; index--)
            {
                Destroy(_root.GetChild(index).gameObject);
            }

            _kind = kind;
            if (kind == TunnelManualWorkKind.WoodenSupport)
            {
                CreatePart(
                    "Wooden support preview",
                    new Vector3(0f, 0.46f, 0.15f),
                    new Vector3(0.16f, 0.92f, 0.12f));
                return;
            }

            if (kind == TunnelManualWorkKind.JunctionStoneTrim)
            {
                CreateJunctionCorner();
                return;
            }

            CreateStoneFrame();
        }

        private void CreateJunctionCorner()
        {
            CreatePart(
                "Junction reinforcement horizontal",
                new Vector3(0.22f, 0.06f, 0.16f),
                new Vector3(0.52f, 0.12f, 0.12f));
            CreatePart(
                "Junction reinforcement vertical",
                new Vector3(0.42f, 0.28f, 0.16f),
                new Vector3(0.12f, 0.56f, 0.12f));
            CreatePart(
                "Junction reinforcement diagonal",
                new Vector3(0.25f, 0.25f, 0.16f),
                new Vector3(0.10f, 0.58f, 0.10f));
            _root!.GetChild(_root.childCount - 1).localRotation =
                Quaternion.Euler(0f, 0f, -45f);
        }

        private void CreateStoneFrame()
        {
            CreatePart(
                "Stone trim preview front",
                new Vector3(0f, 0.03f, 0.18f),
                new Vector3(0.88f, 0.06f, 0.10f));
            CreatePart(
                "Stone trim preview back",
                new Vector3(0f, 0.03f, -0.18f),
                new Vector3(0.88f, 0.06f, 0.10f));
            CreatePart(
                "Stone trim preview left",
                new Vector3(-0.39f, 0.03f, 0f),
                new Vector3(0.10f, 0.06f, 0.46f));
            CreatePart(
                "Stone trim preview right",
                new Vector3(0.39f, 0.03f, 0f),
                new Vector3(0.10f, 0.06f, 0.46f));
        }

        private void CreatePart(
            string name,
            Vector3 localPosition,
            Vector3 localScale)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(_root, worldPositionStays: false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            Collider collider = part.GetComponent<Collider>();
            collider.enabled = false;
            part.GetComponent<MeshRenderer>().sharedMaterial = _material;
            part.layer = 2;
        }

        private void OnDestroy()
        {
            if (_material != null)
            {
                Destroy(_material);
            }
        }
    }
}
