using System;
using System.Collections.Generic;
using System.Linq;

namespace Dig.Domain.Inventory
{

public enum ItemWorldInteractionAction
{
    None = 0,
    Pickup = 1,
    SelectBuildingBox = 2,
    DirectUse = 3,
    UseProductionPackage = 4,
}

public enum ItemInventoryInteractionAction
{
    None = 0,
    PlaceItem = 1,
    PlaceBuilding = 2,
    DirectUse = 3,
}

public enum ItemInteractionFeedbackKind
{
    None = 0,
    Pickup = 1,
    Eat = 2,
    Use = 3,
    Drop = 4,
}

public sealed class ItemFoodUseDefinition
{
    public ItemFoodUseDefinition(int nutritionUnits, int biteCount)
    {
        if (nutritionUnits <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(nutritionUnits));
        }

        if (biteCount <= 0 || nutritionUnits % biteCount != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(biteCount));
        }

        NutritionUnits = nutritionUnits;
        BiteCount = biteCount;
    }

    public int NutritionUnits { get; }

    public int BiteCount { get; }

    public int NutritionPerBite => NutritionUnits / BiteCount;
}

public sealed class ItemInteractionProfile
{
    public ItemInteractionProfile(
        ItemWorldInteractionAction worldPrimaryAction,
        ItemWorldInteractionAction worldAltAction,
        ItemInventoryInteractionAction inventoryPrimaryAction,
        ItemInventoryInteractionAction inventoryAltAction,
        bool inventoryQuickDropAllowed,
        ItemInteractionFeedbackKind directUseFeedback)
    {
        ValidateEnum(worldPrimaryAction, nameof(worldPrimaryAction));
        ValidateEnum(worldAltAction, nameof(worldAltAction));
        ValidateEnum(inventoryPrimaryAction, nameof(inventoryPrimaryAction));
        ValidateEnum(inventoryAltAction, nameof(inventoryAltAction));
        ValidateEnum(directUseFeedback, nameof(directUseFeedback));

        bool hasDirectUse = worldPrimaryAction == ItemWorldInteractionAction.DirectUse
            || worldAltAction == ItemWorldInteractionAction.DirectUse
            || inventoryPrimaryAction == ItemInventoryInteractionAction.DirectUse
            || inventoryAltAction == ItemInventoryInteractionAction.DirectUse;
        if (hasDirectUse == (directUseFeedback == ItemInteractionFeedbackKind.None))
        {
            throw new ArgumentException(
                "Direct-use actions require non-empty feedback and non-use profiles require empty feedback.",
                nameof(directUseFeedback));
        }

        WorldPrimaryAction = worldPrimaryAction;
        WorldAltAction = worldAltAction;
        InventoryPrimaryAction = inventoryPrimaryAction;
        InventoryAltAction = inventoryAltAction;
        InventoryQuickDropAllowed = inventoryQuickDropAllowed;
        DirectUseFeedback = directUseFeedback;
    }

    public ItemWorldInteractionAction WorldPrimaryAction { get; }

    public ItemWorldInteractionAction WorldAltAction { get; }

    public ItemInventoryInteractionAction InventoryPrimaryAction { get; }

    public ItemInventoryInteractionAction InventoryAltAction { get; }

    public bool InventoryQuickDropAllowed { get; }

    public ItemInteractionFeedbackKind DirectUseFeedback { get; }

    public ItemWorldInteractionAction ResolveWorldAction(bool altPressed)
    {
        return altPressed && WorldAltAction != ItemWorldInteractionAction.None
            ? WorldAltAction
            : WorldPrimaryAction;
    }

    public ItemInventoryInteractionAction ResolveInventoryAction(bool altPressed)
    {
        return altPressed && InventoryAltAction != ItemInventoryInteractionAction.None
            ? InventoryAltAction
            : InventoryPrimaryAction;
    }

    public bool SupportsWorldAction(ItemWorldInteractionAction action)
    {
        return action != ItemWorldInteractionAction.None
            && (WorldPrimaryAction == action || WorldAltAction == action);
    }

    public bool SupportsInventoryAction(ItemInventoryInteractionAction action)
    {
        return action != ItemInventoryInteractionAction.None
            && (InventoryPrimaryAction == action || InventoryAltAction == action);
    }

    private static void ValidateEnum<T>(T value, string parameterName)
        where T : struct
    {
        if (!Enum.IsDefined(typeof(T), value))
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}

public static class ItemInteractionCategoryIds
{
    public static readonly ItemCategoryId BuildingBox =
        new ItemCategoryId("building.box");
}

public static class ItemInteractionProfiles
{
    public static ItemInteractionProfile Generic { get; } = new ItemInteractionProfile(
        ItemWorldInteractionAction.Pickup,
        ItemWorldInteractionAction.None,
        ItemInventoryInteractionAction.PlaceItem,
        ItemInventoryInteractionAction.None,
        inventoryQuickDropAllowed: true,
        ItemInteractionFeedbackKind.None);

    public static ItemInteractionProfile Food { get; } = new ItemInteractionProfile(
        ItemWorldInteractionAction.Pickup,
        ItemWorldInteractionAction.DirectUse,
        ItemInventoryInteractionAction.PlaceItem,
        ItemInventoryInteractionAction.DirectUse,
        inventoryQuickDropAllowed: true,
        ItemInteractionFeedbackKind.Eat);

    public static ItemInteractionProfile Tool { get; } = new ItemInteractionProfile(
        ItemWorldInteractionAction.Pickup,
        ItemWorldInteractionAction.None,
        ItemInventoryInteractionAction.PlaceItem,
        ItemInventoryInteractionAction.DirectUse,
        inventoryQuickDropAllowed: true,
        ItemInteractionFeedbackKind.Use);

    public static ItemInteractionProfile BuildingBox { get; } = new ItemInteractionProfile(
        ItemWorldInteractionAction.SelectBuildingBox,
        ItemWorldInteractionAction.Pickup,
        ItemInventoryInteractionAction.PlaceBuilding,
        ItemInventoryInteractionAction.None,
        inventoryQuickDropAllowed: true,
        ItemInteractionFeedbackKind.None);

    public static ItemInteractionProfile ClosedProductionPackage { get; } =
        new ItemInteractionProfile(
            ItemWorldInteractionAction.UseProductionPackage,
            ItemWorldInteractionAction.None,
            ItemInventoryInteractionAction.None,
            ItemInventoryInteractionAction.None,
            inventoryQuickDropAllowed: false,
            ItemInteractionFeedbackKind.None);

    public static ItemInteractionProfile NonInteractive { get; } =
        new ItemInteractionProfile(
            ItemWorldInteractionAction.None,
            ItemWorldInteractionAction.None,
            ItemInventoryInteractionAction.None,
            ItemInventoryInteractionAction.None,
            inventoryQuickDropAllowed: false,
            ItemInteractionFeedbackKind.None);

    public static ItemInteractionProfile ResolveDefault(
        bool isTool,
        IEnumerable<ItemCategoryId> categories,
        ItemFoodUseDefinition? foodUse)
    {
        ItemCategoryId[] values = (categories ?? Array.Empty<ItemCategoryId>()).ToArray();
        if (values.Contains(ItemInteractionCategoryIds.BuildingBox))
        {
            return BuildingBox;
        }

        if (foodUse != null)
        {
            return Food;
        }

        return isTool ? Tool : Generic;
    }
}

}
