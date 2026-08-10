using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Dig.Unity.Tests
{

public sealed class ClockScheduleMeaningPlayModeTests
{
    private GameObject? _root;

    [TearDown]
    public void TearDown()
    {
        if (_root != null)
        {
            Object.DestroyImmediate(_root);
        }

        if (EventSystem.current != null)
        {
            Object.DestroyImmediate(EventSystem.current.gameObject);
        }
    }

    [Test]
    public void Orange_segments_are_work_and_blue_segments_are_rest_free_time()
    {
        _root = new GameObject("Clock schedule meaning", typeof(RectTransform));
        DigHudOverlay overlay = _root.AddComponent<DigHudOverlay>();
        DigGameHudCanvas hud = _root.AddComponent<DigGameHudCanvas>();
        Invoke(hud, "InitializeStartup", overlay);
        Invoke(hud, "UpdateScheduleSegments", 24, 8, 16, true);

        Image work = Require(
            "Game Clock Panel/Clock Face/Schedule Overlay/Schedule Segment 8")
            .GetComponent<Image>();
        Image rest = Require(
            "Game Clock Panel/Clock Face/Schedule Overlay/Schedule Segment 0")
            .GetComponent<Image>();

        Assert.That(work.color.r, Is.EqualTo(0.96f).Within(0.001f));
        Assert.That(work.color.g, Is.EqualTo(0.50f).Within(0.001f));
        Assert.That(work.color.b, Is.EqualTo(0.12f).Within(0.001f));
        Assert.That(rest.color.r, Is.EqualTo(0.26f).Within(0.001f));
        Assert.That(rest.color.g, Is.EqualTo(0.56f).Within(0.001f));
        Assert.That(rest.color.b, Is.EqualTo(0.88f).Within(0.001f));
    }

    private Transform Require(string path)
    {
        Transform? value = _root!.transform.Find(path);
        Assert.That(value, Is.Not.Null, path);
        return value!;
    }

    private static void Invoke(
        DigGameHudCanvas hud,
        string methodName,
        params object[] arguments)
    {
        MethodInfo? method = typeof(DigGameHudCanvas).GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, methodName);
        method!.Invoke(hud, arguments);
    }
}

}