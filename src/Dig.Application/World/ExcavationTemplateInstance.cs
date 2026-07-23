using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.World;

namespace Dig.Application.World
{

public enum ExcavationTemplateLifecycleState
{
    Active = 0,
    Completed = 1,
    Cancelled = 2,
}

public enum ExcavationTemplateCellState
{
    Pending = 0,
    Excavated = 1,
    Cancelled = 2,
}

public readonly struct ExcavationTemplateEntrance
{
    public ExcavationTemplateEntrance(CaveRoomEntranceSide side, CellId cell)
    {
        Side = side;
        Cell = cell;
    }

    public CaveRoomEntranceSide Side { get; }
    public CellId Cell { get; }
}

public sealed class ExcavationTemplateCellProgress
{
    internal ExcavationTemplateCellProgress(CellId cell)
    {
        Cell = cell;
        State = ExcavationTemplateCellState.Pending;
    }

    public CellId Cell { get; }
    public ExcavationTemplateCellState State { get; internal set; }
}

public sealed class ExcavationTemplateInstance
{
    private readonly Dictionary<CellId, ExcavationTemplateCellProgress> _progress;
    private readonly IReadOnlyList<CellId> _orderedMask;
    private readonly IReadOnlyList<ExcavationTemplateCellProgress> _orderedProgress;

    internal ExcavationTemplateInstance(
        string id,
        CaveRoomPlan plan,
        CaveRoomTemplatePlacementUnlock unlock,
        string styleId)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Template instance id is required.", nameof(id));
        }

        if (plan == null)
        {
            throw new ArgumentNullException(nameof(plan));
        }

        if (unlock == null)
        {
            throw new ArgumentNullException(nameof(unlock));
        }

        if (unlock.TemplateId != plan.Preset.Id
            || unlock.TemplateVersion != plan.Preset.Version)
        {
            throw new ArgumentException(
                "Unlock snapshot does not match the template definition.",
                nameof(unlock));
        }

        if (string.IsNullOrWhiteSpace(styleId))
        {
            throw new ArgumentException("Template style id is required.", nameof(styleId));
        }

        CellId[] mask = plan.VolumeCells.OrderBy(cell => cell).ToArray();
        if (mask.Length == 0 || mask.Distinct().Count() != mask.Length)
        {
            throw new ArgumentException(
                "Template mask must contain unique cells.",
                nameof(plan));
        }

        if (mask.Any(cell => cell.Z < CellId.MinimumDepth || cell.Z > CellId.MaximumDepth))
        {
            throw new ArgumentException(
                "Template mask contains an invalid world depth.",
                nameof(plan));
        }

        CellId[] entranceRow = mask
            .Where(cell => cell.Z == CellId.MinimumDepth && cell.Y == plan.Entrance.Y)
            .OrderBy(cell => cell.X)
            .ToArray();
        if (entranceRow.Length < 2)
        {
            throw new ArgumentException(
                "Template front slice must contain left and right X entrances.",
                nameof(plan));
        }

