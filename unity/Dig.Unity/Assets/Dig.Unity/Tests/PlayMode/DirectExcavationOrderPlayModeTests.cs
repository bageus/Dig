using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dig.Domain.Core;
using Dig.Domain.World;
using Dig.Presentation.Agents;
using Dig.Presentation.Jobs;
using Dig.Presentation.World;
using NUnit.Framework;
using UnityEngine;

namespace Dig.Unity.Tests
{
public sealed class DirectExcavationOrderPlayModeTests
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
    public void Designated_cell_stays_clickable_when_movement_surfaces_are_active()
    {
        _root = new GameObject("Direct excavation collider test");
        DigWorldRenderer renderer = _root.AddComponent<DigWorldRenderer>();
        renderer.Render(World());

        Invoke(renderer, "SetTunnelDigInteractionActive", false);

        DigCellVisual designated = _root.GetComponentsInChildren<DigCellVisual>(true)
            .Single(value => value.Model.IsDesignated);
        DigCellVisual ordinary = _root.GetComponentsInChildren<DigCellVisual>(true)
            .Single(value => !value.Model.IsDesignated);
        Assert.That(designated.GetComponent<Collider>().enabled, Is.True);
        Assert.That(ordinary.GetComponent<Collider>().enabled, Is.False);
    }

    [Test]
    public void Manual_cluster_claims_a_real_dig_job_for_the_requested_resident()
    {
        Assembly runtime = typeof(DigWorldInteraction).Assembly;
        object world = InvokeStatic(
            RequireType(runtime, "Dig.Unity.DigWorldSession"),
            "CreateDemo",
            8,
            8,
            4);
        object worldView = Invoke(world, "LoadView");
        object journal = GetProperty(world, "Journal");
        object residents = InvokeStatic(
            RequireType(runtime, "Dig.Unity.DigAgentSession"),
            "CreateDemo",
            worldView,
            journal);
        IReadOnlyList<AgentViewModel> residentModels =
            ((IEnumerable)Invoke(residents, "LoadView"))
                .Cast<AgentViewModel>()
                .ToArray();
        object terrain = InvokeStatic(
            RequireType(runtime, "Dig.Unity.DigTerrainWorkSession"),
            "CreateDemo",
            world,
            residentModels,
            journal,
            GetProperty(residents, "SkillGrants"));
        Invoke(terrain, "InitializeDynamicDesignations", journal);
        JobOverlayViewModel seed =
            ((IEnumerable)Invoke(terrain, "LoadJobs"))
                .Cast<JobOverlayViewModel>()
                .First(value => value.AssignedAgentId != null
                    && value.TargetX.HasValue
                    && value.TargetY.HasValue
                    && value.TargetZ == 0);

        object result = Invoke(
            terrain,
            "AssignExcavationClusterToResidents",
            new CellId(seed.TargetX!.Value, seed.TargetY!.Value),
            new[] { seed.AssignedAgentId! },
            1L);

        Assert.That((bool)GetProperty(result, "IsSuccess"), Is.True);
        JobOverlayViewModel[] jobs =
            ((IEnumerable)Invoke(terrain, "LoadJobs"))
                .Cast<JobOverlayViewModel>()
                .ToArray();
        Assert.That(jobs.Any(value =>
            value.AssignedAgentId == seed.AssignedAgentId
            && (value.Status == "Claimed" || value.Status == "InProgress")),
            Is.True);
    }

    private static WorldViewModel World()
    {
        WorldCellViewModel designated = Cell(0, designated: true);
        WorldCellViewModel ordinary = Cell(1, designated: false);
        return new WorldViewModel(
            2,
            1,
            2,
            1,
            new[]
            {
                new WorldChunkViewModel(
                    0,
                    0,
                    1,
                    new[] { designated, ordinary }),
            });
    }

    private static WorldCellViewModel Cell(int x, bool designated)
    {
        return new WorldCellViewModel(
            x,
            0,
            "test.rock",
            isSolid: true,
            isExplored: true,
            isDesignated: designated,
            hardness: 100,
            damage: 0,
            temperature: 20,
            worldVersion: 1);
    }

    private static Type RequireType(Assembly assembly, string name)
    {
        Type? type = assembly.GetType(name);
        Assert.That(type, Is.Not.Null, name);
        return type!;
    }

    private static object InvokeStatic(
        Type type,
        string name,
        params object[] arguments)
    {
        MethodInfo method = RequireMethod(
            type,
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            name,
            arguments.Length);
        return method.Invoke(null, arguments)!;
    }

    private static object Invoke(
        object target,
        string name,
        params object[] arguments)
    {
        MethodInfo method = RequireMethod(
            target.GetType(),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            name,
            arguments.Length);
        return method.Invoke(target, arguments)!;
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
