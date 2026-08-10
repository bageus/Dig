using System;
using System.Linq;
using System.Reflection;
using Dig.Domain.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Dig.Unity.Tests
{

public sealed class DemoStartupRegressionPlayModeTests
{
    [Test]
    public void Demo_creation_excludes_unmineable_patch_from_deposit_hosts()
    {
        DigWorldSession session = DigWorldSession.CreateDemo(24, 16, 29);
        WorldState world = session.Repository.Get();
        TerrainDepositInstance[] deposits = world.TerrainDeposits.Snapshot().ToArray();

        Assert.That(deposits, Is.Not.Empty);
        for (int index = 0; index < deposits.Length; index++)
        {
            TerrainDepositInstance deposit = deposits[index];
            var cell = world.GetCell(deposit.Cell);
            Assert.That(cell.IsSuccess, Is.True, deposit.Cell.ToString());
            MaterialDefinition? material = world.Materials.Get(
                cell.Value.State.MaterialId);
            Assert.That(material, Is.Not.Null, deposit.Cell.ToString());
            Assert.That(material!.IsSolid, Is.True, deposit.Cell.ToString());
            Assert.That(material.IsMineable, Is.True, deposit.Cell.ToString());
            Assert.That(
                cell.Value.State.MaterialId,
                Is.Not.EqualTo(DefaultTerrainMaterials.Unmineable));
        }
    }

    [Test]
    public void Startup_hud_clock_hover_is_idle_before_runtime_binding()
    {
        GameObject root = new GameObject(
            "Startup HUD Regression",
            typeof(RectTransform));
        try
        {
            DigHudOverlay overlay = root.AddComponent<DigHudOverlay>();
            DigGameHudCanvas hud = root.AddComponent<DigGameHudCanvas>();
            Invoke(hud, "InitializeStartup", overlay);

            Assert.DoesNotThrow(() =>
                Invoke(hud, "RefreshClockInteractionFrame"));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            if (EventSystem.current != null)
            {
                UnityEngine.Object.DestroyImmediate(EventSystem.current.gameObject);
            }
        }
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
