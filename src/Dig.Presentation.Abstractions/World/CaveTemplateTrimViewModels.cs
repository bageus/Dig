using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Application.World;
using Dig.Domain.World;

namespace Dig.Presentation.World
{

public enum CaveTemplateTrimRole
{
    Entrance = 0,
    Arch = 1,
    SideWall = 2,
    BackWall = 3,
}

public sealed class CaveTemplateTrimRowViewModel
{
    public CaveTemplateTrimRowViewModel(
        int level,
        int minX,
        int y,
        int width)
    {
        if (level < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(level));
        }

        if (minX < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minX));
        }

        if (y < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(y));
        }

        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        Level = level;
        MinX = minX;
        Y = y;
        Width = width;
    }

    public int Level { get; }

    public int MinX { get; }

    public int Y { get; }

    public int Width { get; }

    public int MaxX => MinX + Width - 1;
}

public sealed class CaveTemplateTrimInstanceViewModel
{
    public CaveTemplateTrimInstanceViewModel(
        string instanceId,
        string templateId,
        CaveRoomPresetKind kind,
        CellId entrance,
        int depth,
        byte variant,
        bool hasBackWall,
        IReadOnlyCollection<CaveTemplateTrimRowViewModel> rows,
        IReadOnlyCollection<int> archDepths)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
        {
            throw new ArgumentException(
                "A template cave trim instance requires a stable id.",
                nameof(instanceId));
        }

        if (string.IsNullOrWhiteSpace(templateId))
        {
            throw new ArgumentException(
                "A template cave trim requires a stable template id.",
                nameof(templateId));
        }

        if (!Enum.IsDefined(typeof(CaveRoomPresetKind), kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        if (depth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(depth));
        }

        if (variant > 3)
        {
            throw new ArgumentOutOfRangeException(nameof(variant));
        }

        if (rows == null)
        {
            throw new ArgumentNullException(nameof(rows));
        }

        if (archDepths == null)
        {
            throw new ArgumentNullException(nameof(archDepths));
        }

        CaveTemplateTrimRowViewModel[] rowArray = rows
            .OrderBy(row => row.Level)
            .ToArray();
        if (rowArray.Length == 0)
        {
            throw new ArgumentException(
                "A template cave trim requires at least one row.",
                nameof(rows));
        }

        int[] archArray = archDepths.OrderBy(value => value).ToArray();
        for (int index = 0; index < archArray.Length; index++)
        {
            int value = archArray[index];
            if (value <= 0 || value >= depth)
            {
                throw new ArgumentOutOfRangeException(nameof(archDepths));
            }

            if (index > 0 && archArray[index - 1] == value)
            {
                throw new ArgumentException(
                    "Template cave arch depths must be unique.",
                    nameof(archDepths));
            }
        }

        InstanceId = instanceId;
        TemplateId = templateId;
        Kind = kind;
        Entrance = entrance;
        Depth = depth;
        Variant = variant;
        HasBackWall = hasBackWall;
        Rows = new ReadOnlyCollection<CaveTemplateTrimRowViewModel>(rowArray);
        ArchDepths = new ReadOnlyCollection<int>(archArray);
    }

    public string InstanceId { get; }

    public string TemplateId { get; }

    public CaveRoomPresetKind Kind { get; }

    public CellId Entrance { get; }

    public int Depth { get; }

    public byte Variant { get; }

    public bool HasBackWall { get; }

    public IReadOnlyList<CaveTemplateTrimRowViewModel> Rows { get; }

    public IReadOnlyList<int> ArchDepths { get; }
}

public sealed class CaveTemplateTrimVolumeViewModel
{
    public CaveTemplateTrimVolumeViewModel(
        long version,
        IReadOnlyCollection<CaveTemplateTrimInstanceViewModel> instances)
    {
        if (version < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version));
        }

        if (instances == null)
        {
            throw new ArgumentNullException(nameof(instances));
        }

        Version = version;
        Instances = new ReadOnlyCollection<CaveTemplateTrimInstanceViewModel>(
            instances.ToArray());
    }

    public long Version { get; }

    public IReadOnlyList<CaveTemplateTrimInstanceViewModel> Instances { get; }

    public static CaveTemplateTrimVolumeViewModel Empty()
    {
        return new CaveTemplateTrimVolumeViewModel(
            version: 0,
            Array.Empty<CaveTemplateTrimInstanceViewModel>());
    }
}

}
