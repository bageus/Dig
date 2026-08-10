using System;
using System.Collections;
using System.Reflection;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Presentation.Buildings;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dig.Unity.Tests
{
    public sealed class BuildingBoxAssemblyProgressPlayModeTests
    {
        [UnityTest]
        public IEnumerator Assembly_visual_exposes_zero_three_progress_and_completed_states()
        {
            GameObject host = new GameObject("BuildingBox five-stage assembly visual");
            DigBuildingVisual visual = host.AddComponent<DigBuildingVisual>();
            Transform container = new GameObject("Model").transform;
            container.SetParent(host.transform, worldPositionStays: false);
            SetField(visual, "_modelContainer", container);

            BuildingStatus[] statuses =
            {
                BuildingStatus.ReadyToBuild,
                BuildingStatus.UnderConstruction,
                BuildingStatus.UnderConstruction,
                BuildingStatus.ReadyToComplete,
                BuildingStatus.Completed,
            };
            int[] completedWork = { 0, 1, 2, 3, 3 };
            float[] heights = new float[statuses.Length];

            for (int index = 0; index < statuses.Length; index++)
            {
                BuildingWorldViewModel model = CreateModel(
                    statuses[index],
                    completedWork[index],
                    version: index + 1);
                SetModel(visual, model);
                Invoke(visual, "ApplyPresentation");
                heights[index] = container.localScale.y;

                Assert.AreEqual(
                    index < statuses.Length - 1
                        ? BuildingVisualState.Assembly
                        : BuildingVisualState.Completed,
                    model.VisualState);
                yield return null;
            }

            Assert.Less(heights[0], heights[1]);
            Assert.Less(heights[1], heights[2]);
            Assert.Less(heights[2], heights[3]);
            Assert.AreEqual(1f, heights[3], 0.001f);
            Assert.AreEqual(1f, heights[4], 0.001f);
            Assert.That(host.name, Does.Contain("Completed"));

            UnityEngine.Object.DestroyImmediate(host);
        }

        private static BuildingWorldViewModel CreateModel(
            BuildingStatus status,
            int completedWork,
            long version)
        {
            EntityId buildingId = EntityId.Parse(
                "00000000-0000-0000-0000-000000000201");
            BuildingDefinitionId definitionId = new BuildingDefinitionId(
                "test.building.box");
            BuildingFunctionsViewModel functions = new BuildingFunctionsViewModel(
                buildingId,
                definitionId,
                status,
                durability: status == BuildingStatus.Completed ? 100 : 0,
                maximumDurability: 100,
                isPacking: false,
                packingCompletedWork: 0,
                packingRequiredWork: 0,
                actions: Array.Empty<BuildingFunctionActionViewModel>());
            return new BuildingWorldViewModel(
                id: buildingId.ToString(),
                definitionId: definitionId.ToString(),
                name: "Test Building",
                originX: 3,
                originY: 3,
                originZ: 1,
                orientation: BuildingOrientation.North,
                workPositionX: 3,
                workPositionY: 2,
                workPositionZ: 1,
                status: status,
                completedWork: completedWork,
                requiredWork: 3,
                version: version,
                footprint: new[] { new BuildingFootprintCellViewModel(3, 3, 1) },
                functions: functions);
        }

        private static void SetModel(DigBuildingVisual visual, BuildingWorldViewModel model)
        {
            PropertyInfo property = typeof(DigBuildingVisual).GetProperty(
                nameof(DigBuildingVisual.Model),
                BindingFlags.Instance | BindingFlags.Public)!;
            property.GetSetMethod(nonPublic: true)!.Invoke(visual, new object[] { model });
        }

        private static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(target, value);
        }

        private static void Invoke(object target, string name)
        {
            target.GetType().GetMethod(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(
                    target,
                    Array.Empty<object>());
        }
    }
}
