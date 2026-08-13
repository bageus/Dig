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
                new Vector3(0f, 0.75f, 0f),
                new Vector3(1.5f, 1.5f, 1.5f),
                new Color(0.82f, 0.68f, 0.38f, 1f),
                new[]
                {
                    Part("Tent Groundsheet", "Box", new Vector3(0f, 0.04f, 0f), new Vector3(1.5f, 0.08f, 1.5f)),
                    Part("Tent Roof Left", "Wedge", new Vector3(0f, 0.78f, -0.375f), new Vector3(1.5f, 1.42f, 0.75f)),
                    Part("Tent Roof Right", "Wedge", new Vector3(0f, 0.78f, 0.375f), new Vector3(1.5f, 1.42f, 0.75f), new Vector3(0f, 180f, 0f)),
                    Part("Tent Entrance Flap", "Pyramid", new Vector3(0f, 0.49f, -0.73f), new Vector3(0.68f, 0.98f, 0.04f)),
                    Part("Tent Ridge Pole", "Box", new Vector3(0f, 1.47f, 0f), new Vector3(1.5f, 0.06f, 0.06f), detail: "Full"),
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
                new Vector3(0f, 0.75f, 0f),
                new Vector3(2f, 1.5f, 1.5f),
                new Color(0.56f, 0.54f, 0.50f, 1f),
                new[]
                {
                    Part("Stone Foundation", "Box", new Vector3(0f, 0.06f, 0f), new Vector3(2f, 0.12f, 1.5f)),
                    Part("Masonry Hall", "Box", new Vector3(0f, 0.52f, 0.08f), new Vector3(1.66f, 0.90f, 1.2f)),
                    Part("Mason Roof", "Pyramid", new Vector3(0f, 1.17f, 0f), new Vector3(1.83f, 0.66f, 1.38f)),
                    Part("Stone Workbench", "Box", new Vector3(0f, 0.39f, -0.63f), new Vector3(1.14f, 0.30f, 0.21f)),
                    Part("Cut Stone Left", "Box", new Vector3(-0.71f, 0.24f, 0.45f), new Vector3(0.31f, 0.36f, 0.33f), detail: "Reduced"),
                    Part("Cut Stone Right", "Box", new Vector3(0.66f, 0.18f, 0.51f), new Vector3(0.40f, 0.24f, 0.30f), detail: "Full"),
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
                new Vector3(0f, 0.75f, 0f),
                new Vector3(2f, 1.5f, 1.5f),
                new Color(0.55f, 0.34f, 0.16f, 1f),
                new[]
                {
                    Part("Wood Foundation", "Box", new Vector3(0f, 0.06f, 0f), new Vector3(2f, 0.12f, 1.5f)),
                    Part("Wood Roof Left", "Wedge", new Vector3(0f, 1.20f, -0.375f), new Vector3(2f, 0.60f, 0.75f)),
                    Part("Wood Roof Right", "Wedge", new Vector3(0f, 1.20f, 0.375f), new Vector3(2f, 0.60f, 0.75f), new Vector3(0f, 180f, 0f)),
                    Part("Frame Left", "Box", new Vector3(-0.84f, 0.52f, 0f), new Vector3(0.13f, 0.90f, 0.12f)),
                    Part("Frame Right", "Box", new Vector3(0.84f, 0.52f, 0f), new Vector3(0.13f, 0.90f, 0.12f)),
                    Part("Saw Bench", "Box", new Vector3(0f, 0.41f, -0.52f), new Vector3(1.24f, 0.19f, 0.34f)),
                    Part("Timber Log", "Box", new Vector3(0f, 0.62f, -0.52f), new Vector3(1.48f, 0.16f, 0.16f), new Vector3(0f, 0f, 5f), "Full"),
                },
                Anchor("Worker", "worker.primary", new Vector3(0f, 0f, -1.10f)),
                Anchor("Input", "input.primary", new Vector3(-0.85f, 0.20f, 0.65f)),
                Anchor("Output", "output.primary", new Vector3(0.85f, 0.20f, 0.65f)));
        }
    }
}
