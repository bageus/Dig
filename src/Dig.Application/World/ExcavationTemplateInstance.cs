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

        CellId[] mask = plan.VolumeCells
            .OrderBy(cell => cell)
            .ToArray();
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
}

public sealed class ExcavationTemplateInstanceFactory
{
    public ExcavationTemplateInstance Create(
        string instanceId,
        CaveRoomPlan plan,
        CaveRoomTemplatePlacementUnlock unlock,
        string styleId)
    {
        return new ExcavationTemplateInstance(
            instanceId,
            plan,
            unlock,
            styleId);
    }
}

}
