using System;
using Dig.Domain.World;

namespace Dig.Presentation.World
{

public readonly struct WorldCellViewModel
{
    public WorldCellViewModel(
        int x,
        int y,
        int z,
        string materialId,
        bool isSolid,
        bool isExplored,
        bool isDesignated,
        int hardness,
        ushort damage,
        short temperature,
        long worldVersion,
        ExcavationQuarter completedExcavationQuarters = ExcavationQuarter.None,
        ExcavationCutPattern excavationCutPattern = ExcavationCutPattern.None)
    {
        if (string.IsNullOrWhiteSpace(materialId))
        {
            throw new ArgumentException("Material id is required.", nameof(materialId));
        }

        X = x;
        Y = y;
        Z = z;
        MaterialId = materialId;
        IsSolid = isSolid;
        IsExplored = isExplored;
        IsDesignated = isDesignated;
        Hardness = hardness;
        Damage = damage;
        Temperature = temperature;
        WorldVersion = worldVersion;
        CompletedExcavationQuarters = completedExcavationQuarters;
        ExcavationCutPattern = excavationCutPattern;
    }

    public int X { get; }
    public int Y { get; }
    public int Z { get; }
    public string MaterialId { get; }
    public bool IsSolid { get; }
    public bool IsExplored { get; }
    public bool IsDesignated { get; }
    public int Hardness { get; }
    public ushort Damage { get; }
    public short Temperature { get; }
    public long WorldVersion { get; }
    public ExcavationQuarter CompletedExcavationQuarters { get; }
    public ExcavationCutPattern ExcavationCutPattern { get; }
    public bool IsExcavationOpen =>
        CompletedExcavationQuarters == ExcavationQuarter.All;
    public bool HasFullActorSupport =>
        IsSolid && CompletedExcavationQuarters == ExcavationQuarter.None;
}
}
