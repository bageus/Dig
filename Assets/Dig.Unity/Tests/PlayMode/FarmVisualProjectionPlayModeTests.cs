using System.Collections;
using System.Linq;
using Dig.Domain.Farming;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dig.Unity.Tests
{

public sealed class FarmVisualProjectionPlayModeTests
{
    private GameObject? _root;

    [TearDown]
    public void TearDown()
    {
        if (_root != null) Object.DestroyImmediate(_root);
    }

    [Test]
    public void Decoration_builds_declared_plot_and_low_fence_without_physics()
    {
        DigFarmVisualDecoration decoration = CreateDecoration();

        Transform dirt = Required("Farm Decoration/Dirt");
        Assert.That(dirt.localScale, Is.EqualTo(new Vector3(2f, 0.08f, 1.5f)));
        Transform[] posts = decoration.GetComponentsInChildren<Transform>(true)
            .Where(value => value.name.StartsWith("Post "))
            .ToArray();
        Assert.That(posts, Has.Length.EqualTo(4));
        Assert.That(posts.All(value => value.localScale.y == 0.5f), Is.True);
        Assert.That(decoration.GetComponentsInChildren<Collider>(true), Is.Empty);
        Assert.That(decoration.GetComponentsInChildren<Rigidbody>(true), Is.Empty);
    }

    [UnityTest]
    public IEnumerator Snapshot_projects_exclusive_contents_and_clamped_animal_motion()
    {
        DigFarmVisualDecoration decoration = CreateDecoration();
        decoration.SetState(Snapshot(
            FarmMode.Hamsters,
            hamsterCount: 8,
            feedCount: 2));
        yield return null;

        GameObject[] hamsters = Children("Hamster ");
        Assert.That(hamsters.Count(value => value.activeSelf), Is.EqualTo(8));
        Assert.That(Children("Grub ").Count(value => value.activeSelf), Is.Zero);
        Assert.That(Children("Mushroom ").Count(value => value.activeSelf), Is.Zero);
        Assert.That(Children("Feed Cap ").Count(value => value.activeSelf), Is.EqualTo(2));
        foreach (GameObject hamster in hamsters)
        {
            Assert.That(Mathf.Abs(hamster.transform.localPosition.x), Is.LessThanOrEqualTo(0.87f));
            Assert.That(Mathf.Abs(hamster.transform.localPosition.z), Is.LessThanOrEqualTo(0.62f));
        }

        decoration.SetState(Snapshot(
            FarmMode.Mushrooms,
            seedEstablished: true,
            mushroomCount: 3));
        yield return null;

        Assert.That(Children("Mushroom ").Count(value => value.activeSelf), Is.EqualTo(3));
        Assert.That(Children("Hamster ").Count(value => value.activeSelf), Is.Zero);
        Assert.That(Children("Feed Cap ").Count(value => value.activeSelf), Is.Zero);
    }

    private DigFarmVisualDecoration CreateDecoration()
    {
        _root = new GameObject("Farm visual projection test");
        DigFarmVisualDecoration.Ensure(_root);
        return _root.GetComponent<DigFarmVisualDecoration>();
    }

    private Transform Required(string path)
    {
        Transform? value = _root!.transform.Find(path);
        Assert.That(value, Is.Not.Null, path);
        return value!;
    }

    private GameObject[] Children(string prefix) => _root!
        .GetComponentsInChildren<Transform>(true)
        .Where(value => value.name.StartsWith(prefix))
        .Select(value => value.gameObject)
        .ToArray();

    private static FarmSnapshot Snapshot(
        FarmMode mode,
        bool seedEstablished = false,
        int mushroomCount = 0,
        int hamsterCount = 0,
        int feedCount = 0)
    {
        return new FarmSnapshot(
            mode,
            seedEstablished,
            mushroomCount,
            residualMushrooms: 0,
            hamsterCount,
            grubCount: 0,
            feedCount,
            nextReproductionTick: -1,
            nextFeedConsumptionTick: -1);
    }
}

}
