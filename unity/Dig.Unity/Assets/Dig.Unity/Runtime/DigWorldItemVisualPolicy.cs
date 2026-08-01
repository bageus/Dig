using System;
using Dig.Domain.Content;
using Dig.Domain.World;
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
            if (IsCampfireBox(itemId))
            {
                return CreateCampfireBoxResolution(itemId, resolution);
            }

            return DigBasketVisualPolicy.Resolve(itemId, resolution);
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

        private static DigItemVisualResolution CreateCampfireBoxResolution(
            string itemId,
            DigItemVisualResolution resolution)
        {
            DigVisualAsset asset = resolution.Asset.IsFallback
                ? DigVisualAsset.CreateRuntimeFallback(itemId, CampfireBoxTint)
                : resolution.Asset;
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
