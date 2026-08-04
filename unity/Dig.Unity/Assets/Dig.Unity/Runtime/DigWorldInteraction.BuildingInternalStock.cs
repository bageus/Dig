using UnityEngine;

namespace Dig.Unity
{

public sealed partial class DigWorldInteraction
{
    private bool TryResolveBuildingInternalStockHit(
        RaycastHit[] hits,
        out DigBuildingInternalStockVisual stock)
    {
        for (int index = 0; index < hits.Length; index++)
        {
            if (_buildingInternalStockRenderer != null
                && _buildingInternalStockRenderer.TryGetStock(hits[index], out stock))
            {
                return true;
            }

            if (_itemRenderer != null
                && _itemRenderer.TryGetItem(hits[index], out _))
            {
                stock = null!;
                return false;
            }
        }

        stock = null!;
        return false;
    }
}

}
