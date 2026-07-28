using System;
using System.Linq;
using System.Reflection;
using Dig.Domain.Core;
using Dig.Domain.Ecology;
using Dig.Domain.World;
using NUnit.Framework;
using UnityEngine;

namespace Dig.Unity.Tests
{
public sealed class MushroomDepthProjectionPlayModeTests
{
    private GameObject? _root;

    [TearDown]
    public void TearDown()
    {
        if (_root != null)
        {
            UnityEngine.Object.DestroyImmediate(_root);
        }
    }

    [Test]
    public void Renderer_keeps_mushrooms_inside_authoritative_z0_to_z3_depth_slabs()
    {
        const float depthOrigin = 0.41f;
        const float depthSpacing = -0.55f;
        const float depthSlabHalfExtent = 0.275f;
        _root = new GameObject("Mushroom depth slab test");
        _root.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        DigMushroomRenderer renderer = _root.AddComponent<DigMushroomRenderer>();
        MushroomDefinitionId definitionId = new MushroomDefinitionId(
            "ecology.mushroom.common");
        MushroomSiteSnapshot[] sites =
        {
            Snapshot("80000000000000000000000000000010", definitionId, 0),
            Snapshot("80000000000000000000000000000011", definitionId, 1),
            Snapshot("80000000000000000000000000000012", definitionId, 2),
            Snapshot("80000000000000000000000000000013", definitionId, 3),
        };

        Invoke(renderer, "Render", (object)sites);

        DigMushroomVisual[] visuals = _root
            .GetComponentsInChildren<DigMushroomVisual>()
            .OrderBy(value => GetModel(value).Cell.Z)
            .ToArray();
        Assert.That(visuals.Length, Is.EqualTo(4));
        for (int z = 0; z < visuals.Length; z++)
        {
            DigMushroomVisual visual = visuals[z];
            MushroomSiteSnapshot model = GetModel(visual);
            float expectedCenter = depthOrigin + (z * depthSpacing);
            BoxCollider collider = visual.GetComponent<BoxCollider>();
            Assert.That(model.Cell.Z, Is.EqualTo(z));
            Assert.That(
                visual.transform.position.z,
                Is.EqualTo(expectedCenter).Within(0.0001f));
            Assert.That(
                collider.bounds.center.z,
                Is.EqualTo(expectedCenter).Within(0.0001f));
            Assert.That(
                collider.bounds.min.z,
                Is.GreaterThan(expectedCenter - depthSlabHalfExtent));
            Assert.That(
                collider.bounds.max.z,
                Is.LessThan(expectedCenter + depthSlabHalfExtent));
        }
    }

    private static MushroomSiteSnapshot Snapshot(
        string siteId,
        MushroomDefinitionId definitionId,
        int z)
    {
        return new MushroomSiteSnapshot(
            EntityId.Parse(siteId),
            definitionId,
            new CellId(3, 3, z),
            MushroomStage.Large,
            stageStartedTick: 0,
            nextStageTick: null,
            growthGeneration: 0,
            activeChopJobId: null,
            activeWorkerId: null,
            requiredSwings: 0,
            completedSwings: 0,
            growthPausedAtTick: null,
            version: 0);
    }

    private static MushroomSiteSnapshot GetModel(DigMushroomVisual visual)
    {
        return (MushroomSiteSnapshot)GetProperty(visual, "Model");
    }

    private static object GetProperty(object target, string name)
    {
        PropertyInfo property = target.GetType().GetProperty(
            name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new MissingMemberException(target.GetType().FullName, name);
        return property.GetValue(target)!;
    }

    private static object Invoke(object target, string name, params object[] arguments)
    {
        MethodInfo? method = target.GetType().GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .SingleOrDefault(value => value.Name == name
                && value.GetParameters().Length == arguments.Length);
        Assert.That(method, Is.Not.Null, name);
        return method!.Invoke(target, arguments)!;
    }
}
}
