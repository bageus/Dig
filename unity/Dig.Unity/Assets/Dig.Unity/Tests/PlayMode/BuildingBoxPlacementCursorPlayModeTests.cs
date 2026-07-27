using System.Collections;
using System.Reflection;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.World;
using Dig.Presentation.Buildings;
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
                BuildingBoxPlacementKind.RelocateBox);
            BuildingBoxGhostViewModel second = new BuildingBoxGhostViewModel(
                stackId,
                definitionId,
                new CellId(5, 3, 1),
                BuildingOrientation.North,
                new[] { new CellId(5, 3, 1) },
                new CellId(5, 2, 1),
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
            AssertAllChildrenNonBlocking(preview.gameObject);

            Object.DestroyImmediate(host);
        }

        private static void AssertAllChildrenNonBlocking(GameObject root)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                Assert.AreEqual(2, child.gameObject.layer);
            }

            foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            {
                Assert.False(collider.enabled);
            }
        }

        private static T GetField<T>(object target, string name)
        {
            return (T)target.GetType().GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(target)!;
        }
    }
}
