using System;
using System.Collections.Generic;
using Dig.Domain.Rooms;
using Dig.Domain.World;
using Dig.Presentation.Rendering;
using Dig.Presentation.Rooms;
using UnityEngine;

namespace Dig.Unity
{

internal sealed partial class DigRoomInfrastructureRenderer
{
    private void RenderProgress(
        RoomInfrastructureViewModel room,
        HashSet<string> visible)
    {
        for (int index = 0; index < room.CompletedUnits.Count; index++)
        {
            RoomMaterialUnitProgressViewModel unit = room.CompletedUnits[index];
            string key = room.Id + ":" + unit.StableId;
            visible.Add(key);
            if (_progress.ContainsKey(key))
            {
                continue;
            }

            GameObject piece = CreateProgressPiece(room, unit, index);
            piece.name = "Room Progress " + key;
            piece.transform.SetParent(_root, worldPositionStays: true);
            _progress.Add(key, piece);
        }
    }

    private GameObject CreateProgressPiece(
        RoomInfrastructureViewModel room,
        RoomMaterialUnitProgressViewModel unit,
        int index)
    {
        string item = unit.ItemId;
        GameObject piece;
        if (item.EndsWith("crystal", StringComparison.Ordinal))
        {
            piece = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            piece.transform.localScale = Vector3.one * 0.18f;
        }
        else
        {
            piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
        }

        Collider collider = piece.GetComponent<Collider>();
        collider.enabled = false;
        Renderer renderer = piece.GetComponent<Renderer>();
        renderer.sharedMaterial = ResolveProgressBaseMaterial();
        ApplyTint(renderer, ResolveProgressTint(item));
        ApplyProgressTransform(piece.transform, room, unit, index);
        return piece;
    }

    private static void ApplyProgressTransform(
        Transform transform,
        RoomInfrastructureViewModel room,
        RoomMaterialUnitProgressViewModel unit,
        int index)
    {
        int width = Math.Max(1, room.MaxX - room.MinX + 1);
        int required = RequiredUnits(room, unit.ItemId);
        float depth = DigTunnelProjection.CellWorldPosition(
            new CellId(0, room.MaxY, room.MarkerZ)).z - 0.04f;
        string item = unit.ItemId;
        if (item.EndsWith("stone", StringComparison.Ordinal))
        {
            float x = DistributedX(room, unit.Ordinal, required);
            transform.position = new Vector3(
                x,
                DigTunnelProjection.WalkSurfaceY(room.MaxY) + 0.04f,
                depth);
            transform.localScale = new Vector3(
                Math.Max(0.18f, (width - 0.25f) / required),
                0.10f,
                0.16f);
            return;
        }

        if (item.EndsWith("mushroom_leg", StringComparison.Ordinal))
        {
            bool right = (unit.Ordinal & 1) == 0;
            int pair = (unit.Ordinal - 1) / 2;
            int pairCount = Math.Max(1, (required + 1) / 2);
            float inset = pairCount == 1
                ? 0f
                : (pair * Math.Max(0f, width - 1f)) / (pairCount - 1);
            float x = right
                ? room.MaxX + 0.34f - inset
                : room.MinX - 0.34f + inset;
            float height = Math.Max(0.7f, room.MaxY - room.MinY + 0.5f);
            transform.position = new Vector3(
                x,
                -((room.MinY + room.MaxY) * 0.5f),
                depth);
            transform.localScale = new Vector3(0.13f, height, 0.13f);
            return;
        }

        if (item.EndsWith("iron", StringComparison.Ordinal))
        {
            float x = DistributedX(room, unit.Ordinal, required);
            transform.position = new Vector3(x, -room.MinY + 0.08f, depth);
            transform.localScale = new Vector3(0.65f, 0.10f, 0.12f);
            transform.rotation = Quaternion.Euler(
                0f,
                0f,
                (unit.Ordinal & 1) == 0 ? 28f : -28f);
            return;
        }

        float fallbackX = DistributedX(room, unit.Ordinal, required);
        float arc = required == 1
            ? 0f
            : Mathf.Sin(((unit.Ordinal - 1f) / (required - 1f)) * Mathf.PI) * 0.3f;
        transform.position = new Vector3(
            fallbackX,
            -room.MinY + 0.34f + arc,
            depth);
    }

