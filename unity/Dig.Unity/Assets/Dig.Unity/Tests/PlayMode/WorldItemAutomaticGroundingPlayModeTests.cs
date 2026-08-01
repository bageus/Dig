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