        Id = id.Trim();
        TemplateId = plan.Preset.Id;
        TemplateVersion = plan.Preset.Version;
        Anchor = plan.Entrance;
        OrientationAxis = plan.Preset.PassageAxis;
        AllowsMirror = plan.Preset.AllowsMirror;
        Unlock = unlock;
        StyleId = styleId.Trim();
        LeftEntrance = new ExcavationTemplateEntrance(
            CaveRoomEntranceSide.Left,
            entranceRow[0]);
        RightEntrance = new ExcavationTemplateEntrance(
            CaveRoomEntranceSide.Right,
            entranceRow[entranceRow.Length - 1]);
        _orderedMask = new ReadOnlyCollection<CellId>(mask);
        _progress = mask.ToDictionary(
            cell => cell,
            cell => new ExcavationTemplateCellProgress(cell));
        _orderedProgress = new ReadOnlyCollection<ExcavationTemplateCellProgress>(
            mask.Select(cell => _progress[cell]).ToArray());
        LifecycleState = ExcavationTemplateLifecycleState.Active;
    }

    public string Id { get; }
    public string TemplateId { get; }
    public int TemplateVersion { get; }
    public CellId Anchor { get; }
    public string OrientationAxis { get; }
    public bool AllowsMirror { get; }
    public CaveRoomTemplatePlacementUnlock Unlock { get; }
    public string StyleId { get; }
    public ExcavationTemplateEntrance LeftEntrance { get; }
    public ExcavationTemplateEntrance RightEntrance { get; }
    public IReadOnlyList<CellId> OrderedMask => _orderedMask;
    public IReadOnlyList<ExcavationTemplateCellProgress> CellProgress => _orderedProgress;
    public ExcavationTemplateLifecycleState LifecycleState { get; private set; }
    public int ExcavatedCellCount =>
        _orderedProgress.Count(value => value.State == ExcavationTemplateCellState.Excavated);
    public int TargetCellCount => _orderedMask.Count;
    public double Progress => TargetCellCount == 0
        ? 0d
        : ExcavatedCellCount / (double)TargetCellCount;

    public bool MarkExcavated(CellId cell)
    {
        if (!_progress.TryGetValue(cell, out ExcavationTemplateCellProgress? progress))
        {
            return false;
        }

        if (progress.State == ExcavationTemplateCellState.Excavated)
        {
            return false;
        }

        progress.State = ExcavationTemplateCellState.Excavated;
        if (_orderedProgress.All(value =>
                value.State != ExcavationTemplateCellState.Pending))
        {
            LifecycleState = ExcavationTemplateLifecycleState.Completed;
        }

        return true;
    }

    public void CancelPending()
    {
        foreach (ExcavationTemplateCellProgress progress in _orderedProgress)
        {
            if (progress.State == ExcavationTemplateCellState.Pending)
            {
                progress.State = ExcavationTemplateCellState.Cancelled;
            }
        }

        LifecycleState = ExcavationTemplateLifecycleState.Cancelled;
    }

    internal void RestoreState(
        IEnumerable<ExcavationTemplateCellSaveEntry> savedProgress,
        ExcavationTemplateLifecycleState lifecycleState)
    {
        if (savedProgress == null)
        {
            throw new ArgumentNullException(nameof(savedProgress));
        }

        ExcavationTemplateCellSaveEntry[] entries = savedProgress.ToArray();
        if (entries.Length != _orderedMask.Count
            || entries.Any(entry => entry == null)
            || entries.Select(entry => entry.Cell).Distinct().Count() != entries.Length
            || entries.Any(entry => !_progress.ContainsKey(entry.Cell)))
        {
            throw new ArgumentException(
                "Saved template progress must contain every target cell exactly once.",
                nameof(savedProgress));
        }

        foreach (ExcavationTemplateCellSaveEntry entry in entries)
        {
            _progress[entry.Cell].State = entry.State;
        }

        bool hasPending = _orderedProgress.Any(value => value.State == ExcavationTemplateCellState.Pending);
        bool hasCancelled = _orderedProgress.Any(value => value.State == ExcavationTemplateCellState.Cancelled);
        bool allExcavated = _orderedProgress.All(value => value.State == ExcavationTemplateCellState.Excavated);
        bool validLifecycle = lifecycleState switch
        {
            ExcavationTemplateLifecycleState.Active => hasPending && !hasCancelled,
            ExcavationTemplateLifecycleState.Completed => allExcavated,
            ExcavationTemplateLifecycleState.Cancelled => !hasPending && hasCancelled,
            _ => false,
        };
        if (!validLifecycle)
        {
            throw new ArgumentException(
                "Saved template lifecycle is inconsistent with per-cell progress.",
                nameof(lifecycleState));
        }

        LifecycleState = lifecycleState;
    }
}

}
