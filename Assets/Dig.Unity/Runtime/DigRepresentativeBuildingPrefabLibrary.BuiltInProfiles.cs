using UnityEngine;

namespace Dig.Unity
{
    internal sealed partial class DigRepresentativeBuildingPrefabLibrary
    {
        private static DigRepresentativeBuildingProfileData[] CreateBuiltInProfiles()
        {
            return new[]
            {
                CampfireProfile(),
                FurnaceProfile(),
                StorageProfile(),
                TentProfile(),
                StoneMasonProfile(),
                WoodWorkshopProfile(),
            };
        }

        private static DigRepresentativeBuildingProfileData CampfireProfile()
        {
            return Profile(
                new[] { "kitchen.campfire", "building.campfire" },
                "Campfire",
                Vector2Int.one,
                Vector2.zero,
                new Vector3(0f, 0.41f, 0f),
                new Vector3(0.82f, 0.82f, 0.82f),
                new Color(0.92f, 0.42f, 0.12f, 1f),
                new[]
                {
                    Part("Hearth", "Box", new Vector3(0f, 0.08f, 0f), new Vector3(0.82f, 0.16f, 0.82f)),
                    Part("Flame", "Octahedron", new Vector3(0f, 0.48f, 0f), new Vector3(0.34f, 0.68f, 0.34f)),
                },
                Anchor("Worker", "worker.primary", new Vector3(0f, 0f, -0.62f)));
        }

        private static DigRepresentativeBuildingProfileData FurnaceProfile()
        {
            return Profile(
                new[] { "building.furnace", "building.forge" },
                "Furnace",
                Vector2Int.one,
                Vector2.zero,
                new Vector3(0f, 0.71f, 0f),
                new Vector3(0.82f, 1.42f, 0.76f),
                new Color(0.64f, 0.30f, 0.16f, 1f),
                new[]
                {
                    Part("Body", "Box", new Vector3(0f, 0.42f, 0f), new Vector3(0.82f, 0.84f, 0.76f)),
                    Part("Chimney", "Box", new Vector3(0.22f, 1.06f, 0.12f), new Vector3(0.28f, 0.72f, 0.28f)),
                },
                Anchor("Worker", "worker.primary", new Vector3(0f, 0f, -0.72f)));
        }

        private static DigRepresentativeBuildingProfileData StorageProfile()
        {
            return Profile(
                new[] { "building.arsenal", "building.storage" },
                "Storage",
                new Vector2Int(3, 2),
                new Vector2(1f, 0.5f),
                new Vector3(1f, 0.76f, 0.5f),
                new Vector3(2.82f, 1.52f, 1.82f),
                new Color(0.40f, 0.48f, 0.56f, 1f),
                new[]
                {
                    Part("Foundation", "Box", new Vector3(1f, 0.12f, 0.5f), new Vector3(2.82f, 0.24f, 1.82f)),
                    Part("Rack", "Box", new Vector3(1f, 0.82f, 0.52f), new Vector3(1.9f, 1.40f, 1.52f)),
                },
                Anchor("Storage", "storage.primary", new Vector3(1f, 0.82f, 0.50f)));
        }

        private static DigRepresentativeBuildingProfileData TentProfile()
        {
            return Profile(
                new[] { "building.tent" },
                "Tent",
                Vector2Int.one,
                Vector2.zero,
                new Vector3(0f, 1f, 0f),
                new Vector3(3f, 2f, 2f),
                new Color(0.82f, 0.68f, 0.38f, 1f),
                new[]
                {
                    Part("Tent Groundsheet", "Box", new Vector3(0f, 0.05f, 0f), new Vector3(3f, 0.10f, 2f)),
                    Part("Tent Roof Left", "Wedge", new Vector3(0f, 1.05f, -0.5f), new Vector3(3f, 1.90f, 1f)),
                    Part("Tent Roof Right", "Wedge", new Vector3(0f, 1.05f, 0.5f), new Vector3(3f, 1.90f, 1f), new Vector3(0f, 180f, 0f)),
                    Part("Tent Entrance Flap", "Pyramid", new Vector3(0f, 0.65f, -0.98f), new Vector3(0.90f, 1.30f, 0.04f)),
                    Part("Tent Ridge Pole", "Box", new Vector3(0f, 1.96f, 0f), new Vector3(3f, 0.08f, 0.08f), detail: "Full"),
                },
                Anchor("Worker", "worker.primary", new Vector3(0f, 0f, -1.15f)),
                Anchor("Visitor", "bed.left", new Vector3(-0.65f, 0f, 0f)),
                Anchor("Visitor", "bed.right", new Vector3(0.65f, 0f, 0f)));
        }

