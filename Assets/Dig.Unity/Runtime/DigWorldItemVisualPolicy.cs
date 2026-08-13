using System;
using Dig.Domain.Content;
using Dig.Domain.World;
using Dig.Presentation.Inventory;
using UnityEngine;

namespace Dig.Unity
{
    internal static class DigWorldItemVisualPolicy
    {
        internal const float WorldItemFrontDepthOffset = 0.22f;
        private const float CampfireBoxFootprintSide = 0.35355339f;
        private const float CampfireBoxHeight = 0.30f;

        private static readonly Color CampfireBoxTint =
            new Color(0.66f, 0.38f, 0.16f, 1f);

        internal static DigItemVisualResolution Resolve(
            DigItemVisualCatalog? catalog,
            string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                throw new ArgumentException("Item id is required.", nameof(itemId));
            }

            DigItemVisualResolution resolution = catalog != null
                ? catalog.ResolveItem(itemId)
                : CreateFallbackResolution(itemId);
            if (IsBuildingBox(itemId))
            {
                return CreateBoxFamilyResolution(
                    "visual.box.building",
                    CampfireBoxTint,
                    resolution);
            }

            if (string.Equals(itemId, ProductionPackageContent.FoodPackageItemId.ToString(), StringComparison.Ordinal))
            {
                return CreateBoxFamilyResolution("visual.box.food", new Color(0.72f, 0.52f, 0.24f, 1f), resolution);
            }

            if (string.Equals(itemId, ProductionPackageContent.WeaponPackageItemId.ToString(), StringComparison.Ordinal))
            {
                return CreateBoxFamilyResolution("visual.box.weapon", new Color(0.42f, 0.44f, 0.48f, 1f), resolution);
            }

            if (string.Equals(itemId, ProductionPackageContent.ToolPackageItemId.ToString(), StringComparison.Ordinal))
            {
                return CreateBoxFamilyResolution("visual.box.tool", new Color(0.48f, 0.34f, 0.20f, 1f), resolution);
            }

            return DigBasketVisualPolicy.Resolve(itemId, resolution);
        }

        internal static Quaternion ResolveLooseWorldRotation(
            WorldItemViewModel item,
            Quaternion authoredRotation)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            return item.IsBuildingBox
                ? authoredRotation
                : Quaternion.Euler(0f, 0f, 90f) * authoredRotation;
        }

        internal static Vector3 ResolveFloorAnchor(
            CellId cell,
            Vector2 cellOffset)
        {
            return DigTunnelProjection.ResidentWorldPosition(cell.X, cell.Y, cell.Z)
                + new Vector3(
                    cellOffset.x,
                    DigTunnelProjection.ResidentFootSink + 0.02f,
                    cellOffset.y + WorldItemFrontDepthOffset);
        }

        internal static Vector3 ResolveWorldPosition(
            CellId cell,
            DigItemVisualResolution resolution,
            Vector2 cellOffset)
        {
            return ResolveFloorAnchor(cell, cellOffset);
        }

        internal static bool IsCampfireBox(string itemId)
        {
            return string.Equals(
                itemId,
                CampfireBuildingBoxContent.CampfireBoxItemId.ToString(),
                StringComparison.Ordinal);
        }

        internal static bool IsBuildingBox(string itemId)
        {
            return itemId.StartsWith("building_box.", StringComparison.Ordinal);
        }

        internal static bool IsLivingMaterial(string itemId)
        {
            return string.Equals(itemId, "creature.hamster", StringComparison.Ordinal)
                || string.Equals(itemId, "creature.grub", StringComparison.Ordinal)
                || string.Equals(itemId, "creature.larva", StringComparison.Ordinal);
        }

        internal static bool ConsumesCellLayoutSlot(string itemId)
        {
            return !IsLivingMaterial(itemId);
        }

        private static DigItemVisualResolution CreateFallbackResolution(string itemId)
        {
            return new DigItemVisualResolution(
                DigVisualAsset.CreateRuntimeFallback(itemId, Color.magenta),
                icon: null,
                DigItemCarrySocketPolicy.None,
                new Vector3(0.34f, 0.34f, 0.34f),
                new Vector3(0.28f, 0.28f, 0.28f),
                DigItemRotationPolicy.StackQuarterTurns,
                DigItemColliderPolicy.InteractiveOnly,
                maxVisibleInstances: 4,
                hasProfile: false);
        }

        private static DigItemVisualResolution CreateBoxFamilyResolution(
            string visualId,
            Color tint,
            DigItemVisualResolution resolution)
        {
            // A box family is a gameplay visual contract. Never retain an authored
            // per-item asset here: otherwise one catalog entry (for example the
            // stone workshop) silently gets a different shape and scale.
            DigVisualAsset asset = DigVisualAsset.CreateRuntimeFallback(visualId, tint);
            return new DigItemVisualResolution(
                asset,
                resolution.Icon,
                DigItemCarrySocketPolicy.Cargo,
                new Vector3(
                    CampfireBoxFootprintSide,
                    CampfireBoxHeight,
                    CampfireBoxFootprintSide),
                resolution.CarryScale,
                DigItemRotationPolicy.Fixed,
                DigItemColliderPolicy.InteractiveOnly,
                maxVisibleInstances: 1,
                hasProfile: true);
        }
    }
}
