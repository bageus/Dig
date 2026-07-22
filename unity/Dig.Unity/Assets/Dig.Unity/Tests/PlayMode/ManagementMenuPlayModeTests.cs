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
