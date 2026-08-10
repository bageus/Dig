using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Dig.Domain.Core;
using NUnit.Framework;

namespace Dig.Unity.Tests
{
public sealed class Issue14AttackPlayModeTests
{
    [Test]
    public void Player_attack_adapter_issues_authoritative_combat_intent()
    {
        Assembly runtime = typeof(DigWorldInteraction).Assembly;
        Type worldType = RequireType(runtime, "Dig.Unity.DigWorldSession");
        object world = InvokeStatic(worldType, "CreateDemo", 8, 8, 4);
        object worldView = Invoke(world, "LoadView");
        object journal = GetProperty(world, "Journal");
        Type agentType = RequireType(runtime, "Dig.Unity.DigAgentSession");
        object agents = InvokeStatic(agentType, "CreateDemo", worldView, journal);
        IEnumerable models = (IEnumerable)Invoke(agents, "LoadView");
        object actorModel = models.Cast<object>().First();
        EntityId actorId = EntityId.Parse((string)GetProperty(actorModel, "Id"));
        EntityId targetId = EntityId.Parse("f0000000000000000000000000000001");

        object result = Invoke(agents, "IssuePlayerAttackOrder", actorId, targetId);

        Assert.That((bool)GetProperty(result, "IsSuccess"), Is.True);
        object intent = GetProperty(result, "Value");
        Assert.That(GetProperty(intent, "Source").ToString(), Is.EqualTo("PlayerOrder"));
        Assert.That(GetProperty(intent, "Kind").ToString(), Is.EqualTo("Attack"));
        Assert.That(GetProperty(intent, "TargetEntityId").ToString(),
            Does.Contain(targetId.ToString()));
    }

    private static Type RequireType(Assembly assembly, string name)
    {
        Type? type = assembly.GetType(name);
        Assert.That(type, Is.Not.Null, name);
        return type!;
    }

    private static object InvokeStatic(Type type, string name, params object[] arguments)
    {
        MethodInfo? method = type.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .FirstOrDefault(value => value.Name == name
                && value.GetParameters().Length == arguments.Length);
        Assert.That(method, Is.Not.Null, name);
        return method!.Invoke(null, arguments)!;
    }

    private static object Invoke(object target, string name, params object[] arguments)
    {
        MethodInfo? method = target.GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .FirstOrDefault(value => value.Name == name
                && value.GetParameters().Length == arguments.Length);
        Assert.That(method, Is.Not.Null, name);
        return method!.Invoke(target, arguments)!;
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
