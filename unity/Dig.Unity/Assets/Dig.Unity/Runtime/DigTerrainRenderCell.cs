using Dig.Presentation.World;

namespace Dig.Unity
{
    internal readonly struct DigTerrainRenderCell
    {
        internal DigTerrainRenderCell(
            DigTerrainCellKey key,
            string materialId,
            bool isSolid,
            bool isExplored,
            bool isDesignated,
            int hardness,
            ushort damage,
            short temperature,
            long version)
        {
            Key = key;
            MaterialId = materialId;
            IsSolid = isSolid;
            IsExplored = isExplored;
            IsDesignated = isDesignated;
            Hardness = hardness;
            Damage = damage;
            Temperature = temperature;
            Version = version;
        }

        internal DigTerrainCellKey Key { get; }
        internal string MaterialId { get; }
        internal bool IsSolid { get; }
        internal bool IsExplored { get; }
        internal bool IsDesignated { get; }
        internal int Hardness { get; }
        internal ushort Damage { get; }
        internal short Temperature { get; }
        internal long Version { get; }

        internal static DigTerrainRenderCell FromWorld(WorldCellViewModel cell, int z)
        {
            return new DigTerrainRenderCell(
                new DigTerrainCellKey(cell.X, cell.Y, z),
                cell.MaterialId,
                cell.IsSolid,
                cell.IsExplored,
                cell.IsDesignated,
                cell.Hardness,
                cell.Damage,
                cell.Temperature,
                cell.WorldVersion);
        }
    }
}
