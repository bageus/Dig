using System;
using UnityEngine;

namespace Dig.Unity
{

[DisallowMultipleComponent]
internal sealed class DigBuildingInternalStockVisual : MonoBehaviour
{
    internal string BuildingId { get; private set; } = string.Empty;
    internal string ItemId { get; private set; } = string.Empty;

    internal void Initialize(string buildingId, string itemId)
    {
        if (string.IsNullOrWhiteSpace(buildingId)
            || string.IsNullOrWhiteSpace(itemId))
        {
            throw new ArgumentException("Building and item ids are required.");
        }

        BuildingId = buildingId;
        ItemId = itemId;
        Collider? collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }
    }
}

}
