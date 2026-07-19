using UnityEngine;

namespace Dig.Unity
{
    [CreateAssetMenu(
        fileName = "TerrainVisualCatalog",
        menuName = "Dig/Visual Catalogs/Terrain")]
    public sealed class DigTerrainVisualCatalog : DigVisualCatalog
    {
    }

    [CreateAssetMenu(
        fileName = "BuildingVisualCatalog",
        menuName = "Dig/Visual Catalogs/Buildings")]
    public sealed class DigBuildingVisualCatalog : DigVisualCatalog
    {
    }

    [CreateAssetMenu(
        fileName = "ItemVisualCatalog",
        menuName = "Dig/Visual Catalogs/Items")]
    public sealed class DigItemVisualCatalog : DigVisualCatalog
    {
    }

    [CreateAssetMenu(
        fileName = "ResidentVisualCatalog",
        menuName = "Dig/Visual Catalogs/Residents")]
    public sealed class DigResidentVisualCatalog : DigVisualCatalog
    {
    }

    [CreateAssetMenu(
        fileName = "CreatureVisualCatalog",
        menuName = "Dig/Visual Catalogs/Creatures")]
    public sealed class DigCreatureVisualCatalog : DigVisualCatalog
    {
    }
}