        private static DigRepresentativeBuildingProfileData StoneMasonProfile()
        {
            return Profile(
                new[] { "building.stone_mason" },
                "StoneMason",
                Vector2Int.one,
                Vector2.zero,
                new Vector3(0f, 1.25f, 0f),
                new Vector3(3.5f, 2.5f, 2.5f),
                new Color(0.56f, 0.54f, 0.50f, 1f),
                new[]
                {
                    Part("Stone Foundation", "Box", new Vector3(0f, 0.10f, 0f), new Vector3(3.5f, 0.20f, 2.5f)),
                    Part("Masonry Hall", "Box", new Vector3(0f, 0.85f, 0.15f), new Vector3(2.9f, 1.50f, 2f)),
                    Part("Mason Roof", "Pyramid", new Vector3(0f, 1.95f, 0f), new Vector3(3.2f, 1.10f, 2.3f)),
                    Part("Stone Workbench", "Box", new Vector3(0f, 0.65f, -1.05f), new Vector3(2f, 0.50f, 0.35f)),
                    Part("Cut Stone Left", "Box", new Vector3(-1.25f, 0.40f, 0.75f), new Vector3(0.55f, 0.60f, 0.55f), detail: "Reduced"),
                    Part("Cut Stone Right", "Box", new Vector3(1.15f, 0.30f, 0.85f), new Vector3(0.70f, 0.40f, 0.50f), detail: "Full"),
                },
                Anchor("Worker", "worker.primary", new Vector3(0f, 0f, -1.35f)),
                Anchor("Input", "input.primary", new Vector3(-1.25f, 0.25f, 0.85f)),
                Anchor("Output", "output.primary", new Vector3(1.20f, 0.25f, 0.85f)));
        }

        private static DigRepresentativeBuildingProfileData WoodWorkshopProfile()
        {
            return Profile(
                new[] { "building.wood_workshop" },
                "WoodWorkshop",
                Vector2Int.one,
                Vector2.zero,
                new Vector3(0f, 1f, 0f),
                new Vector3(2.5f, 2f, 2f),
                new Color(0.55f, 0.34f, 0.16f, 1f),
                new[]
                {
                    Part("Wood Foundation", "Box", new Vector3(0f, 0.08f, 0f), new Vector3(2.5f, 0.16f, 2f)),
                    Part("Wood Roof Left", "Wedge", new Vector3(0f, 1.60f, -0.5f), new Vector3(2.5f, 0.80f, 1f)),
                    Part("Wood Roof Right", "Wedge", new Vector3(0f, 1.60f, 0.5f), new Vector3(2.5f, 0.80f, 1f), new Vector3(0f, 180f, 0f)),
                    Part("Frame Left", "Box", new Vector3(-1.05f, 0.70f, 0f), new Vector3(0.16f, 1.20f, 0.16f)),
                    Part("Frame Right", "Box", new Vector3(1.05f, 0.70f, 0f), new Vector3(0.16f, 1.20f, 0.16f)),
                    Part("Saw Bench", "Box", new Vector3(0f, 0.55f, -0.70f), new Vector3(1.55f, 0.25f, 0.45f)),
                    Part("Timber Log", "Box", new Vector3(0f, 0.82f, -0.70f), new Vector3(1.85f, 0.22f, 0.22f), new Vector3(0f, 0f, 5f), "Full"),
                },
                Anchor("Worker", "worker.primary", new Vector3(0f, 0f, -1.10f)),
                Anchor("Input", "input.primary", new Vector3(-0.85f, 0.20f, 0.65f)),
                Anchor("Output", "output.primary", new Vector3(0.85f, 0.20f, 0.65f)));
        }
    }
}
