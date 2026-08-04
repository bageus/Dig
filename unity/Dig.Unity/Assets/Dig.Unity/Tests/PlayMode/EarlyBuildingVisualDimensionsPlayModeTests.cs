using System;
using System.Linq;
using Dig.Presentation.Buildings;
using NUnit.Framework;
using UnityEngine;

namespace Dig.Unity.Tests
{

public sealed class EarlyBuildingVisualDimensionsPlayModeTests
{
    private const float Tolerance = 0.02f;

    [Test]
    public void Completed_profiles_match_declared_visual_bounds()
    {
        DigRepresentativeBuildingPrefabLibrary library =
            DigRepresentativeBuildingPrefabLibrary.Acquire();
        try
        {
            Assert.That(library.ValidationErrors, Is.Empty);
            AssertCompleted(
                library,
                "building.tent",
                new Vector3(3f, 2f, 2f),
                "Tent Roof Left",
                "Tent Roof Right",
                "Tent Entrance Flap");
            AssertCompleted(
                library,
                "building.stone_mason",
                new Vector3(3.5f, 2.5f, 2.5f),
                "Stone Foundation",
                "Stone Workbench",
                "Mason Roof");
            AssertCompleted(
                library,
                "building.wood_workshop",
                new Vector3(2.5f, 2f, 2f),
                "Wood Foundation",
                "Saw Bench",
                "Timber Log");
        }
        finally
        {
            library.Dispose();
        }
    }

    [Test]
    public void Building_box_profiles_remain_compact()
    {
        DigRepresentativeBuildingPrefabLibrary library =
            DigRepresentativeBuildingPrefabLibrary.Acquire();
        try
        {
            foreach (string stableId in new[]
            {
                "building.tent",
                "building.stone_mason",
                "building.wood_workshop",
            })
            {
                Assert.That(library.TryResolve(
                    stableId,
                    BuildingVisualState.BuildingBox,
                    out DigBuildingVisualResolution resolution), Is.True);
                GameObject instance = Instantiate(resolution);
                try
                {
                    Bounds bounds = RendererBounds(instance);
                    Assert.That(bounds.size.x, Is.LessThan(1f));
                    Assert.That(bounds.size.y, Is.LessThan(1f));
                    Assert.That(bounds.size.z, Is.LessThan(1f));
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(instance);
                }
            }
        }
        finally
        {
            library.Dispose();
        }
    }

    private static void AssertCompleted(
        DigRepresentativeBuildingPrefabLibrary library,
        string stableId,
        Vector3 expectedSize,
        params string[] requiredParts)
    {
        Assert.That(library.TryResolve(
            stableId,
            BuildingVisualState.Completed,
            out DigBuildingVisualResolution resolution), Is.True);
        Assert.That(resolution.Asset.IsFallback, Is.False);
        Assert.That(resolution.ExpectedFootprintSize, Is.EqualTo(Vector2Int.one));

        GameObject instance = Instantiate(resolution);
        try
        {
            Bounds bounds = RendererBounds(instance);
            AssertClose(expectedSize.x, bounds.size.x, $"{stableId} width");
            AssertClose(expectedSize.y, bounds.size.y, $"{stableId} height");
            AssertClose(expectedSize.z, bounds.size.z, $"{stableId} depth");
            AssertClose(0f, bounds.min.y, $"{stableId} floor grounding");

            BoxCollider selection = instance.GetComponentInChildren<BoxCollider>(true);
            Assert.That(selection, Is.Not.Null);
            AssertClose(expectedSize.x, selection.size.x, $"{stableId} collider width");
            AssertClose(expectedSize.y, selection.size.y, $"{stableId} collider height");
            AssertClose(expectedSize.z, selection.size.z, $"{stableId} collider depth");
            AssertClose(0f, selection.center.y - (selection.size.y * 0.5f),
                $"{stableId} collider grounding");

            string[] names = instance.GetComponentsInChildren<Transform>(true)
                .Select(value => value.name)
                .ToArray();
            foreach (string requiredPart in requiredParts)
            {
                Assert.That(names, Does.Contain(requiredPart));
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    private static GameObject Instantiate(DigBuildingVisualResolution resolution)
    {
        Assert.That(resolution.Asset.Prefab, Is.Not.Null);
        GameObject instance = UnityEngine.Object.Instantiate(resolution.Asset.Prefab!);
        instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        instance.transform.localScale = Vector3.one;
        instance.SetActive(true);
        return instance;
    }

    private static Bounds RendererBounds(GameObject instance)
    {
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
        Assert.That(renderers, Is.Not.Empty);
        Bounds bounds = renderers[0].bounds;
        for (int index = 1; index < renderers.Length; index++)
        {
            bounds.Encapsulate(renderers[index].bounds);
        }

        return bounds;
    }

    private static void AssertClose(float expected, float actual, string message)
    {
        Assert.That(Math.Abs(expected - actual), Is.LessThanOrEqualTo(Tolerance), message);
    }
}

}
