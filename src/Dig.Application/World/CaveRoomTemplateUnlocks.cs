using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Dig.Application.World
{

public sealed class CaveRoomTemplateCandidate
{
    public CaveRoomTemplateCandidate(
        string residentId,
        int stonework,
        bool isEligible)
    {
        if (string.IsNullOrWhiteSpace(residentId))
        {
            throw new ArgumentException("Resident id is required.", nameof(residentId));
        }

        if (stonework < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stonework));
        }

        ResidentId = residentId.Trim();
        Stonework = stonework;
        IsEligible = isEligible;
    }

    public string ResidentId { get; }
    public int Stonework { get; }
    public bool IsEligible { get; }
}

public sealed class CaveRoomTemplateUnlockState
{
    internal CaveRoomTemplateUnlockState(
        CaveRoomPreset preset,
        bool isUnlocked,
        int maximumStonework,
        string? qualifyingResidentId,
        string reason)
    {
        Preset = preset ?? throw new ArgumentNullException(nameof(preset));
        IsUnlocked = isUnlocked;
        MaximumStonework = maximumStonework;
        QualifyingResidentId = qualifyingResidentId;
        Reason = reason ?? throw new ArgumentNullException(nameof(reason));
    }

    public CaveRoomPreset Preset { get; }
    public bool IsUnlocked { get; }
    public int MaximumStonework { get; }
    public string? QualifyingResidentId { get; }
    public string Reason { get; }
}

public sealed class CaveRoomTemplateUnlockSnapshot
{
    internal CaveRoomTemplateUnlockSnapshot(
        int maximumStonework,
        string? qualifyingResidentId,
        IReadOnlyList<CaveRoomTemplateUnlockState> templates)
    {
        MaximumStonework = maximumStonework;
        QualifyingResidentId = qualifyingResidentId;
        Templates = templates;
    }

    public int MaximumStonework { get; }
    public string? QualifyingResidentId { get; }
    public IReadOnlyList<CaveRoomTemplateUnlockState> Templates { get; }

    public CaveRoomTemplateUnlockState Get(CaveRoomPresetKind kind)
    {
        return Templates.Single(value => value.Preset.Kind == kind);
    }
}

public sealed class CaveRoomTemplateUnlockEvaluator
{
    public CaveRoomTemplateUnlockSnapshot Evaluate(
        IEnumerable<CaveRoomTemplateCandidate> candidates)
    {
        if (candidates == null)
        {
            throw new ArgumentNullException(nameof(candidates));
        }

        CaveRoomTemplateCandidate? qualifying = candidates
            .Where(candidate => candidate != null && candidate.IsEligible)
            .OrderByDescending(candidate => candidate.Stonework)
            .ThenBy(candidate => candidate.ResidentId, StringComparer.Ordinal)
            .FirstOrDefault();

        int maximumStonework = qualifying?.Stonework ?? 0;
        string? residentId = qualifying?.ResidentId;
        CaveRoomTemplateUnlockState[] states = CaveRoomPresetCatalog.Definitions
            .OrderBy(preset => preset.Kind)
            .Select(preset => CreateState(preset, maximumStonework, residentId))
            .ToArray();

        return new CaveRoomTemplateUnlockSnapshot(
            maximumStonework,
            residentId,
            new ReadOnlyCollection<CaveRoomTemplateUnlockState>(states));
    }

    private static CaveRoomTemplateUnlockState CreateState(
        CaveRoomPreset preset,
        int maximumStonework,
        string? residentId)
    {
        bool unlocked = maximumStonework >= preset.RequiredStonework;
        string reason = unlocked
            ? preset.RequiredStonework == 0
                ? "Доступно с начала игры."
                : $"Доступно: Камень {maximumStonework} из {preset.RequiredStonework}."
            : $"Требуется Камень {preset.RequiredStonework}; сейчас максимум {maximumStonework}.";

        return new CaveRoomTemplateUnlockState(
            preset,
            unlocked,
            maximumStonework,
            residentId,
            reason);
    }
}

public sealed class CaveRoomTemplatePlacementUnlock
{
    public CaveRoomTemplatePlacementUnlock(
        string templateId,
        int templateVersion,
        int requiredStonework,
        int maximumStonework,
        string? qualifyingResidentId)
    {
        if (string.IsNullOrWhiteSpace(templateId))
        {
            throw new ArgumentException("Template id is required.", nameof(templateId));
        }

        if (templateVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(templateVersion));
        }

        if (requiredStonework < 0 || maximumStonework < requiredStonework)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumStonework));
        }

        TemplateId = templateId.Trim();
        TemplateVersion = templateVersion;
        RequiredStonework = requiredStonework;
        MaximumStonework = maximumStonework;
        QualifyingResidentId = qualifyingResidentId;
    }

    public string TemplateId { get; }
    public int TemplateVersion { get; }
    public int RequiredStonework { get; }
    public int MaximumStonework { get; }
    public string? QualifyingResidentId { get; }

    public static CaveRoomTemplatePlacementUnlock Capture(
        CaveRoomTemplateUnlockState state)
    {
        if (state == null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        if (!state.IsUnlocked)
        {
            throw new InvalidOperationException("Locked templates cannot be placed.");
        }

        return new CaveRoomTemplatePlacementUnlock(
            state.Preset.Id,
            state.Preset.Version,
            state.Preset.RequiredStonework,
            state.MaximumStonework,
            state.QualifyingResidentId);
    }
}

}
