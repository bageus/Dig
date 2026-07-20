using System;
using System.Collections.Generic;
using Dig.Domain.Buildings;
using Dig.Presentation.Buildings;
using Dig.Presentation.World;
using UnityEngine;

namespace Dig.Unity
{
    internal sealed partial class DigRepresentativeBuildingPrefabLibrary
    {
        private GameObject BuildTemplate(
            string canonicalId,
            DigRepresentativeBuildingProfileData profile,
            BuildingVisualState state)
        {
            GameObject template = new GameObject(
                $"Representative {canonicalId} {state}");
            template.transform.SetParent(_templateRoot.transform, worldPositionStays: false);
            template.AddComponent<DigVisualPrefabRoot>();

            GameObject model = new GameObject("Model");
            model.transform.SetParent(template.transform, worldPositionStays: false);
            BoxCollider selection = model.AddComponent<BoxCollider>();
            selection.center = new Vector3(
                profile.pivotCell.x,
                0.85f,
                profile.pivotCell.y);
            selection.size = new Vector3(
                profile.footprintSize.x * 0.94f,
                1.70f,
                profile.footprintSize.y * 0.94f);

            BuildStateGeometry(model.transform, profile, state);
            DigBuildingAnchor[] anchors = BuildAnchors(model.transform, profile);
            DigBuildingPrefabAuthoring authoring =
                template.AddComponent<DigBuildingPrefabAuthoring>();
            authoring.ConfigureRuntime(
                profile.footprintSize,
                profile.pivotCell,
                anchors);
            template.SetActive(true);
            return template;
        }

        private void BuildStateGeometry(
            Transform parent,
            DigRepresentativeBuildingProfileData profile,
            BuildingVisualState state)
        {
            switch (state)
            {
                case BuildingVisualState.BuildingBox:
                    BuildCrate(parent, profile, packing: false);
                    break;
                case BuildingVisualState.Packing:
                    BuildCrate(parent, profile, packing: true);
                    break;
                default:
                    BuildProfileParts(parent, profile);
                    if (state == BuildingVisualState.Assembly)
                    {
                        BuildScaffold(parent, profile);
                    }
                    else if (state == BuildingVisualState.Damaged)
                    {
                        parent.localRotation = Quaternion.Euler(0f, 0f, -7f);
                        parent.localScale = new Vector3(0.96f, 0.92f, 0.96f);
                    }
                    break;
            }
        }

        private void BuildProfileParts(
            Transform parent,
            DigRepresentativeBuildingProfileData profile)
        {
            for (int index = 0; index < profile.parts.Length; index++)
            {
                DigRepresentativeBuildingPartData? part = profile.parts[index];
                if (part == null
                    || !part.TryResolveShape(out DigRepresentativeBuildingShape shape)
                    || !part.TryResolveDetail(out TerrainVisualDetailLevel detail))
                {
                    continue;
                }

                CreatePart(
                    parent,
                    string.IsNullOrWhiteSpace(part.name) ? $"Part {index}" : part.name,
                    shape,
                    detail,
                    part.position,
                    part.scale,
                    part.rotation);
            }
        }

        private void BuildCrate(
            Transform parent,
            DigRepresentativeBuildingProfileData profile,
            bool packing)
        {
            Vector3 center = new Vector3(
                profile.pivotCell.x,
                0.34f,
                profile.pivotCell.y);
            Vector3 size = new Vector3(
                Mathf.Max(0.62f, profile.footprintSize.x * 0.72f),
                0.68f,
                Mathf.Max(0.62f, profile.footprintSize.y * 0.72f));
            CreatePart(
                parent,
                "Building Box",
                DigRepresentativeBuildingShape.Box,
                TerrainVisualDetailLevel.Marker,
                center,
                size,
                Vector3.zero);
            CreatePart(
                parent,
                "Box Lid",
                DigRepresentativeBuildingShape.Pyramid,
                TerrainVisualDetailLevel.Reduced,
                center + new Vector3(0f, 0.42f, 0f),
                new Vector3(size.x * 0.84f, 0.22f, size.z * 0.84f),
                Vector3.zero);
            CreatePart(
                parent,
                "Box Mark",
                DigRepresentativeBuildingShape.Octahedron,
                TerrainVisualDetailLevel.Marker,
                center + new Vector3(0f, 0.02f, -(size.z * 0.54f)),
                new Vector3(0.28f, 0.34f, 0.10f),
                Vector3.zero);
            if (packing)
            {
                CreatePart(
                    parent,
                    "Packing Strap",
                    DigRepresentativeBuildingShape.Box,
                    TerrainVisualDetailLevel.Full,
                    center + new Vector3(0f, 0.04f, 0f),
                    new Vector3(size.x * 1.04f, 0.10f, 0.14f),
                    Vector3.zero);
            }
        }

        private void BuildScaffold(
            Transform parent,
            DigRepresentativeBuildingProfileData profile)
        {
            Vector3 center = new Vector3(
                profile.pivotCell.x,
                0.82f,
                profile.pivotCell.y);
            float width = profile.footprintSize.x * 0.92f;
            CreatePart(
                parent,
                "Scaffold Left",
                DigRepresentativeBuildingShape.Box,
                TerrainVisualDetailLevel.Reduced,
                center + new Vector3(-(width * 0.50f), 0f, 0f),
                new Vector3(0.12f, 1.64f, 0.12f),
                new Vector3(0f, 0f, 4f));
            CreatePart(
                parent,
                "Scaffold Right",
                DigRepresentativeBuildingShape.Box,
                TerrainVisualDetailLevel.Reduced,
                center + new Vector3(width * 0.50f, 0f, 0f),
                new Vector3(0.12f, 1.64f, 0.12f),
                new Vector3(0f, 0f, -4f));
        }

        private DigBuildingAnchor[] BuildAnchors(
            Transform parent,
            DigRepresentativeBuildingProfileData profile)
        {
            List<DigBuildingAnchor> result = new List<DigBuildingAnchor>();
            for (int index = 0; index < profile.anchors.Length; index++)
            {
                DigRepresentativeBuildingAnchorData? data = profile.anchors[index];
                if (data == null
                    || !data.TryResolveKind(out DigBuildingAnchorKind kind)
                    || string.IsNullOrWhiteSpace(data.stableId))
                {
                    continue;
                }

                GameObject anchorObject = new GameObject($"Anchor {data.stableId.Trim()}");
                anchorObject.transform.SetParent(parent, worldPositionStays: false);
                anchorObject.transform.localPosition = data.position;
                DigBuildingAnchor anchor = anchorObject.AddComponent<DigBuildingAnchor>();
                anchor.Configure(kind, data.stableId);
                result.Add(anchor);
            }

            return result.ToArray();
        }

        private void CreatePart(
            Transform parent,
            string name,
            DigRepresentativeBuildingShape shape,
            TerrainVisualDetailLevel detail,
            Vector3 position,
            Vector3 scale,
            Vector3 rotation)
        {
            GameObject part = new GameObject(name);
            part.transform.SetParent(parent, worldPositionStays: false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.transform.localRotation = Quaternion.Euler(rotation);
            MeshFilter filter = part.AddComponent<MeshFilter>();
            filter.sharedMesh = _meshes.Resolve(shape);
            MeshRenderer renderer = part.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _material;
            DigBuildingDetailGroup group = part.AddComponent<DigBuildingDetailGroup>();
            group.Configure(detail);
            group.SetDetailLevel(TerrainVisualDetailLevel.Full);
        }
    }
}
