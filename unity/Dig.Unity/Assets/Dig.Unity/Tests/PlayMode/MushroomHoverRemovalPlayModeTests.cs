using System;
using System.Reflection;
using Dig.Domain.Core;
using Dig.Domain.Ecology;
using Dig.Domain.World;
using NUnit.Framework;
using UnityEngine;

namespace Dig.Unity.Tests
{
public sealed class MushroomHoverRemovalPlayModeTests
{
    private GameObject? _interactionRoot;
    private GameObject? _mushroomRoot;

    [TearDown]
    public void TearDown()
    {
        if (_mushroomRoot != null)
        {
            UnityEngine.Object.DestroyImmediate(_mushroomRoot);
        }

        if (_interactionRoot != null)
        {
            UnityEngine.Object.DestroyImmediate(_interactionRoot);
        }
    }

    [Test]
    public void Destroyed_hovered_mushroom_is_cleared_without_mesh_reference_access()
    {
        _interactionRoot = new GameObject("Mushroom hover interaction fixture");
        DigWorldInteraction interaction =
            _interactionRoot.AddComponent<DigWorldInteraction>();

        _mushroomRoot = new GameObject("Hovered mushroom fixture");
        DigMushroomVisual visual =
            _mushroomRoot.AddComponent<DigMushroomVisual>();
        Invoke(visual, "Configure", Snapshot());
        Invoke(visual, "SetHovered", true);
        SetField(interaction, "_hoveredMushroom", visual);

        GameObject mushroomRoot = _mushroomRoot;
        _mushroomRoot = null;
        UnityEngine.Object.DestroyImmediate(mushroomRoot);

        Assert.DoesNotThrow(() => Invoke(interaction, "ClearPointerHover"));
        Assert.That(GetField(interaction, "_hoveredMushroom"), Is.Null);
    }

    private static MushroomSiteSnapshot Snapshot()
    {
        return new MushroomSiteSnapshot(
            EntityId.Parse("cf000000000000000000000000000001"),
            new MushroomDefinitionId("ecology.mushroom.common"),
            new CellId(3, 3, 0),
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

    private static object? GetField(object target, string name)
    {
        FieldInfo field = target.GetType().GetField(
            name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(target.GetType().FullName, name);
        return field.GetValue(target);
    }

    private static void SetField(object target, string name, object? value)
    {
        FieldInfo field = target.GetType().GetField(
            name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(target.GetType().FullName, name);
        field.SetValue(target, value);
    }

    private static object? Invoke(object target, string name, params object[] arguments)
    {
        MethodInfo method = target.GetType().GetMethod(
            name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(target.GetType().FullName, name);
        return method.Invoke(target, arguments);
    }
}
}
