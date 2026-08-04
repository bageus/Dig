using NUnit.Framework;
using UnityEngine;

namespace Dig.Unity.Tests
{

public sealed class CombatHealthBarPresentationPlayModeTests
{
    [Test]
    public void Different_actor_scales_keep_equal_world_width_and_place_bar_above_renderers()
    {
        GameObject cameraObject = new GameObject("Health bar test camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.transform.position = new Vector3(0f, 1f, -10f);

        GameObject small = CreateOwner("Small actor", new Vector3(-2f, 0f, 0f), 1f);
        GameObject large = CreateOwner("Large actor", new Vector3(2f, 0f, 0f), 2f);
        DigCombatHealthBar smallBar = CreateBar(small, camera);
        DigCombatHealthBar largeBar = CreateBar(large, camera);

        smallBar.SendMessage("LateUpdate");
        largeBar.SendMessage("LateUpdate");

        Renderer smallOwnerRenderer = small.GetComponent<Renderer>();
        Renderer largeOwnerRenderer = large.GetComponent<Renderer>();
        Renderer smallBackground = FindPart(smallBar, "Background");
        Renderer largeBackground = FindPart(largeBar, "Background");
        Assert.That(
            smallBackground.bounds.size.x,
            Is.EqualTo(largeBackground.bounds.size.x).Within(0.001f));
        Assert.That(
            smallBar.transform.position.y,
            Is.GreaterThan(smallOwnerRenderer.bounds.max.y));
        Assert.That(
            largeBar.transform.position.y,
            Is.GreaterThan(largeOwnerRenderer.bounds.max.y));

        Object.DestroyImmediate(small);
        Object.DestroyImmediate(large);
        Object.DestroyImmediate(cameraObject);
    }

    private static GameObject CreateOwner(string name, Vector3 position, float scale)
    {
        GameObject owner = GameObject.CreatePrimitive(PrimitiveType.Cube);
        owner.name = name;
        owner.transform.position = position;
        owner.transform.localScale = Vector3.one * scale;
        return owner;
    }

    private static DigCombatHealthBar CreateBar(GameObject owner, Camera camera)
    {
        GameObject root = new GameObject("CombatHealthBar");
        root.transform.SetParent(owner.transform, false);
        DigCombatHealthBar bar = root.AddComponent<DigCombatHealthBar>();
        bar.Configure(50, 100, visible: true, camera, verticalOffset: 1.45f);
        return bar;
    }

    private static Renderer FindPart(DigCombatHealthBar bar, string name)
    {
        foreach (Renderer renderer in bar.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer.gameObject.name == name)
            {
                return renderer;
            }
        }

        Assert.Fail("Health bar part was not found: " + name);
        return null!;
    }
}

}
