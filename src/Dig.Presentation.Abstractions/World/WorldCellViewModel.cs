using System;

namespace Dig.Presentation.World
{

public readonly struct WorldCellViewModel
{
    public WorldCellViewModel(
        int x,
        int y,
        string materialId,
        bool isSolid,
        bool isExplored,
        bool isDesignated,
        int hardness,
        ushort damage,
        short temperature,
        long worldVersion)
    {
        if (string.IsNullOrWhiteSpace(materialId))
        {
            throw new ArgumentException("Material id is required.", nameof(materialId));
        }

        X = x;
        Y = y;
        MaterialId = materialId;
        IsSolid = isSolid;
        IsExplored = isExplored;
        IsDesignated = isDesignated;
        Hardness = hardness;
        Damage = damage;
        Temperature = temperature;
        WorldVersion = worldVersion;
    }

    public int X { get; }
    public int Y { get; }
    public string MaterialId { get; }
    public bool IsSolid { get; }
    public bool IsExplored { get; }
    public bool IsDesignated { get; }
    public int Hardness { get; }
    public ushort Damage { get; }
    public short Temperature { get; }
    public long WorldVersion { get; }
}
}
