using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Dig.Domain.Core;
using Dig.Domain.World;
using Dig.Presentation.Agents;
using Dig.Presentation.World;
using NUnit.Framework;

namespace Dig.Unity.Tests
{
public sealed class MushroomWorkPositionPlayModeTests
{
    [Test]
    public void Work_position_uses_supported_depth_cell_when_side_cells_are_void()
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
        AgentViewModel worker = ((IEnumerable)Invoke(residents, "LoadView"))
            .Cast<AgentViewModel>()
            .First();
        object terrain = InvokeStatic(
            RequireType(runtime, "Dig.Unity.DigTerrainWorkSession"),
            "CreateDemo",
            world,
            new[] { worker },
            journal,
            GetProperty(residents, "SkillGrants"));

        var cells = worldView.Chunks
            .SelectMany(chunk => chunk.Cells)
            .ToArray();
        MethodInfo resolver = terrain.GetType().GetMethod(
            "TryResolveMushroomWorkPosition",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        CellId workerCell = new CellId(worker.CellX, worker.CellY, worker.CellZ);
        CellId? chosenTarget = null;
        CellId chosenWork = default;
        foreach (CellId target in cells
            .Where(value => !value.IsSolid)
            .Select(value => new CellId(value.X, value.Y, value.Z))
            .OrderBy(value => value))
        {
            bool sideSupported =
                HasFullSupport(worldView, new CellId(target.X - 1, target.Y, target.Z))
                || HasFullSupport(worldView, new CellId(target.X + 1, target.Y, target.Z));
            bool depthSupported =
                (target.Z > CellId.MinimumDepth
                    && HasFullSupport(
                        worldView,
                        new CellId(target.X, target.Y, target.Z - 1)))
                || (target.Z < CellId.MaximumDepth
                    && HasFullSupport(
                        worldView,
                        new CellId(target.X, target.Y, target.Z + 1)));
            if (sideSupported || !depthSupported)
            {
                continue;
            }

            object[] arguments = { target, workerCell, default(CellId) };
            if (!(bool)resolver.Invoke(terrain, arguments)!)
            {
                continue;
            }

            CellId work = (CellId)arguments[2];
            if (work.X == target.X
                && work.Y == target.Y
                && Math.Abs(work.Z - target.Z) == 1
                && HasFullSupport(worldView, work))
            {
                chosenTarget = target;
                chosenWork = work;
                break;
            }
        }

        Assert.That(
            chosenTarget.HasValue,
            Is.True,
            "The demo must expose a side-void/depth-supported action-position case.");
        Assert.That(chosenWork.Y, Is.EqualTo(chosenTarget!.Value.Y));
        Assert.That(chosenWork.X, Is.EqualTo(chosenTarget.Value.X));
        Assert.That(Math.Abs(chosenWork.Z - chosenTarget.Value.Z), Is.EqualTo(1));
        Assert.That(HasFullSupport(worldView, chosenWork), Is.True);
    }

    private static bool HasFullSupport(
        WorldViewModel worldView,
        CellId actionCell)
    {
        CellId support = new CellId(
            actionCell.X,
            actionCell.Y + 1,
            actionCell.Z);
        return worldView.Chunks
            .SelectMany(chunk => chunk.Cells)
            .Any(value => value.X == support.X
                && value.Y == support.Y
                && value.Z == support.Z
                && value.HasFullActorSupport);
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
