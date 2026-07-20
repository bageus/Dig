using System;
using System.Collections.Generic;
using Dig.Presentation.World;
using UnityEngine;

namespace Dig.Unity
{
    internal enum DigRepresentativeBuildingShape
    {
        Box = 0,
        Pyramid = 1,
        Octahedron = 2,
        Wedge = 3,
    }

    [Serializable]
    internal sealed class DigRepresentativeBuildingPackData
    {
        public DigRepresentativeBuildingProfileData[] profiles =
            Array.Empty<DigRepresentativeBuildingProfileData>();
    }

    [Serializable]
    internal sealed class DigRepresentativeBuildingProfileData
    {
        public string[] stableIds = Array.Empty<string>();
        public string kind = string.Empty;
        public Vector2Int footprintSize = Vector2Int.one;
        public Vector2 pivotCell = Vector2.zero;
        public Color tint = Color.white;
        public int maxRenderers;
        public int maxTriangles;
        public DigRepresentativeBuildingPartData[] parts =
            Array.Empty<DigRepresentativeBuildingPartData>();
        public DigRepresentativeBuildingAnchorData[] anchors =
            Array.Empty<DigRepresentativeBuildingAnchorData>();

        internal bool TryResolveKind(out DigBuildingProfileKind value)
        {
            return Enum.TryParse(kind, ignoreCase: true, out value)
                && Enum.IsDefined(typeof(DigBuildingProfileKind), value);
        }

        internal DigBuildingAnchorMask ResolveAnchorMask()
        {
            DigBuildingAnchorMask result = DigBuildingAnchorMask.None;
            for (int index = 0; index < anchors.Length; index++)
            {
                if (anchors[index] != null
                    && anchors[index].TryResolveKind(out DigBuildingAnchorKind kindValue))
                {
                    result |= (DigBuildingAnchorMask)(1 << (int)kindValue);
                }
            }

            return result;
        }
    }

    [Serializable]
    internal sealed class DigRepresentativeBuildingPartData
    {
        public string name = string.Empty;
        public string shape = string.Empty;
        public string detail = string.Empty;
        public Vector3 position = Vector3.zero;
        public Vector3 scale = Vector3.one;
        public Vector3 rotation = Vector3.zero;

        internal bool TryResolveShape(out DigRepresentativeBuildingShape value)
        {
            return Enum.TryParse(shape, ignoreCase: true, out value)
                && Enum.IsDefined(typeof(DigRepresentativeBuildingShape), value);
        }

        internal bool TryResolveDetail(out TerrainVisualDetailLevel value)
        {
            return Enum.TryParse(detail, ignoreCase: true, out value)
                && Enum.IsDefined(typeof(TerrainVisualDetailLevel), value);
        }
    }

    [Serializable]
    internal sealed class DigRepresentativeBuildingAnchorData
    {
        public string kind = string.Empty;
        public string stableId = string.Empty;
        public Vector3 position = Vector3.zero;

        internal bool TryResolveKind(out DigBuildingAnchorKind value)
        {
            return Enum.TryParse(kind, ignoreCase: true, out value)
                && Enum.IsDefined(typeof(DigBuildingAnchorKind), value);
        }
    }

    internal static class DigRepresentativeBuildingDataValidator
    {
        internal const int HardRendererLimit = 16;
        internal const int HardTriangleLimit = 512;

        internal static IReadOnlyList<string> Validate(
            DigRepresentativeBuildingPackData? pack)
        {
            List<string> errors = new List<string>();
            if (pack == null || pack.profiles == null || pack.profiles.Length == 0)
            {
                errors.Add("Representative building pack has no profiles.");
                return errors;
            }

            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            bool[] kinds = new bool[3];
            for (int index = 0; index < pack.profiles.Length; index++)
            {
                ValidateProfile(pack.profiles[index], index, ids, kinds, errors);
            }

            RequireKind(DigBuildingProfileKind.Campfire, kinds, errors);
            RequireKind(DigBuildingProfileKind.Furnace, kinds, errors);
            RequireKind(DigBuildingProfileKind.Storage, kinds, errors);
            return errors;
        }

