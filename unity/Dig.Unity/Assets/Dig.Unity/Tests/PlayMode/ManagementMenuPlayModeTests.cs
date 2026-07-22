using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Dig.Unity.Tests
{

public sealed class ManagementMenuPlayModeTests
{
    private GameObject? _root;

    [SetUp]
    public void SetUp()
    {
        _root = new GameObject("Management HUD Test", typeof(RectTransform));
        DigHudOverlay overlay = _root.AddComponent<DigHudOverlay>();
        DigGameHudCanvas hud = _root.AddComponent<DigGameHudCanvas>();
        Invoke(hud, "InitializeStartup", overlay);
    }

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
    public void Menu_button_toggles_dropdown_and_entry_opens_overlay()
    {
        Transform dropdown = Require("Management Dropdown");
        Assert.That(dropdown.gameObject.activeSelf, Is.False);

        Require("Management Menu Button").GetComponent<Button>().onClick.Invoke();
        Assert.That(dropdown.gameObject.activeSelf, Is.True);

        Require("Management Menu Button").GetComponent<Button>().onClick.Invoke();
        Assert.That(dropdown.gameObject.activeSelf, Is.False);

        Require("Management Menu Button").GetComponent<Button>().onClick.Invoke();
        Require("Management Dropdown/Dwarfs").GetComponent<Button>().onClick.Invoke();
        Assert.That(dropdown.gameObject.activeSelf, Is.False);
        Assert.That(Require("Management Overlay").gameObject.activeSelf, Is.True);
    }

    [Test]
    public void Overlay_close_is_top_right_and_management_labels_are_english()
    {
        Require("Management Menu Button").GetComponent<Button>().onClick.Invoke();
        Require("Management Dropdown/Dwarfs").GetComponent<Button>().onClick.Invoke();
        Invoke(
            _root!.GetComponent<DigGameHudCanvas>(),
            "BeginManagementOverlay",
            "Dwarfs",
            new[] { "Standard" },
            0,
            new System.Action<int>(_ => { }));

        RectTransform close = Require("Management Overlay/Close")
            .GetComponent<RectTransform>();
        Assert.That(close.anchorMin, Is.EqualTo(Vector2.one));
        Assert.That(close.anchorMax, Is.EqualTo(Vector2.one));
        Assert.That(close.offsetMin, Is.EqualTo(new Vector2(-52f, -52f)));
        Assert.That(close.offsetMax, Is.EqualTo(new Vector2(-10f, -10f)));

        System.Type? localization = typeof(DigGameHudCanvas).Assembly.GetType(
            "Dig.Unity.DigManagementLocalization");
        Assert.That(localization, Is.Not.Null);
        MethodInfo? resolve = localization!.GetMethod(
            "Resolve",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(resolve, Is.Not.Null);
        Assert.That(resolve!.Invoke(null, new object[] { "management.name" }),
            Is.EqualTo("Name"));
        Assert.That(resolve.Invoke(null, new object[] { "resident.need.alertness.vigor" }),
            Is.EqualTo("Vigor"));
        Assert.That(resolve.Invoke(null, new object[] { "management.schedule.work" }),
            Is.EqualTo("Work time"));
    }

    private Transform Require(string path)
    {
        Transform? value = _root!.transform.Find(path);
        Assert.That(value, Is.Not.Null, path);
        return value!;
    }

    private static void Invoke(object target, string methodName, params object[] values)
    {
        MethodInfo? method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, methodName);
        method!.Invoke(target, values);
    }
}

}
