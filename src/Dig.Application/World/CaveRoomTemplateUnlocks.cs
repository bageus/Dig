using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Dig.Application.World
{

public sealed class CaveRoomTemplateCandidate
{
    public CaveRoomTemplateCandidate(string residentId, int stoneworkUnits, bool isEligible)
    {
        if (string.IsNullOrWhiteSpace(residentId))
        {
            throw new ArgumentException("Resident id is required.", nameof(residentId));
        }

        if (stoneworkUnits < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stoneworkUnits));
        }

        ResidentId = residentId.Trim();
        StoneworkUnits = stoneworkUnits;
        IsEligible = isEligible;
    }

    public string ResidentId { get; }
    public int StoneworkUnits { get; }
    public bool IsEligible { get; }
}

public sealed class CaveRoomTemplateUnlockState
{
    internal CaveRoomTemplateUnlockState(
        CaveRoomPreset preset,
        bool isUnlocked,
        int maximumStoneworkUnits,
        string? qualifyingResidentId,
        string reason)
    {
        Preset = preset ?? throw new ArgumentNullException(nameof(preset));
        IsUnlocked = isUnlocked;
        MaximumStoneworkUnits = maximumStoneworkUnits;
        QualifyingResidentId = qualifyingResidentId;
        Reason = reason ?? throw new ArgumentNullException(nameof(reason));
    }

    public CaveRoomPreset Preset { get; }
    public bool IsUnlocked { get; }
    public int MaximumStoneworkUnits { get; }
    public string? QualifyingResidentId { get; }
    public string Reason { get; }
}

public sealed class CaveRoomTemplateUnlockSnapshot
{
    internal CaveRoomTemplateUnlockSnapshot(
        int maximumStoneworkUnits,
        string? qualifyingResidentId,
        IReadOnlyList<CaveRoomTemplateUnlockState> templates)
    {
        MaximumStoneworkUnits = maximumStoneworkUnits;
        QualifyingResidentId = qualifyingResidentId;
        Templates = templates;
    }

    public int MaximumStoneworkUnits { get; }
    public string? QualifyingResidentId { get; }
    public IReadOnlyList<CaveRoomTemplateUnlockState> Templates { get; }

    public CaveRoomTemplateUnlockState Get(CaveRoomPresetKind kind) =>
        Templates.Single(value => value.Preset.Kind == kind);
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
            .OrderByDescending(candidate => candidate.StoneworkUnits)
            .ThenBy(candidate => candidate.ResidentId, StringComparer.Ordinal)
            .FirstOrDefault();

        int maximum = qualifying?.StoneworkUnits ?? 0;
        string? residentId = qualifying?.ResidentId;
        CaveRoomTemplateUnlockState[] states = CaveRoomPresetCatalog.Definitions
            .OrderBy(preset => preset.Kind)
            .Select(preset => CreateState(preset, maximum, residentId))
            .ToArray();

        return new CaveRoomTemplateUnlockSnapshot(
            maximum,
            residentId,
            new ReadOnlyCollection<CaveRoomTemplateUnlockState>(states));
    }

    private static CaveRoomTemplateUnlockState CreateState(
        CaveRoomPreset preset,
        int maximum,
        string? residentId)
    {
        bool unlocked = maximum >= preset.RequiredStoneworkUnits;
        string reason = unlocked
            ? preset.RequiredStoneworkUnits == 0
                ? "Доступно с начала игры."
                : $"Доступно: Камень {maximum / 100} из {preset.RequiredStoneworkUnits / 100}."
            : $"Требуется Камень {preset.RequiredStoneworkUnits / 100}; сейчас максимум {maximum / 100}.";

        return new CaveRoomTemplateUnlockState(
            preset,
            unlocked,
            maximum,
            residentId,
            reason);
    }
}

public sealed class CaveRoomTemplatePlacementUnlock
{
    public CaveRoomTemplatePlacementUnlock(
        string templateId,
        int templateVersion,
        int requiredStoneworkUnits,
        int maximumStoneworkUnits,
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

        if (requiredStoneworkUnits < 0 || maximumStoneworkUnits < requiredStoneworkUnits)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumStoneworkUnits));
        }

        TemplateId = templateId.Trim();
        TemplateVersion = templateVersion;
        RequiredStoneworkUnits = requiredStoneworkUnits;
        MaximumStoneworkUnits = maximumStoneworkUnits;
        QualifyingResidentId = qualifyingResidentId;
    }

    public string TemplateId { get; }
    public int TemplateVersion { get; }
    public int RequiredStoneworkUnits { get; }
    public int MaximumStoneworkUnits { get; }
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
            state.Preset.RequiredStoneworkUnits,
            state.MaximumStoneworkUnits,
            state.QualifyingResidentId);
    }
}

}
