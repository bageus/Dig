using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dig.Application.World;
using Dig.Domain.Core;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Dig.Presentation.Agents;
using Dig.Presentation.Jobs;
using NUnit.Framework;

namespace Dig.Unity.Tests
{
public sealed class SpatialExcavationDesignationPlayModeTests
{
    [Test]
    public void Spatial_designation_precedes_first_world_quarter_commit()
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

        TunnelNavigationVolume volume =
            (TunnelNavigationVolume)GetProperty(residents, "TunnelVolume");
        TunnelDepthExcavationPolicy policy = new TunnelDepthExcavationPolicy();
        TunnelDepthExcavationPlanResult planned = volume.Cells
            .Where(cell => cell.Z + 1 < volume.Depth)
            .Select(cell => policy.Plan(volume, cell))
            .First(value => value.Succeeded);
        TunnelDepthExcavationPlan plan = planned.Plan!;

        object result = Invoke(
            terrain,
            "DesignateSpatialExcavation",
            plan,
            residentModels,
            750,
            2L);

        Assert.That((bool)GetProperty(result, "IsSuccess"), Is.True);
        CellSnapshot target = Cell((WorldSnapshot)Invoke(world, "LoadSnapshot"), plan.Target);
        Assert.That(target.IsSolid, Is.True);
        Assert.That(target.State.Designation, Is.EqualTo(CellDesignation.Dig));

        Invoke(terrain, "SynchronizeDesignations", 3L, residentModels, 750);
        JobOverlayViewModel[] targetJobs =
            ((IEnumerable)Invoke(terrain, "LoadJobs"))
                .Cast<JobOverlayViewModel>()
                .Where(value => value.TargetX == plan.Target.X
                    && value.TargetY == plan.Target.Y
                    && value.TargetZ == plan.Target.Z
                    && value.Status != "Completed"
                    && value.Status != "Cancelled"
                    && value.Status != "Failed")
                .ToArray();
        Assert.That(targetJobs, Has.Length.EqualTo(1));

        Invoke(
            terrain,
            "BindExcavationSkillSource",
            new Func<EntityId, int>(_ => 100));
        EntityId worker = EntityId.Parse(residentModels[0].Id);
        ExcavationWorkTarget workTarget = new ExcavationWorkTarget(
            plan.Target,
            plan.Target.Z);
        bool progressed = false;
        for (long tick = 3; tick <= 90 && !progressed; tick += 3)
        {
            Invoke(
                terrain,
                "AdvanceExcavationQuarterWork",
                worker,
                workTarget,
                plan.WorkCell,
                tick);
            target = Cell((WorldSnapshot)Invoke(world, "LoadSnapshot"), plan.Target);
            progressed = !target.IsSolid
                || target.State.CompletedExcavationQuarters != ExcavationQuarter.None;
        }

        Assert.That(progressed, Is.True);
    }

    private static CellSnapshot Cell(WorldSnapshot world, CellId id)
    {
        return world.Chunks
            .SelectMany(chunk => chunk.Cells)
            .Single(value => value.Id == id);
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
