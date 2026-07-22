using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dig.Domain.Agents;
using Dig.Presentation.Agents;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Dig.Unity.Tests
{

public sealed class SkillInspectorPlayModeTests
{
    private GameObject? _root;

    [TearDown]
    public void TearDown()
    {
        if (_root != null)
        {
            UnityEngine.Object.DestroyImmediate(_root);
            _root = null;
        }
    }

    [Test]
    public void Inspector_renders_twelve_skills_capacity_thresholds_and_gradient()
    {
        ResidentSkillSetViewModel skills = CreateSkills(
            new Dictionary<AgentSkillId, int>
            {
                [AgentSkillCatalog.Stonework] =
                    AgentSkillCatalog.IndividualMaximumUnits,
            },
            AgentSkillCatalog.UniversityCapacityUnits);
        Transform parent = CreateRoot();

        BuildInspector(parent, skills);

        Assert.That(parent.childCount, Is.EqualTo(14));
        Text capacity = RequireChild(parent, "Skill Capacity").GetComponent<Text>();
        StringAssert.Contains("Capacity 100 / 200", capacity.text);
        StringAssert.Contains("University 100/100", capacity.text);
        foreach (AgentSkillDefinition definition in AgentSkillCatalog.All)
        {
            Transform row = RequireChild(parent, "Skill " + definition.Id);
            Assert.That(
                RequireChild(row, "Name").GetComponent<Text>().text,
                Is.EqualTo(definition.Id.ToString()));
        }

        AssertFill(
            parent,
            AgentSkillCatalog.Stonework,
            expectedProgress: 1f,
            new Color(0.18f, 0.78f, 0.30f, 1f));
        AssertFill(
            parent,
            AgentSkillCatalog.Cooking,
            expectedProgress: 0f,
            new Color(0.05f, 0.14f, 0.42f, 1f));
        Text report = RequireChild(parent, "Skill Report").GetComponent<Text>();
        StringAssert.Contains("Stonework thresholds 20/40/60", report.text);
    }

    [Test]
    public void Top_five_uses_value_then_stable_id_from_same_snapshot()
    {
        ResidentSkillSetViewModel skills = CreateSkills(
            new Dictionary<AgentSkillId, int>
            {
                [AgentSkillCatalog.Stonework] = 2_000,
                [AgentSkillCatalog.Cooking] = 1_500,
                [AgentSkillCatalog.Woodworking] = 1_500,
                [AgentSkillCatalog.Alchemy] = 1_000,
                [AgentSkillCatalog.Defense] = 1_000,
                [AgentSkillCatalog.Logistics] = 500,
            },
            AgentSkillCatalog.BaseCapacityUnits);

        Assert.That(
            skills.TopFive.Select(value => value.SkillId),
            Is.EqualTo(new[]
            {
                AgentSkillCatalog.Stonework.ToString(),
                AgentSkillCatalog.Cooking.ToString(),
                AgentSkillCatalog.Woodworking.ToString(),
                AgentSkillCatalog.Alchemy.ToString(),
                AgentSkillCatalog.Defense.ToString(),
            }));
        Assert.That(skills.TopFive.All(value => skills.All.Contains(value)), Is.True);
    }

    [Test]
    public void Roster_renders_exactly_five_highest_skills_in_snapshot_order()
    {
        ResidentSkillSetViewModel skills = CreateSkills(
            new Dictionary<AgentSkillId, int>
            {
                [AgentSkillCatalog.Stonework] = 2_000,
                [AgentSkillCatalog.Cooking] = 1_500,
                [AgentSkillCatalog.Woodworking] = 1_500,
                [AgentSkillCatalog.Alchemy] = 1_000,
                [AgentSkillCatalog.Defense] = 1_000,
                [AgentSkillCatalog.Logistics] = 500,
            },
            AgentSkillCatalog.BaseCapacityUnits);
        Transform parent = CreateRoot();

        BuildTopSkills(parent, skills);

        Assert.That(parent.childCount, Is.EqualTo(6));
        Assert.That(parent.GetChild(0).name, Is.EqualTo("Top Skills"));
        Assert.That(
            parent.Cast<Transform>().Skip(1).Select(child => child.name),
            Is.EqualTo(skills.TopFive.Select(value => "Top Skill " + value.SkillId)));
    }

    [Test]
    public void Roster_hides_zero_skills_and_omits_empty_top_skill_section()
    {
        ResidentSkillSetViewModel skills = CreateSkills(
            new Dictionary<AgentSkillId, int>
            {
                [AgentSkillCatalog.Stonework] = 2_000,
                [AgentSkillCatalog.Logistics] = 500,
            },
            AgentSkillCatalog.BaseCapacityUnits);
        Transform parent = CreateRoot();

        BuildTopSkills(parent, skills);

        Assert.That(parent.childCount, Is.EqualTo(3));
        Assert.That(
            parent.Cast<Transform>().Skip(1).Select(child => child.name),
            Is.EqualTo(new[]
            {
                "Top Skill " + AgentSkillCatalog.Stonework,
                "Top Skill " + AgentSkillCatalog.Logistics,
            }));
        Assert.That(skills.TopFive.All(value => value.Level > 0), Is.True);

        UnityEngine.Object.DestroyImmediate(_root);
        _root = null;
        parent = CreateRoot();
        BuildTopSkills(parent, CreateSkills(
            new Dictionary<AgentSkillId, int>(),
            AgentSkillCatalog.BaseCapacityUnits));

        Assert.That(parent.childCount, Is.EqualTo(0));
    }

    private Transform CreateRoot()
    {
        _root = new GameObject("Skill Inspector Test Root", typeof(RectTransform));
        return _root.transform;
    }

    private static ResidentSkillSetViewModel CreateSkills(
        IReadOnlyDictionary<AgentSkillId, int> levels,
        int capacityUnits)
    {
        return new ResidentSkillSetViewModel(
            AgentSkillCatalog.All.Select(definition => new ResidentSkillViewModel(
                definition.Id.ToString(),
                levels.TryGetValue(definition.Id, out int level) ? level : 0)),
            capacityUnits,
            AgentSkillCatalog.PrecisionVersion,
            lastReport: null);
    }

    private static void BuildInspector(
        Transform parent,
        ResidentSkillSetViewModel skills)
    {
        MethodInfo? method = typeof(DigGameHudCanvas).GetMethod(
            "BuildSkillInspector",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(method, Is.Not.Null);
        method!.Invoke(null, new object[] { parent, skills });
    }

    private static void BuildTopSkills(
        Transform parent,
        ResidentSkillSetViewModel skills)
    {
        MethodInfo? method = typeof(DigGameHudCanvas).GetMethod(
            "BuildTopSkillList",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(method, Is.Not.Null);
        method!.Invoke(null, new object[] { parent, skills });
    }

    private static void AssertFill(
        Transform parent,
        AgentSkillId skillId,
        float expectedProgress,
        Color expectedColor)
    {
        Transform row = RequireChild(parent, "Skill " + skillId);
        Transform fill = RequireChild(RequireChild(row, "Bar"), "Fill");
        Assert.That(fill.GetComponent<RectTransform>().anchorMax.x,
            Is.EqualTo(expectedProgress).Within(0.0001f));
        Color actual = fill.GetComponent<Image>().color;
        Assert.That(actual.r, Is.EqualTo(expectedColor.r).Within(0.0001f));
        Assert.That(actual.g, Is.EqualTo(expectedColor.g).Within(0.0001f));
        Assert.That(actual.b, Is.EqualTo(expectedColor.b).Within(0.0001f));
        Assert.That(actual.a, Is.EqualTo(expectedColor.a).Within(0.0001f));
    }

    private static Transform RequireChild(Transform parent, string name)
    {
        Transform? child = parent.Find(name);
        Assert.That(child, Is.Not.Null, name);
        return child!;
    }
}

}
