using Dig.Domain.Content;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Dig.Presentation.Inventory;
using NUnit.Framework;
using UnityEngine;

namespace Dig.Unity.Tests
{
public sealed class WorldItemAutomaticGroundingPlayModeTests
{
    private GameObject? _root;

    [TearDown]
    public void TearDown()
    {
        if (_root != null)
        {
            Object.DestroyImmediate(_root);
        }
    }

    [Test]
    public void Centered_and_bottom_pivots_touch_the_same_floor()
    {
        _root = new GameObject("Automatic world item grounding test");
        Transform centered = CreateItem("Centered pivot", x: -1f, bottomPivot: false);
        Transform bottom = CreateItem("Bottom pivot", x: 1f, bottomPivot: true);
        const float floorY = 2.75f;

        DigWorldItemGrounding.PlaceOnFloor(
            centered,
            new Vector3(-1f, floorY, 0f));
        DigWorldItemGrounding.PlaceOnFloor(
            bottom,
            new Vector3(1f, floorY, 0f));
        Physics.SyncTransforms();

        Assert.That(
            centered.GetComponentInChildren<Renderer>().bounds.min.y,
            Is.EqualTo(floorY).Within(0.0001f));
        Assert.That(
            bottom.GetComponentInChildren<Renderer>().bounds.min.y,
            Is.EqualTo(floorY).Within(0.0001f));
    }

    [Test]
    public void Ordinary_world_tool_lies_flat_and_building_box_keeps_upright_pose()
    {
        _root = new GameObject("Loose world item floor pose test");
        ItemDefinition club = CombatEquipmentContent.CreateItems()[0];
        ItemDefinition box = CampfireBuildingBoxContent.Definition.BoxItem;
        DigWorldItemVisual clubVisual = CreateWorldVisual(
            club,
            "00000000000000000000000000000001");
        DigWorldItemVisual boxVisual = CreateWorldVisual(
            box,
            "00000000000000000000000000000002");
        const float floorY = 1.75f;

        DigWorldItemGrounding.PlaceOnFloor(
            clubVisual.transform,
            new Vector3(-1f, floorY, 0f));
        DigWorldItemGrounding.PlaceOnFloor(
            boxVisual.transform,
            new Vector3(1f, floorY, 0f));
        Physics.SyncTransforms();

        Transform clubInstance = clubVisual.transform.Find("Item instance 0");
        Transform boxInstance = boxVisual.transform.Find("Item instance 0");
        Assert.That(clubInstance, Is.Not.Null);
        Assert.That(boxInstance, Is.Not.Null);
        Assert.That(
            Mathf.Abs(Vector3.Dot(clubInstance.up.normalized, Vector3.up)),
            Is.LessThan(0.15f));
        Assert.That(
            Vector3.Dot(boxInstance.up.normalized, Vector3.up),
            Is.GreaterThan(0.95f));
        Bounds clubBounds = ResolveRendererBounds(clubVisual.transform);
        Assert.That(clubBounds.min.y, Is.EqualTo(floorY).Within(0.0001f));
        Assert.That(
            clubVisual.GetComponent<BoxCollider>().bounds.size.x,
            Is.GreaterThan(clubVisual.GetComponent<BoxCollider>().bounds.size.y));
    }

    private DigWorldItemVisual CreateWorldVisual(
        ItemDefinition definition,
        string stackId)
    {
        GameObject root = new GameObject("World visual " + definition.Id);
        root.transform.SetParent(_root!.transform, worldPositionStays: false);
        DigWorldItemVisual visual = root.AddComponent<DigWorldItemVisual>();
        WorldItemViewModel model = new WorldItemViewModel(
            stackId,
            definition.Id.ToString(),
            quantity: 1,
            reservedQuantity: 0,
            cellX: 0,
            cellY: 0,
            cellZ: 0,
            interactionProfile: definition.Interactions);
        ItemStackVisualLayoutViewModel layout =
            new ItemStackVisualLayoutPresenter().Present(model);
        visual.Configure(
            model,
            layout,
            DigWorldItemVisualPolicy.Resolve(catalog: null, itemId: model.ItemId));
        return visual;
    }

    private static Bounds ResolveRendererBounds(Transform root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        Assert.That(renderers, Is.Not.Empty);
        Bounds bounds = renderers[0].bounds;
        for (int index = 1; index < renderers.Length; index++)
        {
            bounds.Encapsulate(renderers[index].bounds);
        }
        return bounds;
    }

    private Transform CreateItem(string name, float x, bool bottomPivot)
    {
        Transform root = new GameObject(name).transform;
        root.SetParent(_root!.transform, worldPositionStays: false);
        root.localPosition = new Vector3(x, 0f, 0f);
        root.localRotation = Quaternion.Euler(0f, 0f, bottomPivot ? 0f : 11f);
        root.localScale = bottomPivot
            ? new Vector3(1.15f, 0.85f, 1f)
            : Vector3.one;

        GameObject mesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
        mesh.name = name + " mesh";
        mesh.transform.SetParent(root, worldPositionStays: false);
        mesh.transform.localScale = new Vector3(0.42f, 0.64f, 0.38f);
        mesh.transform.localPosition = bottomPivot
            ? new Vector3(0f, 0.32f, 0f)
            : Vector3.zero;
        Object.DestroyImmediate(mesh.GetComponent<Collider>());
        return root;
    }
}
}