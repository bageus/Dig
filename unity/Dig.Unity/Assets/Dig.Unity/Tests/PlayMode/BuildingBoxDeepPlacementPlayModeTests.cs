using System.Collections;
using System.Reflection;
using Dig.Domain.Buildings;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Dig.Presentation.Buildings;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dig.Unity.Tests
{
    public sealed class BuildingBoxDeepPlacementPlayModeTests
    {
        [UnityTest]
        public IEnumerator Campfire_building_ghost_is_visible_on_z1_through_z3()
        {
            GameObject host = new GameObject("Deep BuildingBox ghost host");
            DigBuildingBoxGhostRenderer renderer =
                host.AddComponent<DigBuildingBoxGhostRenderer>();
            renderer.SetVisualCatalog(null);
            renderer.SetItemVisualCatalog(null);
            BuildingBoxPlacementPresenter presenter =
                new BuildingBoxPlacementPresenter(new BuildingPlacementValidator());
            ItemDefinition item = CampfireBuildingBoxContent.Definition.BoxItem;
            EntityId stackId = EntityId.Parse(
                "00000000-0000-0000-0000-000000000104");
            ItemStackSnapshot stack = new ItemStackSnapshot(
                stackId,
                item.Id,
                quantity: 1,
                ItemLocation.InWorld(new CellId(1, 1, 0)),
                System.Array.Empty<ItemQuantityReservationSnapshot>());

            for (int depth = 1; depth <= 3; depth++)
            {
                CellId origin = new CellId(3, 3, depth);
                CellId work = new CellId(3, 2, depth);
                BuildingBoxGhostViewModel preview = presenter.Preview(
                    stack,
                    item,
                    CampfireBuildingBoxContent.Definition.Building,
                    origin,
                    BuildingOrientation.North,
                    CreateSupportedWorld(origin, work),
                    System.Array.Empty<CellId>(),
                    new[] { work });
                Assert.IsTrue(preview.IsVisible, preview.ReasonCode);
                Assert.IsTrue(preview.IsValid, preview.ReasonCode);
                Assert.AreEqual(
                    BuildingBoxPlacementKind.AssembleBuilding,
                    preview.PlacementKind);

                renderer.Render(preview);
                yield return null;
                Transform previewRoot = GetField<Transform>(
                    renderer,
                    "_previewContainer");
                Assert.IsTrue(previewRoot.gameObject.activeSelf);
                Assert.AreEqual(origin.X, previewRoot.localPosition.x, 0.001f);
            }

            Object.DestroyImmediate(host);
        }

        private static WorldSnapshot CreateSupportedWorld(
            CellId origin,
            CellId work)
        {
            MaterialId rock = new MaterialId("playmode.rock");
            MaterialId air = new MaterialId("playmode.air");
            MaterialCatalog materials = new MaterialCatalog(new[]
            {
                new MaterialDefinition(rock, isSolid: true, hardness: 100),
                new MaterialDefinition(air, isSolid: false, hardness: 0),
            });
            Result<WorldState> created = WorldState.CreateFilled(
                new WorldSize(8, 8),
                chunkSize: 4,
                materials,
                rock,
                explored: true);
            Assert.IsTrue(created.IsSuccess, created.Error?.ToString());
            WorldState world = created.Value;
            Assert.IsTrue(world.Excavate(origin, air, tick: 1).IsSuccess);
            Assert.IsTrue(world.Excavate(work, air, tick: 2).IsSuccess);
            return world.CreateSnapshot();
        }

        private static T GetField<T>(object target, string name)
        {
            return (T)target.GetType().GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(target)!;
        }
    }
}
