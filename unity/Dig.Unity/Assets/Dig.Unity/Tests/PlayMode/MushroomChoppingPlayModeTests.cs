using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dig.Domain.Core;
using Dig.Domain.Ecology;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Dig.Presentation.Agents;
using Dig.Presentation.Inventory;
using NUnit.Framework;
using UnityEngine;

namespace Dig.Unity.Tests
{
public sealed class MushroomChoppingPlayModeTests
{
    private const float ResidentInteractionWorldHeight = 1.52f * 0.5f;
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
    public void Direct_command_completes_large_mushroom_drops_and_same_cell_regrowth()
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
        AssertSuccess(Invoke(terrain, "AdvanceMushrooms", 3L, agents));
        MushroomSiteSnapshot site = ((IEnumerable)Invoke(terrain, "LoadMushrooms"))
            .Cast<MushroomSiteSnapshot>()
            .First(value => value.Cell.Z == 0);
        Assert.That(site.Stage, Is.EqualTo(MushroomStage.Large));
        AgentViewModel worker = agents[0];

        AssertSuccess(Invoke(
            terrain,
            "StartDirectMushroomChop",
            site.SiteId,
            EntityId.Parse(worker.Id),
            new CellId(worker.CellX, worker.CellY, worker.CellZ),
            4L));
        site = ((IEnumerable)Invoke(terrain, "LoadMushrooms"))
            .Cast<MushroomSiteSnapshot>()
            .Single(value => value.SiteId == site.SiteId);
        Assert.That(site.ActiveChopJobId.HasValue, Is.True);
        Assert.That(site.RequiredSwings, Is.GreaterThan(0));

        EntityId jobId = site.ActiveChopJobId!.Value;
        object arrive = GetField(terrain, "_arriveAtMushroom");
        object swing = GetField(terrain, "_completeMushroomSwing");
        object complete = GetField(terrain, "_completeMushroomChop");
        Assembly application = arrive.GetType().Assembly;
        AssertSuccess(Invoke(
            arrive,
            "Handle",
            Create(application, "Dig.Application.Ecology.ArriveAtMushroomCommand", jobId, 5L)));
        for (int index = 0; index < site.RequiredSwings; index++)
        {
            AssertSuccess(Invoke(
                swing,
                "Handle",
                Create(
                    application,
                    "Dig.Application.Ecology.CompleteMushroomSwingCommand",
                    jobId,
                    6L + index)));
        }

        long completionTick = 7L + site.RequiredSwings;
        AssertSuccess(Invoke(
            complete,
            "Handle",
            Create(
                application,
                "Dig.Application.Ecology.CompleteMushroomChopCommand",
                jobId,
                EntityId.Parse("8f000000000000000000000000000001"),
                completionTick)));
        MushroomSiteSnapshot absent = ((IEnumerable)Invoke(terrain, "LoadMushrooms"))
            .Cast<MushroomSiteSnapshot>()
            .Single(value => value.SiteId == site.SiteId);
        Assert.That(absent.Stage, Is.EqualTo(MushroomStage.AbsentRegrowing));
        WorldItemViewModel[] drops = ((IEnumerable)Invoke(terrain, "LoadAllWorldItems"))
            .Cast<WorldItemViewModel>()
            .Where(value => value.CellX == site.Cell.X
                && value.CellY == site.Cell.Y
                && value.CellZ == site.Cell.Z)
            .ToArray();
        Assert.That(drops.Count(value => value.ItemId == "material.mushroom_cap"), Is.EqualTo(2));
        Assert.That(drops.Count(value => value.ItemId == "material.mushroom_leg"), Is.EqualTo(1));
        Assert.That(drops.All(value => value.Quantity == 1), Is.True);
        Assert.That(drops.All(value => value.CanPickup), Is.True);
        Assert.That(
            drops.All(value => value.InteractionProfile.SupportsWorldAction(
                ItemWorldInteractionAction.Pickup)),
            Is.True);

        _root = new GameObject("Mushroom drop renderer test");
        _root.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        DigWorldItemRenderer itemRenderer = _root.AddComponent<DigWorldItemRenderer>();
        itemRenderer.Render(drops);
        DigWorldItemVisual[] dropVisuals =
            _root.GetComponentsInChildren<DigWorldItemVisual>();
        Assert.That(dropVisuals.Length, Is.EqualTo(3));
        Assert.That(dropVisuals.All(value => value.Model.CanPickup), Is.True);
        Assert.That(
            dropVisuals.All(value => value.GetComponentInParent<DigMushroomVisual>() == null),
            Is.True);

        AssertSuccess(Invoke(terrain, "AdvanceMushrooms", completionTick + 1L, agents));
        MushroomSiteSnapshot regrown = ((IEnumerable)Invoke(terrain, "LoadMushrooms"))
            .Cast<MushroomSiteSnapshot>()
            .Single(value => value.SiteId == site.SiteId);
        Assert.That(regrown.Stage, Is.EqualTo(MushroomStage.Tiny));
        Assert.That(regrown.Cell, Is.EqualTo(site.Cell));
    }

    [Test]
    public void Renderer_places_large_mushroom_upright_slightly_above_resident_and_highlights_hover()
    {
        _root = new GameObject("Mushroom renderer test");
        _root.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
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
        BoxCollider collider = visual.GetComponent<BoxCollider>();
        Assert.That(collider.size.y, Is.InRange(0.80f, 0.88f));
        Assert.That(
            collider.size.y / ResidentInteractionWorldHeight,
            Is.InRange(1.05f, 1.15f));
        Assert.That(
            collider.center.y - (collider.size.y * 0.5f),
            Is.EqualTo(0f).Within(0.0001f));
        Assert.That(visual.transform.rotation, Is.EqualTo(Quaternion.identity));
        Assert.That(Vector3.Dot(visual.transform.up, Vector3.up), Is.GreaterThan(0.999f));

        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>();
        Assert.That(renderers.Length, Is.EqualTo(2));
        Assert.That(
            renderers.All(value => value.sharedMaterial.shader.name
                == "Universal Render Pipeline/Lit"),
            Is.True);
        Color original = renderers[0].sharedMaterial.color;
        Invoke(visual, "SetHovered", true);
        MaterialPropertyBlock properties = new MaterialPropertyBlock();
        renderers[0].GetPropertyBlock(properties);
        Color highlighted = properties.GetColor(Shader.PropertyToID("_BaseColor"));
        Assert.That(highlighted.r + highlighted.g + highlighted.b,
            Is.GreaterThan(original.r + original.g + original.b));

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

    private static object Create(Assembly assembly, string typeName, params object[] arguments)
    {
        Type type = RequireType(assembly, typeName);
        object? value = Activator.CreateInstance(type, arguments);
        Assert.That(value, Is.Not.Null, typeName);
        return value!;
    }

    private static object GetField(object target, string name)
    {
        FieldInfo? field = target.GetType().GetField(
            name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, name);
        object? value = field!.GetValue(target);
        Assert.That(value, Is.Not.Null, name);
        return value!;
    }

    private static void AssertSuccess(object result)
    {
        PropertyInfo? property = result.GetType().GetProperty("IsSuccess");
        Assert.That(property, Is.Not.Null, result.GetType().FullName);
        Assert.That((bool)property!.GetValue(result)!, Is.True, result.ToString());
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
