using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dig.Domain.Core;
using Dig.Domain.Ecology;
using Dig.Domain.World;
using Dig.Presentation.Agents;
using NUnit.Framework;
using UnityEngine;

namespace Dig.Unity.Tests
{
public sealed class MushroomChoppingPlayModeTests
{
    private GameObject? _root;

    [TearDown]
    public void TearDown()
    {
        if (_root != null)
        {
            UnityEngine.Object.DestroyImmediate(_root);
        }
    }

    [Test]
    public void Demo_contains_surface_and_lower_cave_mushroom_sites()
    {
        Assembly runtime = typeof(DigWorldInteraction).Assembly;
        object world = InvokeStatic(
            RequireType(runtime, "Dig.Unity.DigWorldSession"),
            "CreateDemo",
            20,
            14,
            5);
        object worldView = Invoke(world, "LoadView");
        object journal = GetProperty(world, "Journal");
        object tunnel = Invoke(world, "CreateTunnelNavigationVolume");
        object residents = InvokeStatic(
            RequireType(runtime, "Dig.Unity.DigAgentSession"),
            "CreateDemo",
            worldView,
            tunnel,
            journal);
        IReadOnlyList<AgentViewModel> agents =
            ((IEnumerable)Invoke(residents, "LoadView"))
                .Cast<AgentViewModel>()
                .ToArray();
        object terrain = InvokeStatic(
            RequireType(runtime, "Dig.Unity.DigTerrainWorkSession"),
            "CreateDemo",
            world,
            agents,
            journal,
            GetProperty(residents, "SkillGrants"));
        Invoke(terrain, "InitializeBuildingDemo", journal);
        Invoke(terrain, "InitializeMushroomDemo", 0L);

        MushroomSiteSnapshot[] sites = ((IEnumerable)Invoke(terrain, "LoadMushrooms"))
            .Cast<MushroomSiteSnapshot>()
            .ToArray();

        Assert.That(sites.Length, Is.EqualTo(2));
        Assert.That(sites.Count(value => value.Cell.Z == 0), Is.EqualTo(1));
        Assert.That(sites.Count(value => value.Cell.Z > 0), Is.EqualTo(1));
        Assert.That(sites.All(value => value.Stage == MushroomStage.Tiny), Is.True);
    }

    [Test]
    public void Renderer_hides_absent_site_and_large_visual_is_taller_than_one_cell()
    {
        _root = new GameObject("Mushroom renderer test");
        DigMushroomRenderer renderer = _root.AddComponent<DigMushroomRenderer>();
        EntityId siteId = EntityId.Parse("80000000000000000000000000000001");
        MushroomDefinitionId definitionId = new MushroomDefinitionId(
            "ecology.mushroom.common");
        MushroomSiteSnapshot large = Snapshot(
            siteId,
            definitionId,
            MushroomStage.Large);

        Invoke(renderer, "Render", (object)new[] { large });

        DigMushroomVisual visual = _root.GetComponentInChildren<DigMushroomVisual>();
        Assert.That(visual, Is.Not.Null);
        Assert.That(visual.GetComponent<BoxCollider>().size.y, Is.GreaterThan(1f));

        Invoke(
            renderer,
            "Render",
            (object)new[]
            {
                Snapshot(siteId, definitionId, MushroomStage.AbsentRegrowing),
            });
        Assert.That((int)GetProperty(renderer, "ActiveCount"), Is.EqualTo(0));
    }

    private static MushroomSiteSnapshot Snapshot(
        EntityId siteId,
        MushroomDefinitionId definitionId,
        MushroomStage stage)
    {
        return new MushroomSiteSnapshot(
            siteId,
            definitionId,
            new CellId(3, 3, 0),
            stage,
            stageStartedTick: 0,
            nextStageTick: stage == MushroomStage.Large ? null : 1,
            growthGeneration: 0,
            activeChopJobId: null,
            activeWorkerId: null,
            requiredSwings: 0,
            completedSwings: 0,
            growthPausedAtTick: null,
            version: 0);
    }

    private static Type RequireType(Assembly assembly, string name)
    {
        Type? type = assembly.GetType(name);
        Assert.That(type, Is.Not.Null, name);
        return type!;
    }

    private static object InvokeStatic(Type type, string name, params object[] arguments)
    {
        return RequireMethod(
            type,
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            name,
            arguments.Length).Invoke(null, arguments)!;
    }

    private static object Invoke(object target, string name, params object[] arguments)
    {
        return RequireMethod(
            target.GetType(),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            name,
            arguments.Length).Invoke(target, arguments)!;
    }

    private static MethodInfo RequireMethod(
        Type type,
        BindingFlags flags,
        string name,
        int argumentCount)
    {
        MethodInfo? method = type.GetMethods(flags)
            .SingleOrDefault(value => value.Name == name
                && value.GetParameters().Length == argumentCount);
        Assert.That(method, Is.Not.Null, name);
        return method!;
    }

    private static object GetProperty(object target, string name)
    {
        PropertyInfo? property = target.GetType().GetProperty(
            name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.That(property, Is.Not.Null, name);
        return property!.GetValue(target)!;
    }
}
}
