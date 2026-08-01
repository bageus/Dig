using System.Collections;
using System.Linq;
using System.Reflection;
using Dig.Domain.Buildings;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Dig.Presentation.Buildings;
using Dig.Presentation.Inventory;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dig.Unity.Tests
{
    public sealed class BuildingBoxPlacementCursorPlayModeTests
    {
        [UnityTest]
        public IEnumerator Ghost_moves_between_pointer_cells_and_stays_non_blocking()
        {
            GameObject host = new GameObject("BuildingBox placement cursor host");
            DigBuildingBoxGhostRenderer renderer =
                host.AddComponent<DigBuildingBoxGhostRenderer>();
            renderer.SetVisualCatalog(null);
            renderer.SetItemVisualCatalog(null);
            EntityId stackId = EntityId.Parse(
                "00000000-0000-0000-0000-000000000101");
            BuildingDefinitionId definitionId = new BuildingDefinitionId(
                "demo.workshop.box");
            BuildingBoxGhostViewModel first = new BuildingBoxGhostViewModel(
                stackId,
                definitionId,
                new CellId(2, 2, 0),
                BuildingOrientation.North,
                new[] { new CellId(2, 2, 0) },
                new CellId(2, 2, 0),
                isValid: true,
                reasonCode: null,
                BuildingBoxPlacementKind.RelocateBox,
                sourceItemId: CampfireBuildingBoxContent.CampfireBoxItemId);
            BuildingBoxGhostViewModel second = new BuildingBoxGhostViewModel(
                stackId,
                definitionId,
                new CellId(5, 3, 2),
                BuildingOrientation.North,
                new[] { new CellId(5, 3, 2) },
                new CellId(5, 2, 2),
                isValid: true,
                reasonCode: null,
                BuildingBoxPlacementKind.AssembleBuilding);

            renderer.Render(first);
            yield return null;
            Transform preview = GetField<Transform>(renderer, "_previewContainer");
            Vector3 firstPosition = preview.localPosition;
            Assert.AreEqual(2f, firstPosition.x, 0.001f);
            AssertAllChildrenNonBlocking(preview.gameObject);

            renderer.Render(second);
            yield return null;
            Vector3 secondPosition = preview.localPosition;
            Assert.AreNotEqual(firstPosition, secondPosition);
            Assert.AreEqual(5f, secondPosition.x, 0.001f);
            Assert.AreNotEqual(firstPosition.y, secondPosition.y);
            Assert.AreNotEqual(firstPosition.z, secondPosition.z);
            Assert.IsTrue(preview.gameObject.activeSelf);
            AssertAllChildrenNonBlocking(preview.gameObject);

            Object.DestroyImmediate(host);
        }

        [UnityTest]
        public IEnumerator Relocation_preview_and_plan_match_actual_box_size()
        {
            GameObject actualHost = new GameObject("Actual BuildingBox host");
            DigWorldItemRenderer actual = actualHost.AddComponent<DigWorldItemRenderer>();
            actual.SetVisualCatalog(null);
            string stackId = "00000000-0000-0000-0000-000000000102";
            string itemId = CampfireBuildingBoxContent.CampfireBoxItemId.ToString();
            actual.Render(new[]
            {
                new WorldItemViewModel(
                    stackId,
                    itemId,
                    quantity: 1,
                    reservedQuantity: 0,
                    cellX: 2,
                    cellY: 2,
                    cellZ: 0,
                    ItemInteractionProfiles.BuildingBox),
            });

            GameObject ghostHost = new GameObject("BuildingBox ghost host");
            DigBuildingBoxGhostRenderer ghost =
                ghostHost.AddComponent<DigBuildingBoxGhostRenderer>();
            ghost.SetVisualCatalog(null);
            ghost.SetItemVisualCatalog(null);
            EntityId parsedStack = EntityId.Parse(stackId);
            BuildingBoxGhostViewModel preview = new BuildingBoxGhostViewModel(
                parsedStack,
                new BuildingDefinitionId("demo.workshop.box"),
                new CellId(4, 2, 0),
                BuildingOrientation.North,
                new[] { new CellId(4, 2, 0) },
                new CellId(4, 2, 0),
                isValid: true,
                reasonCode: null,
                BuildingBoxPlacementKind.RelocateBox,
                sourceItemId: CampfireBuildingBoxContent.CampfireBoxItemId);
            ghost.Render(preview);
            EntityId relocationJobId = EntityId.Parse(
                "00000000-0000-0000-0000-000000000103");
            Invoke(
                ghost,
                "RenderPlans",
                new[]
                {
                    new BuildingBoxRelocationPlanViewModel(
                        relocationJobId,
                        parsedStack,
                        CampfireBuildingBoxContent.CampfireBoxItemId,
                        new CellId(6, 2, 0)),
                });
            yield return null;

            DigWorldItemVisual actualVisual =
                actual.GetComponentInChildren<DigWorldItemVisual>(true);
            GameObject previewInstance = GetField<GameObject>(ghost, "_previewInstance");
            Transform planned = ghost.GetComponentsInChildren<Transform>(true)
                .First(value => value.name == $"Planned BuildingBox {relocationJobId}");
            Vector3 actualSize = CombinedRendererBounds(actualVisual.gameObject).size;
            Vector3 previewSize = CombinedRendererBounds(previewInstance).size;
            Vector3 plannedSize = CombinedRendererBounds(planned.gameObject).size;

            AssertVectorApproximately(actualSize, previewSize);
            AssertVectorApproximately(actualSize, plannedSize);
            Assert.IsTrue(planned.gameObject.activeInHierarchy);
            AssertAllChildrenNonBlocking(previewInstance);
            AssertAllChildrenNonBlocking(planned.gameObject);

            Object.DestroyImmediate(actualHost);
            Object.DestroyImmediate(ghostHost);
        }

        [UnityTest]
        public IEnumerator Placement_target_resolves_enabled_movement_surface_on_deeper_layer()
        {
            GameObject cameraHost = new GameObject("Placement target camera");
            Camera camera = cameraHost.AddComponent<Camera>();
            camera.transform.position = new Vector3(4f, 4f, -10f);
            camera.transform.LookAt(new Vector3(4f, -3f, 0f));
            camera.fieldOfView = 45f;

            GameObject tunnelHost = new GameObject("Placement target tunnel");
            DigTunnelDemoRenderer tunnel = tunnelHost.AddComponent<DigTunnelDemoRenderer>();
            Invoke(tunnel, "Initialize", TunnelNavigationVolume.CreateDemo(12, 12));
            Physics.SyncTransforms();

            GameObject interactionHost = new GameObject("Placement target interaction");
            DigWorldInteraction interaction = interactionHost.AddComponent<DigWorldInteraction>();
            SetField(interaction, "_camera", camera);
            SetField(interaction, "_tunnelRenderer", tunnel);
            CellId expected = new CellId(3, 2, 2);
            Vector3 expectedWorld = new Vector3(3f, -2f, -0.69f);
            Vector3 screen = camera.WorldToScreenPoint(expectedWorld);
            Ray ray = camera.ScreenPointToRay(screen);
            RaycastHit[] hits = Physics.RaycastAll(ray, 50f);

            object?[] arguments =
            {
                hits,
                new Vector2(screen.x, screen.y),
                default(CellId),
            };
            bool resolved = (bool)GetMethod(
                interaction,
                "TryResolveBuildingPlacementMovementSurface",
                parameterCount: 3).Invoke(interaction, arguments)!;

            Assert.IsTrue(resolved);
            Assert.AreEqual(expected, (CellId)arguments[2]!);
            yield return null;

            Object.DestroyImmediate(interactionHost);
            Object.DestroyImmediate(tunnelHost);
            Object.DestroyImmediate(cameraHost);
        }

        private static Bounds CombinedRendererBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            Assert.IsNotEmpty(renderers);
            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return bounds;
        }

        private static void AssertVectorApproximately(Vector3 expected, Vector3 actual)
        {
            Assert.AreEqual(expected.x, actual.x, 0.001f);
            Assert.AreEqual(expected.y, actual.y, 0.001f);
            Assert.AreEqual(expected.z, actual.z, 0.001f);
        }

        private static void AssertAllChildrenNonBlocking(GameObject root)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child != root.transform)
                {
                    Assert.AreEqual(2, child.gameObject.layer);
                }
            }

            foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            {
                Assert.False(collider.enabled);
            }
        }

        private static void Invoke(object target, string name, object value)
        {
            GetMethod(target, name, parameterCount: 1).Invoke(target, new[] { value });
        }

        private static MethodInfo GetMethod(
            object target,
            string name,
            int parameterCount)
        {
            return target.GetType().GetMethods(
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .Single(method => method.Name == name
                    && method.GetParameters().Length == parameterCount);
        }

        private static T GetField<T>(object target, string name)
        {
            return (T)target.GetType().GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(target)!;
        }

        private static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(target, value);
        }
    }
}
