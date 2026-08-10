using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dig.Application.WorldObjects;
using Dig.Domain.Core;
using Dig.Domain.Jobs;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Dig.Domain.WorldObjects;
using Dig.Presentation.Agents;
using Dig.Presentation.World;
using NUnit.Framework;

namespace Dig.Unity.PlayModeTests
{

public sealed class BarrelAttackSurfacePlayModeTests
{
    [Test]
    public void Barrel_attack_requires_supported_route_and_supported_adjacent_work_cell()
    {
        Assembly runtime = typeof(DigWorldInteraction).Assembly;
        object world = InvokeStatic(
            RequireType(runtime, "Dig.Unity.DigWorldSession"),
            "CreateDemo",
            20,
            14,
            5);
        WorldViewModel worldView = (WorldViewModel)Invoke(world, "LoadView");
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
        Invoke(terrain, "InitializeBarrelDemo", 0L);

        Dictionary<CellId, WorldCellViewModel> cells = worldView.Chunks
            .SelectMany(chunk => chunk.Cells)
            .ToDictionary(value => new CellId(value.X, value.Y, value.Z));
        CellId supported = cells.Values
            .Where(value => !value.IsSolid)
            .Select(value => new CellId(value.X, value.Y, value.Z))
            .First(value => cells.TryGetValue(
                new CellId(value.X, value.Y + 1, value.Z),
                out WorldCellViewModel below)
                && below.HasFullActorSupport);
        CellId airborne = cells.Values
            .Where(value => !value.IsSolid)
            .Select(value => new CellId(value.X, value.Y, value.Z))
            .First(value => !cells.TryGetValue(
                    new CellId(value.X, value.Y + 1, value.Z),
                    out WorldCellViewModel below)
                || !below.HasFullActorSupport);

        NavigationSnapshot navigation = LoadNavigationSnapshot(terrain);
        Assert.That(
            (bool)Invoke(
                terrain,
                "IsSupportedBarrelAttackPath",
                navigation,
                SingleCellPath(navigation, supported)),
            Is.True);
        Assert.That(
            (bool)Invoke(
                terrain,
                "IsSupportedBarrelAttackPath",
                navigation,
                SingleCellPath(navigation, airborne)),
            Is.False);

        BarrelSnapshot[] barrels = ((IEnumerable)Invoke(terrain, "LoadBarrels"))
            .Cast<BarrelSnapshot>()
            .ToArray();
        CellId resolved = default;
        BarrelSnapshot? selected = null;
        foreach (BarrelSnapshot barrel in barrels)
        {
            foreach (AgentViewModel agent in agents)
            {
                object[] arguments =
                {
                    barrel.BarrelId,
                    new CellId(agent.CellX, agent.CellY, agent.CellZ),
                    default(CellId),
                };
                if ((bool)Invoke(terrain, "CanDirectAttackBarrel", arguments))
                {
                    selected = barrel;
                    resolved = (CellId)arguments[2];
                    break;
                }
            }

            if (selected != null)
            {
                break;
            }
        }

        Assert.That(selected, Is.Not.Null, "No supported demo barrel attack route was found.");
        Assert.That(
            Math.Abs(resolved.X - selected!.Cell.X)
                + Math.Abs(resolved.Y - selected.Cell.Y)
                + Math.Abs(resolved.Z - selected.Cell.Z),
            Is.EqualTo(1));
        CellId support = new CellId(resolved.X, resolved.Y + 1, resolved.Z);
        Assert.That(cells.TryGetValue(support, out WorldCellViewModel workSupport), Is.True);
        Assert.That(workSupport.HasFullActorSupport, Is.True);
    }

