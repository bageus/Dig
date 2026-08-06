using System;
using Dig.Application.Tunnels;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using UnityEngine;

namespace Dig.Unity
{
[DisallowMultipleComponent]
internal sealed class DigTunnelReinforcementGhostRenderer : MonoBehaviour
{
    private static readonly Color Valid = new Color(0.45f, 0.92f, 0.62f, 0.48f);
    private static readonly Color Invalid = new Color(0.96f, 0.28f, 0.22f, 0.48f);
    private Transform? _root;
    private Material? _material;
    private TunnelManualReinforcementKind? _kind;

    internal void Render(
        TunnelManualReinforcementPlan? plan,
        CellId cell,
        bool valid)
    {
        EnsureResources();
        TunnelManualReinforcementKind kind = plan?.Kind
            ?? TunnelManualReinforcementKind.StoneFloorTrim;
        if (_kind != kind || _root!.childCount == 0)
        {
            Rebuild(kind);
        }

        _root!.gameObject.SetActive(true);
        _root.position = new Vector3(
            cell.X,
            DigTunnelProjection.WalkSurfaceY(cell.Y),
            DigTunnelProjection.DepthOrigin
                + (cell.Z * DigTunnelProjection.DepthSpacing));
        _material!.color = valid ? Valid : Invalid;
    }

    internal void Clear()
    {
        if (_root != null)
        {
            _root.gameObject.SetActive(false);
        }
    }

    private void Rebuild(TunnelManualReinforcementKind kind)
    {
        for (int index = _root!.childCount - 1; index >= 0; index--)
        {
            Destroy(_root.GetChild(index).gameObject);
        }

        _kind = kind;
        switch (kind)
        {
            case TunnelManualReinforcementKind.WoodenSupport:
                CreatePart("Wooden support ghost", new Vector3(0f, 0.46f, 0.15f),
                    new Vector3(0.16f, 0.92f, 0.12f));
                break;
            case TunnelManualReinforcementKind.JunctionStoneTrim:
                CreatePart("Junction trim front", new Vector3(0f, 0.03f, 0.18f),
                    new Vector3(0.88f, 0.06f, 0.10f));
                CreatePart("Junction trim back", new Vector3(0f, 0.03f, -0.18f),
                    new Vector3(0.88f, 0.06f, 0.10f));
                CreatePart("Junction trim left", new Vector3(-0.39f, 0.03f, 0f),
                    new Vector3(0.10f, 0.06f, 0.46f));
                CreatePart("Junction trim right", new Vector3(0.39f, 0.03f, 0f),
                    new Vector3(0.10f, 0.06f, 0.46f));
                break;
            default:
                CreatePart("Stone floor reinforcement ghost", new Vector3(0f, 0.03f, 0f),
                    new Vector3(0.86f, 0.06f, 0.72f));
                break;
        }
    }

    private void CreatePart(string name, Vector3 position, Vector3 scale)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        part.transform.SetParent(_root, worldPositionStays: false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        part.GetComponent<MeshRenderer>().sharedMaterial = _material;
        Collider collider = part.GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
        }
    }

    private void EnsureResources()
    {
        if (_root == null)
        {
            _root = new GameObject("Tunnel Reinforcement Ghost").transform;
            _root.SetParent(transform, worldPositionStays: false);
        }

        if (_material == null)
        {
            Shader shader = Shader.Find("Dig/Stylized Unlit")
                ?? Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Standard")
                ?? throw new InvalidOperationException("Reinforcement ghost shader unavailable.");
            _material = new Material(shader)
            {
                name = "Tunnel reinforcement ghost material",
                color = Valid,
            };
        }
    }
}
}
