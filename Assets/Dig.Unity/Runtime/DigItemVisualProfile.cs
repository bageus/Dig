using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dig.Unity
{
    public enum DigItemProfileKind
    {
        Material = 0,
        Ore = 1,
        BuildingBox = 2,
        Food = 3,
        Alcohol = 4,
        Equipment = 5,
    }

    public enum DigItemCarrySocketPolicy
    {
        None = 0,
        Hand = 1,
        Cargo = 2,
        Weapon = 3,
        Back = 4,
    }

    public enum DigItemRotationPolicy
    {
        Fixed = 0,
        StackQuarterTurns = 1,
        LeanByQuantityBand = 2,
    }

    public enum DigItemColliderPolicy
    {
        None = 0,
        InteractiveOnly = 1,
    }

    internal readonly struct DigItemVisualResolution
    {
        internal DigItemVisualResolution(
            DigVisualAsset asset,
            Sprite? icon,
            DigItemCarrySocketPolicy carrySocket,
            Vector3 worldScale,
            Vector3 carryScale,
            DigItemRotationPolicy rotationPolicy,
            DigItemColliderPolicy colliderPolicy,
            int maxVisibleInstances,
            bool hasProfile)
        {
            Asset = asset;
            Icon = icon;
            CarrySocket = carrySocket;
            WorldScale = worldScale;
            CarryScale = carryScale;
            RotationPolicy = rotationPolicy;
            ColliderPolicy = colliderPolicy;
            MaxVisibleInstances = maxVisibleInstances;
            HasProfile = hasProfile;
        }

        internal DigVisualAsset Asset { get; }
        internal Sprite? Icon { get; }
        internal DigItemCarrySocketPolicy CarrySocket { get; }
        internal Vector3 WorldScale { get; }
        internal Vector3 CarryScale { get; }
        internal DigItemRotationPolicy RotationPolicy { get; }
        internal DigItemColliderPolicy ColliderPolicy { get; }
        internal int MaxVisibleInstances { get; }
        internal bool HasProfile { get; }
    }

    [Serializable]
    public sealed class DigItemVisualProfile
    {
        [SerializeField]
        private string stableId = string.Empty;

        [SerializeField]
        private DigItemProfileKind kind;

        [SerializeField]
        private GameObject? prefab;

        [SerializeField]
        private Sprite? icon;

        [SerializeField]
        private Material? material;

        [SerializeField]
        private Color tint = Color.white;

        [SerializeField]
        private DigItemCarrySocketPolicy carrySocket;

        [SerializeField]
        private Vector3 worldScale = new Vector3(0.34f, 0.34f, 0.34f);

        [SerializeField]
        private Vector3 carryScale = new Vector3(0.28f, 0.28f, 0.28f);

        [SerializeField]
        private DigItemRotationPolicy rotationPolicy =
            DigItemRotationPolicy.StackQuarterTurns;

        [SerializeField]
        private DigItemColliderPolicy colliderPolicy =
            DigItemColliderPolicy.InteractiveOnly;

        [SerializeField]
        [Range(1, 4)]
        private int maxVisibleInstances = 4;

        public string StableId => stableId;
        public DigItemProfileKind Kind => kind;

        internal DigItemVisualResolution Resolve(DigVisualAsset fallback)
        {
            DigVisualAsset asset = prefab == null && material == null
                ? fallback
                : new DigVisualAsset(
                    stableId,
                    prefab,
                    material,
                    tint,
                    isFallback: false);
            return new DigItemVisualResolution(
                asset,
                icon,
                carrySocket,
                worldScale,
                carryScale,
                rotationPolicy,
                colliderPolicy,
                maxVisibleInstances,
                hasProfile: true);
        }

        internal void AppendValidation(int index, ICollection<string> errors)
        {
            if (string.IsNullOrWhiteSpace(stableId))
            {
                errors.Add($"Item profile {index} has no stable id.");
            }

            if (!Enum.IsDefined(typeof(DigItemProfileKind), kind)
                || !Enum.IsDefined(typeof(DigItemCarrySocketPolicy), carrySocket)
                || !Enum.IsDefined(typeof(DigItemRotationPolicy), rotationPolicy)
                || !Enum.IsDefined(typeof(DigItemColliderPolicy), colliderPolicy))
            {
                errors.Add($"Item profile '{stableId}' has invalid policy metadata.");
            }

            if (maxVisibleInstances < 1 || maxVisibleInstances > 4)
            {
                errors.Add($"Item profile '{stableId}' must allow one to four instances.");
            }

            if (!IsPositive(worldScale) || !IsPositive(carryScale))
            {
                errors.Add($"Item profile '{stableId}' has invalid scale metadata.");
            }

            if (prefab != null && prefab.GetComponent<DigVisualPrefabRoot>() == null)
            {
                errors.Add($"Item profile '{stableId}' prefab requires DigVisualPrefabRoot.");
            }

            if (prefab == null && material == null)
            {
                errors.Add($"Item profile '{stableId}' has neither prefab nor material.");
            }
        }

        private static bool IsPositive(Vector3 value)
        {
            return value.x > 0f && value.y > 0f && value.z > 0f;
        }
    }
}
