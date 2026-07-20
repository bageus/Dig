using UnityEngine;

namespace Dig.Unity
{

public sealed partial class DigAgentRenderer
{
    private const string ItemCatalogResourcePath = "Dig/VisualCatalogs/Items";

    [SerializeField]
    private DigItemVisualCatalog? itemVisualCatalog;

    public void SetItemVisualCatalog(DigItemVisualCatalog? catalog)
    {
        itemVisualCatalog = catalog;
        DigVisualCatalogDiagnostics.LogValidation(
            itemVisualCatalog,
            this,
            "Resident item attachments");
        foreach (DigResidentInventoryAttachmentVisual visual
            in _inventoryAttachmentVisuals.Values)
        {
            visual.InvalidateAsset();
        }
    }

    private DigItemVisualResolution ResolveItemVisual(string itemId)
    {
        if (itemVisualCatalog == null)
        {
            itemVisualCatalog = Resources.Load<DigItemVisualCatalog>(
                ItemCatalogResourcePath);
        }

        if (itemVisualCatalog != null)
        {
            return itemVisualCatalog.ResolveItem(itemId);
        }

        return new DigItemVisualResolution(
            DigVisualAsset.CreateRuntimeFallback(itemId, Color.magenta),
            icon: null,
            DigItemCarrySocketPolicy.None,
            new Vector3(0.34f, 0.34f, 0.34f),
            new Vector3(0.28f, 0.28f, 0.28f),
            DigItemRotationPolicy.Fixed,
            DigItemColliderPolicy.None,
            maxVisibleInstances: 1,
            hasProfile: false);
    }
}
}