    private static int RequiredUnits(
        RoomInfrastructureViewModel room,
        string itemId)
    {
        for (int index = 0; index < room.Materials.Count; index++)
        {
            if (string.Equals(
                    room.Materials[index].ItemId,
                    itemId,
                    StringComparison.Ordinal))
            {
                return room.Materials[index].Required;
            }
        }

        return 1;
    }

    private static float DistributedX(
        RoomInfrastructureViewModel room,
        int ordinal,
        int required)
    {
        float width = Math.Max(1f, room.MaxX - room.MinX + 1f);
        return room.MinX - 0.5f + (ordinal * width) / (required + 1f);
    }

    private Material ResolveMarkerBaseMaterial()
    {
        return _materials!.Resolve(
            RenderMaterialSemantic.Emissive,
            RenderSurfaceKind.Unlit,
            Color.white);
    }

    private static Color ResolveMarkerTint(RoomInfrastructureViewModel room)
    {
        return room.Status switch
        {
            RoomImprovementStatus.Unimproved => new Color(0.75f, 0.78f, 0.84f, 1f),
            RoomImprovementStatus.AwaitingMaterials => new Color(0.95f, 0.67f, 0.20f, 1f),
            RoomImprovementStatus.ReadyForWork => new Color(0.30f, 0.80f, 0.96f, 1f),
            RoomImprovementStatus.Improving => new Color(0.45f, 0.90f, 0.44f, 1f),
            RoomImprovementStatus.Improved => PurposeTint(room.ActivePurpose),
            _ => Color.white,
        };
    }

    private Material ResolveSelectionMaterial()
    {
        return _materials!.Resolve(
            RenderMaterialSemantic.Overlay,
            RenderSurfaceKind.Overlay,
            new Color(1f, 0.93f, 0.25f, 0.48f));
    }

    private Material ResolveProgressBaseMaterial()
    {
        return _materials!.Resolve(
            RenderMaterialSemantic.Building,
            RenderSurfaceKind.Lit,
            Color.white);
    }

    private static Color ResolveProgressTint(string itemId)
    {
        return itemId.EndsWith("stone", StringComparison.Ordinal)
            ? new Color(0.50f, 0.55f, 0.62f, 1f)
            : itemId.EndsWith("mushroom_leg", StringComparison.Ordinal)
                ? new Color(0.50f, 0.28f, 0.12f, 1f)
                : itemId.EndsWith("iron", StringComparison.Ordinal)
                    ? new Color(0.34f, 0.38f, 0.44f, 1f)
                    : new Color(0.48f, 0.86f, 1f, 1f);
    }

    private static void ApplyTint(Renderer renderer, Color tint)
    {
        MaterialPropertyBlock properties = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(properties);
        properties.SetColor("_BaseColor", tint);
        properties.SetColor("_Color", tint);
        renderer.SetPropertyBlock(properties);
    }

    private static Color PurposeTint(RoomPurposeKind purpose)
    {
        return purpose switch
        {
            RoomPurposeKind.Bedroom => new Color(0.55f, 0.62f, 1f, 1f),
            RoomPurposeKind.KitchenDining => new Color(1f, 0.58f, 0.28f, 1f),
            RoomPurposeKind.Workshop => new Color(0.88f, 0.72f, 0.24f, 1f),
            RoomPurposeKind.Farm => new Color(0.40f, 0.82f, 0.35f, 1f),
            _ => new Color(0.75f, 0.78f, 0.84f, 1f),
        };
    }
}

}
