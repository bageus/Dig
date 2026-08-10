using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Dig.Domain.Inventory;
using Dig.Presentation.Agents;
using Dig.Presentation.Buildings;
using Dig.Presentation.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dig.Unity.Tests
{

public sealed class FullDepthEatingTentPlayModeTests
{
    private GameObject? _root;
    private Material? _material;

    [TearDown]
    public void TearDown()
    {
        if (_root != null)
        {
            UnityEngine.Object.DestroyImmediate(_root);
        }

        if (_material != null)
        {
            UnityEngine.Object.DestroyImmediate(_material);
        }
    }

    [Test]
    public void Every_depth_layer_spans_one_full_cell_and_shares_boundaries()
    {
        Type? builder = typeof(DigWorldRenderer).Assembly.GetType(
            "Dig.Unity.DigTerrainChunkMeshBuilder");
        Assert.That(builder, Is.Not.Null);
        MethodInfo? resolve = builder!.GetMethod(
            "ResolveDepthExtents",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(resolve, Is.Not.Null);

        (float min, float max)[] layers = new (float min, float max)[4];
        for (int z = 0; z < layers.Length; z++)
        {
            layers[z] = Resolve(resolve!, z);
            Assert.That(
                layers[z].max - layers[z].min,
                Is.EqualTo(1f).Within(0.00001f));
        }

        for (int z = 0; z < layers.Length - 1; z++)
        {
            Assert.That(
                layers[z].min,
                Is.EqualTo(layers[z + 1].max).Within(0.00001f));
        }
    }

    [Test]
    public void Excavated_open_cell_does_not_render_a_cyan_floor_tile()
    {
        _root = GameObject.CreatePrimitive(PrimitiveType.Cube);
        DigCellVisual visual = _root.AddComponent<DigCellVisual>();
        WorldCellViewModel open = new WorldCellViewModel(
            x: 4,
            y: 3,
            z: 2,
            materialId: "terrain.air",
            isSolid: false,
            isExplored: true,
            isDesignated: false,
            hardness: 0,
            damage: 0,
            temperature: 20,
            worldVersion: 1);

        visual.transform.localScale = Vector3.zero;
        visual.Configure(open, new Color(0.20f, 0.52f, 0.66f, 1f));

        Assert.That(_root.GetComponent<Renderer>().enabled, Is.False);
        Assert.That(visual.transform.localScale, Is.EqualTo(Vector3.zero));
    }

    [UnityTest]
    public IEnumerator Meal_portion_is_visible_in_hand_without_colliders()
    {
        _root = new GameObject("Meal Portion Visual Test");
        DigAgentEquipmentVisual visual =
            _root.AddComponent<DigAgentEquipmentVisual>();
        _material = CreateMaterial();

        visual.Configure(
            DigAgentVisual.MealVisualId,
            EquipmentAppearanceKind.Generic,
            _material);
        yield return null;

        string[] parts = visual.GetComponentsInChildren<Transform>(true)
            .Select(value => value.name)
            .ToArray();
        Assert.That(visual.CurrentItemId, Is.EqualTo(DigAgentVisual.MealVisualId));
        Assert.That(parts, Does.Contain("Meal Portion"));
        Assert.That(parts, Does.Contain("Meal Bite Edge"));
        Assert.That(visual.GetComponentsInChildren<Collider>(true), Is.Empty);
    }

    [Test]
    public void Eat_action_places_the_rig_in_a_seated_ground_pose()
    {
        _root = new GameObject("Resident Eating Rig Test");
        DigResidentRig rig = _root.AddComponent<DigResidentRig>();
        Transform leftArm = Child("Left Arm");
        Transform rightArm = Child("Right Arm");
        Transform leftLeg = Child("Left Leg");
        Transform rightLeg = Child("Right Leg");
        Renderer[] renderers = new Renderer[4];
        for (int index = 0; index < renderers.Length; index++)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = "Rig Part " + index;
            part.transform.SetParent(_root.transform, false);
            renderers[index] = part.GetComponent<Renderer>();
        }

        Transform[] sockets = new Transform[6];
        for (int index = 0; index < sockets.Length; index++)
        {
            sockets[index] = Child("Socket " + index);
        }

        rig.Initialize(
            renderers,
            leftArm,
            rightArm,
            leftLeg,
            rightLeg,
            sockets);
        rig.ApplyAction(new ResidentActionVisualViewModel(
            "resident.eater",
            ResidentActionVisualState.Eat,
            normalizedProgress: 0.25d,
            isLooping: true,
            version: 1));

        Assert.That(rig.transform.localPosition.y, Is.EqualTo(-0.31f).Within(0.001f));
        Assert.That(rightArm.localRotation, Is.Not.EqualTo(Quaternion.identity));
        Assert.That(leftLeg.localRotation, Is.Not.EqualTo(Quaternion.identity));
        Assert.That(rightLeg.localRotation, Is.Not.EqualTo(Quaternion.identity));
    }

    [Test]
    public void Tent_template_places_entrance_on_the_camera_facing_positive_z_side()
    {
        using DigRepresentativeBuildingPrefabLibrary library =
            DigRepresentativeBuildingPrefabLibrary.Acquire();
        Assert.That(
            library.TryResolve(
                "building.tent",
                BuildingVisualState.Completed,
                out DigBuildingVisualResolution resolution),
            Is.True);
        Assert.That(resolution.FacesCamera, Is.True);
        Assert.That(resolution.ExpectedFootprintSize, Is.EqualTo(Vector2Int.one));

        GameObject? template = resolution.Asset.Prefab;
        Assert.That(template, Is.Not.Null);
        Transform? flap = template!.GetComponentsInChildren<Transform>(true)
            .SingleOrDefault(value => value.name == "Tent Entrance Flap");
        Assert.That(flap, Is.Not.Null);
        Vector3 flapInTemplate = template.transform.InverseTransformPoint(flap!.position);
        Assert.That(flapInTemplate.z, Is.GreaterThan(0.9f));
    }

    private Transform Child(string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(_root!.transform, false);
        return child.transform;
    }

    private static (float min, float max) Resolve(MethodInfo method, int z)
    {
        object[] arguments = { z, 0f, 0f };
        method.Invoke(null, arguments);
        return ((float)arguments[1], (float)arguments[2]);
    }

    private static Material CreateMaterial()
    {
        Shader? shader = Shader.Find("Universal Render Pipeline/Lit")
            ?? Shader.Find("Standard");
        Assert.That(shader, Is.Not.Null);
        return new Material(shader!);
    }
}

}