    [Test]
    public void Direct_barrel_order_replaces_existing_job_and_claims_selected_resident()
    {
        Assembly runtime = typeof(DigWorldInteraction).Assembly;
        object world = InvokeStatic(
            RequireType(runtime, "Dig.Unity.DigWorldSession"),
            "CreateDemo",
            20,
            14,
            5);
        WorldViewModel worldView = (WorldViewModel)Invoke(world, "LoadView");
        object journal = GetProperty(world, "Journal");
        object tunnel = Invoke(world, "CreateTunnelNavigationVolume");
        object residents = InvokeStatic(
            RequireType(runtime, "Dig.Unity.DigAgentSession"),
            "CreateDemo",
            worldView,
            tunnel,
            journal);
        AgentViewModel[] agents = ((IEnumerable)Invoke(residents, "LoadView"))
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
        Invoke(terrain, "InitializeBarrelDemo", 0L);

        object jobRepository = GetField(terrain, "_jobRepository");
        JobSystem jobs = (JobSystem)Invoke(jobRepository, "Get");
        BarrelSnapshot[] barrels = ((IEnumerable)Invoke(terrain, "LoadBarrels"))
            .Cast<BarrelSnapshot>()
            .ToArray();
        AgentViewModel? selectedAgent = null;
        BarrelSnapshot? selectedBarrel = null;
        foreach (AgentViewModel agent in agents)
        {
            EntityId agentId = EntityId.Parse(agent.Id);
            if (jobs.GetReservations().Any(value =>
                    value.Key == ReservationKey.ForAgent(agentId)))
            {
                continue;
            }

            foreach (BarrelSnapshot barrel in barrels)
            {
                object[] decision =
                {
                    barrel.BarrelId,
                    new CellId(agent.CellX, agent.CellY, agent.CellZ),
                    default(CellId),
                };
                if ((bool)Invoke(terrain, "CanDirectAttackBarrel", decision))
                {
                    selectedAgent = agent;
                    selectedBarrel = barrel;
                    break;
                }
            }

            if (selectedAgent != null)
            {
                break;
            }
        }

        Assert.That(selectedAgent, Is.Not.Null, "No unreserved resident can reach a demo barrel.");
        EntityId workerId = EntityId.Parse(selectedAgent!.Id);
        CellId workerCell = new CellId(
            selectedAgent.CellX,
            selectedAgent.CellY,
            selectedAgent.CellZ);
        EntityId existingJobId = EntityId.Parse("ce000000000000000000000000000001");
        Assert.That(jobs.Add(new DigJobDefinition(
            existingJobId,
            new DigJobTarget(workerCell),
            priority: 500,
            createdTick: 0,
            retryPolicy: JobRetryPolicy.Default)).IsSuccess, Is.True);
        Assert.That(jobs.MakeAvailable(existingJobId, tick: 0).IsSuccess, Is.True);
        Assert.That(jobs.Claim(existingJobId, workerId, tick: 0).IsSuccess, Is.True);
        Assert.That(jobs.Start(existingJobId, tick: 0).IsSuccess, Is.True);
        Invoke(jobRepository, "Save", jobs);

        Result started = (Result)Invoke(
            terrain,
            "StartDirectBarrelAttack",
            selectedBarrel!.BarrelId,
            workerId,
            workerCell,
            1L);

        Assert.That(started.IsSuccess, Is.True, started.Error?.ToString());
        JobSystem updated = (JobSystem)Invoke(jobRepository, "Get");
        JobSnapshot oldJob = updated.Get(existingJobId)!;
        Assert.That(oldJob.Status, Is.EqualTo(JobStatus.Available));
        Assert.That(oldJob.AssignedAgentId, Is.Null);
        JobSnapshot barrelJob = updated.GetAll().Single(value =>
            value.Definition is BarrelAttackJobDefinition
            && !value.IsTerminal
            && value.AssignedAgentId == workerId);
        Assert.That(updated.GetReservations(), Has.Some.Matches<ReservationSnapshot>(value =>
            value.JobId == barrelJob.Id
            && value.Key == ReservationKey.ForAgent(workerId)));
    }

    private static NavigationSnapshot LoadNavigationSnapshot(object terrain)
    {
        object repository = GetField(terrain, "_navigationRepository");
        object profile = GetField(terrain, "_profile");
        object map = Invoke(repository, "Get", GetProperty(profile, "Id"));
        object result = Invoke(map, "GetSnapshot");
        Assert.That((bool)GetProperty(result, "IsSuccess"), Is.True, result.ToString());
        return (NavigationSnapshot)GetProperty(result, "Value");
    }

    private static NavigationPath SingleCellPath(
        NavigationSnapshot navigation,
        CellId cell)
    {
        return new NavigationPath(
            navigation.Profile.Id,
            navigation.WorldVersion,
            navigation.NavigationVersion,
            navigation.LinkVersion,
            totalCost: 0,
            new[] { cell },
            Array.Empty<NavigationChunkStamp>());
    }

    private static Type RequireType(Assembly assembly, string name)
    {
        return assembly.GetType(name)
            ?? throw new TypeLoadException(name);
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
