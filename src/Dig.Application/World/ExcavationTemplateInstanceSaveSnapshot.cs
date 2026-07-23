using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.World;

namespace Dig.Application.World
{

public sealed class ExcavationTemplateCellSaveEntry
{
    public ExcavationTemplateCellSaveEntry(CellId cell, ExcavationTemplateCellState state)
    {
        if (!Enum.IsDefined(typeof(ExcavationTemplateCellState), state))
        {
            throw new ArgumentOutOfRangeException(nameof(state));
        }

        Cell = cell;
        State = state;
    }

    public CellId Cell { get; }
    public ExcavationTemplateCellState State { get; }
}

public sealed class ExcavationTemplateInstanceSaveSnapshot
{
    public const int CurrentFormatVersion = 1;

    public ExcavationTemplateInstanceSaveSnapshot(
        int formatVersion,
        string instanceId,
        string templateId,
        int templateVersion,
        CellId anchor,
        string orientationAxis,
        bool allowsMirror,
        string styleId,
        CellId leftEntrance,
        CellId rightEntrance,
        int requiredStoneworkUnits,
        int maximumStoneworkUnits,
        string? qualifyingResidentId,
        ExcavationTemplateLifecycleState lifecycleState,
        IEnumerable<ExcavationTemplateCellSaveEntry> cells)
    {
        if (formatVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(formatVersion));
        }

        if (string.IsNullOrWhiteSpace(instanceId))
        {
            throw new ArgumentException("Template instance id is required.", nameof(instanceId));
        }

        if (string.IsNullOrWhiteSpace(templateId))
        {
            throw new ArgumentException("Template definition id is required.", nameof(templateId));
        }

        if (templateVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(templateVersion));
        }

        if (string.IsNullOrWhiteSpace(orientationAxis))
        {
            throw new ArgumentException("Template orientation axis is required.", nameof(orientationAxis));
        }

        if (string.IsNullOrWhiteSpace(styleId))
        {
            throw new ArgumentException("Template style id is required.", nameof(styleId));
        }

        if (requiredStoneworkUnits < 0 || maximumStoneworkUnits < requiredStoneworkUnits)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumStoneworkUnits));
        }

        if (!Enum.IsDefined(typeof(ExcavationTemplateLifecycleState), lifecycleState))
        {
            throw new ArgumentOutOfRangeException(nameof(lifecycleState));
        }

        if (cells == null)
        {
            throw new ArgumentNullException(nameof(cells));
        }

        ExcavationTemplateCellSaveEntry[] ordered = cells.ToArray();
        if (ordered.Length == 0
            || ordered.Any(entry => entry == null)
            || ordered.Select(entry => entry.Cell).Distinct().Count() != ordered.Length)
        {
            throw new ArgumentException(
                "Template save progress must contain unique non-null cells.",
                nameof(cells));
        }

        FormatVersion = formatVersion;
        InstanceId = instanceId.Trim();
        TemplateId = templateId.Trim();
        TemplateVersion = templateVersion;
        Anchor = anchor;
        OrientationAxis = orientationAxis.Trim();
        AllowsMirror = allowsMirror;
        StyleId = styleId.Trim();
        LeftEntrance = leftEntrance;
        RightEntrance = rightEntrance;
        RequiredStoneworkUnits = requiredStoneworkUnits;
        MaximumStoneworkUnits = maximumStoneworkUnits;
        QualifyingResidentId = qualifyingResidentId;
        LifecycleState = lifecycleState;
        Cells = new ReadOnlyCollection<ExcavationTemplateCellSaveEntry>(
            ordered.OrderBy(entry => entry.Cell).ToArray());
    }

    public int FormatVersion { get; }
    public string InstanceId { get; }
    public string TemplateId { get; }
    public int TemplateVersion { get; }
    public CellId Anchor { get; }
    public string OrientationAxis { get; }
    public bool AllowsMirror { get; }
    public string StyleId { get; }
    public CellId LeftEntrance { get; }
    public CellId RightEntrance { get; }
    public int RequiredStoneworkUnits { get; }
    public int MaximumStoneworkUnits { get; }
    public string? QualifyingResidentId { get; }
    public ExcavationTemplateLifecycleState LifecycleState { get; }
    public IReadOnlyList<ExcavationTemplateCellSaveEntry> Cells { get; }

    public static ExcavationTemplateInstanceSaveSnapshot Capture(
        ExcavationTemplateInstance instance)
    {
        if (instance == null)
        {
            throw new ArgumentNullException(nameof(instance));
        }

        return new ExcavationTemplateInstanceSaveSnapshot(
            CurrentFormatVersion,
            instance.Id,
            instance.TemplateId,
            instance.TemplateVersion,
            instance.Anchor,
            instance.OrientationAxis,
            instance.AllowsMirror,
            instance.StyleId,
            instance.LeftEntrance.Cell,
            instance.RightEntrance.Cell,
            instance.Unlock.RequiredStoneworkUnits,
            instance.Unlock.MaximumStoneworkUnits,
            instance.Unlock.QualifyingResidentId,
            instance.LifecycleState,
            instance.CellProgress.Select(progress =>
                new ExcavationTemplateCellSaveEntry(progress.Cell, progress.State)));
    }

    public ExcavationTemplateInstance Restore()
    {
        if (FormatVersion != CurrentFormatVersion)
        {
            throw new InvalidOperationException(
                $"Unsupported excavation template save format version {FormatVersion}.");
        }

        CaveRoomPreset? preset = CaveRoomPresetCatalog.Definitions.SingleOrDefault(value =>
            string.Equals(value.Id, TemplateId, StringComparison.Ordinal)
            && value.Version == TemplateVersion);
        if (preset == null)
        {
            throw new InvalidOperationException(
                $"Unknown excavation template definition '{TemplateId}' version {TemplateVersion}.");
        }

        if (!string.Equals(OrientationAxis, preset.PassageAxis, StringComparison.Ordinal)
            || AllowsMirror != preset.AllowsMirror)
        {
            throw new InvalidOperationException(
                "Saved template orientation contract does not match the current definition.");
        }

        CellId[] mask = Cells.Select(entry => entry.Cell).OrderBy(cell => cell).ToArray();
        CaveRoomPlan plan = CaveRoomPlan.CreateSnapshot(
            preset,
            Anchor,
            mask.Where(cell => cell.Z == CellId.MinimumDepth),
            mask,
            Array.Empty<CellId>());
        CaveRoomTemplatePlacementUnlock unlock = new CaveRoomTemplatePlacementUnlock(
            TemplateId,
            TemplateVersion,
            RequiredStoneworkUnits,
            MaximumStoneworkUnits,
            QualifyingResidentId);
        ExcavationTemplateInstance restored = new ExcavationTemplateInstanceFactory().Create(
            InstanceId,
            plan,
            unlock,
            StyleId);

        if (restored.LeftEntrance.Cell != LeftEntrance
            || restored.RightEntrance.Cell != RightEntrance)
        {
            throw new InvalidOperationException(
                "Saved entrance provenance does not match the restored template mask.");
        }

        restored.RestoreState(Cells, LifecycleState);
        return restored;
    }
}

}