        private static void ValidateProfile(
            DigRepresentativeBuildingProfileData? profile,
            int index,
            ISet<string> ids,
            IList<bool> kinds,
            ICollection<string> errors)
        {
            if (profile == null)
            {
                errors.Add($"Representative building profile {index} is null.");
                return;
            }

            if (!profile.TryResolveKind(out DigBuildingProfileKind kind))
            {
                errors.Add($"Representative building profile {index} has invalid kind.");
            }
            else
            {
                kinds[(int)kind] = true;
            }

            ValidateIds(profile, index, ids, errors);
            ValidateBudget(profile, index, errors);
            ValidateParts(profile, index, errors);
            ValidateAnchors(profile, index, errors);
        }

        private static void ValidateIds(
            DigRepresentativeBuildingProfileData profile,
            int index,
            ISet<string> ids,
            ICollection<string> errors)
        {
            if (profile.stableIds == null || profile.stableIds.Length == 0)
            {
                errors.Add($"Representative building profile {index} has no stable ids.");
                return;
            }

            for (int idIndex = 0; idIndex < profile.stableIds.Length; idIndex++)
            {
                string id = profile.stableIds[idIndex] ?? string.Empty;
                if (string.IsNullOrWhiteSpace(id))
                {
                    errors.Add($"Representative building profile {index} has an empty id.");
                }
                else if (!ids.Add(id.Trim()))
                {
                    errors.Add($"Duplicate representative building id '{id.Trim()}'.");
                }
            }
        }

        private static void ValidateBudget(
            DigRepresentativeBuildingProfileData profile,
            int index,
            ICollection<string> errors)
        {
            if (profile.footprintSize.x <= 0 || profile.footprintSize.y <= 0)
            {
                errors.Add($"Representative building profile {index} has invalid footprint.");
            }

            if (profile.maxRenderers <= 0
                || profile.maxRenderers > HardRendererLimit)
            {
                errors.Add($"Representative building profile {index} exceeds renderer budget.");
            }

            if (profile.maxTriangles <= 0
                || profile.maxTriangles > HardTriangleLimit)
            {
                errors.Add($"Representative building profile {index} exceeds triangle budget.");
            }
        }

        private static void ValidateParts(
            DigRepresentativeBuildingProfileData profile,
            int index,
            ICollection<string> errors)
        {
            if (profile.parts == null || profile.parts.Length == 0)
            {
                errors.Add($"Representative building profile {index} has no parts.");
                return;
            }

            int markerCount = 0;
            int triangleCount = 0;
            for (int partIndex = 0; partIndex < profile.parts.Length; partIndex++)
            {
                DigRepresentativeBuildingPartData? part = profile.parts[partIndex];
                if (part == null
                    || !part.TryResolveShape(out DigRepresentativeBuildingShape shape)
                    || !part.TryResolveDetail(out TerrainVisualDetailLevel detail))
                {
                    errors.Add($"Representative building profile {index} has invalid part {partIndex}.");
                    continue;
                }

                markerCount += detail == TerrainVisualDetailLevel.Marker ? 1 : 0;
                triangleCount += DigRepresentativeBuildingMeshFactory.TriangleCount(shape);
            }

            if (markerCount == 0)
            {
                errors.Add($"Representative building profile {index} has no marker silhouette.");
            }

            if (profile.parts.Length > profile.maxRenderers
                || triangleCount > profile.maxTriangles)
            {
                errors.Add($"Representative building profile {index} exceeds authored budget.");
            }
        }

        private static void ValidateAnchors(
            DigRepresentativeBuildingProfileData profile,
            int index,
            ICollection<string> errors)
        {
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            if (profile.anchors == null || profile.anchors.Length == 0)
            {
                errors.Add($"Representative building profile {index} has no anchors.");
                return;
            }

            for (int anchorIndex = 0; anchorIndex < profile.anchors.Length; anchorIndex++)
            {
                DigRepresentativeBuildingAnchorData? anchor = profile.anchors[anchorIndex];
                if (anchor == null
                    || !anchor.TryResolveKind(out _)
                    || string.IsNullOrWhiteSpace(anchor.stableId))
                {
                    errors.Add($"Representative building profile {index} has invalid anchor {anchorIndex}.");
                    continue;
                }

                if (!ids.Add(anchor.stableId.Trim()))
                {
                    errors.Add($"Representative building profile {index} duplicates anchor '{anchor.stableId}'.");
                }
            }
        }

        private static void RequireKind(
            DigBuildingProfileKind kind,
            IReadOnlyList<bool> kinds,
            ICollection<string> errors)
        {
            if (!kinds[(int)kind])
            {
                errors.Add($"Representative building pack requires {kind}.");
            }
        }
    }
}
