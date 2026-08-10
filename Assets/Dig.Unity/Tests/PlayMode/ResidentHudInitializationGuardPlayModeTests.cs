using System.Reflection;
using Dig.Domain.Core;
using Dig.Domain.Society;
using Dig.Presentation.Agents;
using NUnit.Framework;
using UnityEngine;

namespace Dig.Unity.Tests
{

public sealed class ResidentHudInitializationGuardPlayModeTests
{
    private GameObject? _root;
    private DigAgentSimulationDriver? _driver;

    [SetUp]
    public void SetUp()
    {
        _root = new GameObject("Resident HUD Initialization Guard");
        _driver = _root.AddComponent<DigAgentSimulationDriver>();
    }

    [TearDown]
    public void TearDown()
    {
        if (_root != null)
        {
            Object.DestroyImmediate(_root);
        }
    }

    [Test]
    public void Uninitialized_driver_returns_unavailable_hud_projection_without_throwing()
    {
        object?[] scheduleArguments = { "resident", 0, 0, 0 };
        bool hasSchedule = (bool)Invoke(
            "TryGetResidentWorkWindow",
            scheduleArguments)!;
        Assert.That(hasSchedule, Is.False);
        Assert.That(scheduleArguments[1], Is.EqualTo(24));
        Assert.That(scheduleArguments[2], Is.EqualTo(0));
        Assert.That(scheduleArguments[3], Is.EqualTo(12));

        object?[] planningArguments = { "resident", false };
        bool hasPlanning = (bool)Invoke(
            "TryGetResidentAutomaticPlanning",
            planningArguments)!;
        Assert.That(hasPlanning, Is.False);
        Assert.That(planningArguments[1], Is.EqualTo(true));

        Result scheduleWrite = (Result)Invoke(
            "SetResidentWorkWindow",
            new object?[] { "resident", 0, 12 })!;
        Result planningWrite = (Result)Invoke(
            "SetResidentAutomaticPlanning",
            new object?[] { "resident", true })!;
        AssertTypedNotInitialized(scheduleWrite);
        AssertTypedNotInitialized(planningWrite);

        Assert.That(ReadProperty<bool>("IsHudReady"), Is.False);
        Assert.That(ReadProperty<long>("CurrentSocietyTick"), Is.Zero);

        SocietySnapshot society = (SocietySnapshot)Invoke(
            "LoadSocietySnapshot",
            System.Array.Empty<object?>())!;
        ResidentRosterViewModel roster = (ResidentRosterViewModel)Invoke(
            "LoadResidentRoster",
            new object?[] { null })!;
        Assert.That(society.Residents, Is.Empty);
        Assert.That(society.Bonds, Is.Empty);
        Assert.That(roster.Rows, Is.Empty);
        Assert.That(roster.SelectedResidentId, Is.Null);
    }

    private static void AssertTypedNotInitialized(Result result)
    {
        Assert.That(result.IsFailure, Is.True);
        Assert.That(
            result.Error!.Code,
            Is.EqualTo("unity.agent_simulation.not_initialized"));
    }

    private object? Invoke(string methodName, object?[] arguments)
    {
        MethodInfo? method = typeof(DigAgentSimulationDriverBase).GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, methodName);
        return method!.Invoke(_driver, arguments);
    }

    private T ReadProperty<T>(string propertyName)
    {
        PropertyInfo? property = typeof(DigAgentSimulationDriverBase).GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(property, Is.Not.Null, propertyName);
        return (T)property!.GetValue(_driver)!;
    }
}

}