using System.Linq;
using Dig.Presentation.Agents;
using UnityEngine;

namespace Dig.Unity
{
public sealed partial class DigGameHudCanvas
{
    private void BuildResidentSkillContext(ResidentRosterRowViewModel resident)
    {
        BeginBottomLayout(330f);
        RectTransform section = CreateSection(
            "Resident Skills",
            _bottomContent!,
            (resident.Name + " · SKILLS").ToUpperInvariant(),
            preferredWidth: 820f);
        BuildSkillInspector(section, resident.Skills);
        CreateButton(
            "Back To Inventory",
            section,
            "Back to inventory",
            HideSkillInspector,
            preferredHeight: 34f);
    }

    private void BuildSkillInspectorShortcut(string residentId)
    {
        RectTransform section = CreateSection(
            "Skills",
            _bottomContent!,
            "SKILLS",
            preferredWidth: 150f);
        CreateButton(
            "Open Skills",
            section,
            "All 12 skills",
            () => ShowSkillInspector(residentId),
            preferredHeight: 52f);
    }

    private void ShowSkillInspector(string residentId)
    {
        _skillInspectorResidentId = residentId;
        InvalidateAll();
    }

    private void HideSkillInspector()
    {
        _skillInspectorResidentId = null;
        InvalidateAll();
    }

    private static string BuildSkillSignature(ResidentSkillSetViewModel skills)
    {
        return string.Join(
            ",",
            skills.All.Select(skill => $"{skill.SkillId}:{skill.Level}"))
            + $":{skills.TotalCapacityUnits}:{skills.PrecisionVersion}";
    }
}
}
