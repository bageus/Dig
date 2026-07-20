using System;
using System.Collections.Generic;
using Dig.Domain.Buildings;
using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    public sealed class DigBuildingPrefabAuthoring : MonoBehaviour
    {
        [SerializeField]
        private Vector2Int footprintSize = Vector2Int.one;

        [SerializeField]
        private Vector2 pivotCell = Vector2.zero;

        [SerializeField]
        private BuildingOrientation authoredOrientation = BuildingOrientation.North;

        [SerializeField]
        private DigBuildingAnchor[] anchors = Array.Empty<DigBuildingAnchor>();

        public Vector2Int FootprintSize => footprintSize;

        public Vector2 PivotCell => pivotCell;

        public BuildingOrientation AuthoredOrientation => authoredOrientation;

        public DigBuildingAnchor[] ResolveAnchors()
        {
            return anchors.Length > 0
                ? anchors
                : GetComponentsInChildren<DigBuildingAnchor>(includeInactive: true);
        }

        internal void ConfigureRuntime(
            Vector2Int configuredFootprintSize,
            Vector2 configuredPivotCell,
            DigBuildingAnchor[] configuredAnchors)
        {
            if (configuredFootprintSize.x <= 0 || configuredFootprintSize.y <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(configuredFootprintSize));
            }

            footprintSize = configuredFootprintSize;
            pivotCell = configuredPivotCell;
            authoredOrientation = BuildingOrientation.North;
            anchors = configuredAnchors
                ?? throw new ArgumentNullException(nameof(configuredAnchors));
        }

        internal void AppendValidation(
            string profileId,
            Vector2Int expectedFootprintSize,
            Vector2 expectedPivotCell,
            DigBuildingAnchorMask requiredAnchors,
            ICollection<string> errors)
        {
            if (footprintSize.x <= 0 || footprintSize.y <= 0)
            {
                errors.Add($"Building prefab '{profileId}' has an invalid footprint size.");
            }
            else if (footprintSize != expectedFootprintSize)
            {
                errors.Add(
                    $"Building prefab '{profileId}' footprint {footprintSize} "
                    + $"does not match profile {expectedFootprintSize}.");
            }

            if ((pivotCell - expectedPivotCell).sqrMagnitude > 0.0001f)
            {
                errors.Add(
                    $"Building prefab '{profileId}' pivot {pivotCell} "
                    + $"does not match profile {expectedPivotCell}.");
            }

            if (authoredOrientation != BuildingOrientation.North)
            {
                errors.Add(
                    $"Building prefab '{profileId}' must be authored facing North.");
            }

            DigVisualPrefabRoot? visualRoot = GetComponent<DigVisualPrefabRoot>();
            if (visualRoot == null)
            {
                errors.Add(
                    $"Building prefab '{profileId}' requires DigVisualPrefabRoot.");
            }
            else
            {
                Collider[] colliders = visualRoot.ResolveSelectionColliders();
                if (colliders.Length == 0)
                {
                    errors.Add(
                        $"Building prefab '{profileId}' has no selection collider.");
                }
                else
                {
                    ValidateColliders(profileId, visualRoot, colliders, errors);
                }
            }

            ValidateAnchors(profileId, requiredAnchors, errors);
        }

        private static void ValidateColliders(
            string profileId,
            DigVisualPrefabRoot visualRoot,
            IReadOnlyList<Collider> colliders,
            ICollection<string> errors)
        {
            for (int index = 0; index < colliders.Count; index++)
            {
                Collider? collider = colliders[index];
                if (collider == null)
                {
                    errors.Add(
                        $"Building prefab '{profileId}' has a null selection collider.");
                    continue;
                }

                if (!collider.transform.IsChildOf(visualRoot.ModelRoot)
                    && collider.transform != visualRoot.ModelRoot)
                {
                    errors.Add(
                        $"Building prefab '{profileId}' selection collider "
                        + $"'{collider.name}' is outside ModelRoot.");
                }
            }
        }

        private void ValidateAnchors(
            string profileId,
            DigBuildingAnchorMask requiredAnchors,
            ICollection<string> errors)
        {
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            DigBuildingAnchorMask available = DigBuildingAnchorMask.None;
            DigBuildingAnchor[] resolved = ResolveAnchors();
            for (int index = 0; index < resolved.Length; index++)
            {
                DigBuildingAnchor? anchor = resolved[index];
                if (anchor == null)
                {
                    errors.Add($"Building prefab '{profileId}' has a null anchor.");
                    continue;
                }

                if (!Enum.IsDefined(typeof(DigBuildingAnchorKind), anchor.Kind))
                {
                    errors.Add(
                        $"Building prefab '{profileId}' has an invalid anchor kind.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(anchor.StableId))
                {
                    errors.Add(
                        $"Building prefab '{profileId}' has an anchor without stable id.");
                }
                else if (!ids.Add(anchor.StableId))
                {
                    errors.Add(
                        $"Building prefab '{profileId}' has duplicate anchor "
                        + $"'{anchor.StableId}'.");
                }

                available |= anchor.Mask;
            }

            DigBuildingAnchorMask missing = requiredAnchors & ~available;
            if (missing != DigBuildingAnchorMask.None)
            {
                errors.Add(
                    $"Building prefab '{profileId}' is missing anchors: {missing}.");
            }
        }
    }
}
