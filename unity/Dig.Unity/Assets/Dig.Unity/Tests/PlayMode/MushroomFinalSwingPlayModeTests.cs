using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dig.Domain.Core;
using Dig.Domain.Ecology;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Presentation.Agents;
using Dig.Presentation.Inventory;
using NUnit.Framework;

namespace Dig.Unity.Tests
{

public sealed class MushroomFinalSwingPlayModeTests
{
    [Test]
    public void Final_runtime_swing_commits_absent_site_completed_job_and_exact_drops()
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
        EntityId jobId = site.ActiveChopJobId!.Value;
        object jobs = GetField(terrain, "_jobRepository");
        JobSnapshot job = LoadJob(jobs, jobId);
        MushroomChopJobDefinition definition =
            (MushroomChopJobDefinition)job.Definition;

        AssertSuccess(Invoke(terrain, "AdvanceMushroomJob", job, definition, 5L));
        for (int index = 0; index < site.RequiredSwings; index++)
        {
            job = LoadJob(jobs, jobId);
            AssertSuccess(Invoke(
                terrain,
                "AdvanceMushroomJob",
                job,
                definition,
                6L + index));
        }

        JobSnapshot completed = LoadJob(jobs, jobId);
        Assert.That(completed.Status, Is.EqualTo(JobStatus.Completed));
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
    }

    private static JobSnapshot LoadJob(object repository, EntityId jobId)
    {
        object jobs = Invoke(repository, "Get");
        object value = Invoke(jobs, "Get", jobId);
        Assert.That(value, Is.Not.Null);
        return (JobSnapshot)value;
    }

    private static void AssertSuccess(object result)
    {
        Assert.That((bool)GetProperty(result, "IsSuccess"), Is.True, result.ToString());
    }

    private static Type RequireType(Assembly assembly, string name)
    {
        return assembly.GetType(name) ?? throw new TypeLoadException(name);
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
        return type.GetMethods(flags).SingleOrDefault(value => value.Name == name
                && value.GetParameters().Length == argumentCount)
            ?? throw new MissingMethodException(type.FullName, name);
    }

    private static object GetField(object target, string name)
    {
        FieldInfo field = target.GetType().GetField(
            name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(target.GetType().FullName, name);
        return field.GetValue(target)!;
    }

    private static object GetProperty(object target, string name)
    {
        PropertyInfo property = target.GetType().GetProperty(
            name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new MissingMemberException(target.GetType().FullName, name);
        return property.GetValue(target)!;
    }
}

}
