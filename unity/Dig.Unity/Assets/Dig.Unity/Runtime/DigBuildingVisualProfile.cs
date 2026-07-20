using System;
using System.Collections.Generic;
using Dig.Presentation.Buildings;
using UnityEngine;

namespace Dig.Unity
{
    public enum DigBuildingProfileKind
    {
        Campfire = 0,
        Furnace = 1,
        Storage = 2,
    }

    internal readonly struct DigBuildingVisualResolution
    {
        internal DigBuildingVisualResolution(
            DigVisualAsset asset,
            Vector2Int expectedFootprintSize,
            Vector2 pivotCell,
            bool hasProfile)
        {
            Asset = asset;
            ExpectedFootprintSize = expectedFootprintSize;
            PivotCell = pivotCell;
            HasProfile = hasProfile;
        }

        internal DigVisualAsset Asset { get; }

        internal Vector2Int ExpectedFootprintSize { get; }

        internal Vector2 PivotCell { get; }

        internal bool HasProfile { get; }
    }

    [Serializable]
    public sealed class DigBuildingVisualProfile
    {
        [SerializeField]
        private string stableId = string.Empty;

        [SerializeField]
        private DigBuildingProfileKind kind;

        [SerializeField]
        private Vector2Int footprintSize = Vector2Int.one;

        [SerializeField]
        private Vector2 pivotCell = Vector2.zero;

        [SerializeField]
        private DigBuildingAnchorMask requiredAnchors = DigBuildingAnchorMask.Worker;

        [SerializeField]
        private GameObject? buildingBoxPrefab;

        [SerializeField]
        private GameObject? assemblyPrefab;

        [SerializeField]
        private GameObject? completedPrefab;

        [SerializeField]
        private GameObject? damagedPrefab;

        [SerializeField]
        private GameObject? packingPrefab;

        public string StableId => stableId;

        public DigBuildingProfileKind Kind => kind;

        public Vector2Int FootprintSize => footprintSize;

        public Vector2 PivotCell => pivotCell;

        public DigBuildingAnchorMask RequiredAnchors => requiredAnchors;

        internal DigBuildingVisualResolution Resolve(
            BuildingVisualState state,
            DigVisualAsset fallback)
        {
            GameObject? prefab = ResolvePrefab(state);
            DigVisualAsset asset = prefab == null
                ? fallback
                : new DigVisualAsset(
                    $"{stableId}:{state}",
                    prefab,
                    material: null,
                    Color.white,
                    isFallback: false);
            return new DigBuildingVisualResolution(
                asset,
                footprintSize,
                pivotCell,
                hasProfile: true);
        }

        internal void AppendValidation(int index, ICollection<string> errors)
        {
            if (string.IsNullOrWhiteSpace(stableId))
            {
                errors.Add($"Building profile {index} has no stable id.");
            }

            if (!Enum.IsDefined(typeof(DigBuildingProfileKind), kind))
            {
                errors.Add($"Building profile {index} has an invalid kind.");
            }

            if (footprintSize.x <= 0 || footprintSize.y <= 0)
            {
                errors.Add($"Building profile '{stableId}' has an invalid footprint.");
            }

            ValidateStage(BuildingVisualState.BuildingBox, buildingBoxPrefab, errors);
            ValidateStage(BuildingVisualState.Assembly, assemblyPrefab, errors);
            ValidateStage(BuildingVisualState.Completed, completedPrefab, errors);
            ValidateStage(BuildingVisualState.Damaged, damagedPrefab, errors);
            ValidateStage(BuildingVisualState.Packing, packingPrefab, errors);
        }

        private GameObject? ResolvePrefab(BuildingVisualState state)
        {
            GameObject? exact = state switch
            {
                BuildingVisualState.BuildingBox => buildingBoxPrefab,
                BuildingVisualState.Assembly => assemblyPrefab,
                BuildingVisualState.Completed => completedPrefab,
                BuildingVisualState.Damaged => damagedPrefab,
                BuildingVisualState.Packing => packingPrefab,
                _ => null,
            };
            return exact
                ?? completedPrefab
                ?? assemblyPrefab
                ?? buildingBoxPrefab
                ?? damagedPrefab
                ?? packingPrefab;
        }

        private void ValidateStage(
            BuildingVisualState state,
            GameObject? prefab,
            ICollection<string> errors)
        {
            if (prefab == null)
            {
                errors.Add($"Building profile '{stableId}' has no {state} prefab.");
                return;
            }

            DigVisualPrefabRoot? visualRoot = prefab.GetComponent<DigVisualPrefabRoot>();
            if (visualRoot == null)
            {
                errors.Add(
                    $"Building profile '{stableId}' {state} prefab "
                    + "requires DigVisualPrefabRoot.");
                return;
            }

            DigBuildingPrefabAuthoring? authoring =
                prefab.GetComponent<DigBuildingPrefabAuthoring>();
            if (authoring == null)
            {
                errors.Add(
                    $"Building profile '{stableId}' {state} prefab "
                    + "requires DigBuildingPrefabAuthoring.");
                return;
            }

            authoring.AppendValidation(
                $"{stableId}:{state}",
                footprintSize,
                pivotCell,
                requiredAnchors,
                errors);
        }
    }
}
