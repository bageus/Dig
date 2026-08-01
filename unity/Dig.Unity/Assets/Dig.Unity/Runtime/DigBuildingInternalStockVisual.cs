using System;
using UnityEngine;

namespace Dig.Unity
{

[DisallowMultipleComponent]
internal sealed class DigBuildingInternalStockVisual : MonoBehaviour
{
    internal string BuildingId { get; private set; } = string.Empty;
    internal string ItemId { get; private set; } = string.Empty;
    internal string StackId { get; private set; } = string.Empty;
    internal DigWorldItemVisual WorldItemVisual =>
        GetComponent<DigWorldItemVisual>()
        ?? throw new InvalidOperationException("Internal stock item visual is missing.");

    internal void Initialize(string buildingId, string itemId, string stackId)
    {
        if (string.IsNullOrWhiteSpace(buildingId)
            || string.IsNullOrWhiteSpace(itemId)
            || string.IsNullOrWhiteSpace(stackId))
        {
            throw new ArgumentException("Building and item ids are required.");
        }

        BuildingId = buildingId;
        ItemId = itemId;
        StackId = stackId;
        Collider? collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }
    }
}

}
