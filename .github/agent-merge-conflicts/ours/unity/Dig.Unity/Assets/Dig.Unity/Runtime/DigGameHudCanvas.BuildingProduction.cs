using System;
using System.Linq;
using Dig.Domain.Core;
using Dig.Presentation.Buildings;
using Dig.Presentation.Production;
using UnityEngine;
using UnityEngine.UI;

namespace Dig.Unity
{

public sealed partial class DigGameHudCanvas
{
    private bool TryShowBuildingProduction(BuildingWorldViewModel building)
    {
        BuildingProductionViewModel? production =
            _terrainSession!.LoadBuildingProduction(building.Id);
        if (production == null)
        {
            return false;
        }

        ShowBuildingProductionFunctions(building, production);
        return true;
    }

    private void ShowBuildingProductionFunctions(
        BuildingWorldViewModel building,
        BuildingProductionViewModel production)
    {
        BuildingFunctionsViewModel functions = building.Functions;
        long tick = _simulation?.CurrentTick ?? 0;
        PackableBuildingExecutionViewModel? operation = _terrainSession!
            .LoadPackableBuildingExecutions(tick)
            .FirstOrDefault(value => string.Equals(
                value.PackageId,
                building.Id,
                StringComparison.Ordinal));
        string signature = BuildProductionSignature(building, production, operation);
        if (!ApplyContextSignature(signature))
        {
            return;
        }

        BeginBottomLayout();
        RectTransform section = CreateSection(
            "Building Production",
            _bottomContent!,
            building.Name.ToUpperInvariant(),
            preferredWidth: 1220f);
        Text tooltip = CreateText(
            "Production Tooltip",
            section,
            "Hover an icon to view required materials.",
            12,
            TextAnchor.MiddleCenter);
        tooltip.resizeTextForBestFit = true;
        tooltip.resizeTextMinSize = 8;
        tooltip.resizeTextMaxSize = 12;
        tooltip.gameObject.AddComponent<LayoutElement>().preferredHeight = 14f;

        RectTransform productRow = CreateHorizontalRow("Products", section, 34f);
        for (int index = 0; index < production.Products.Count; index++)
        {
            ProductionIconViewModel product = production.Products[index];
            CreateProductionIconButton(building, product, productRow, tooltip);
        }

        RectTransform stockRow = CreateHorizontalRow("Internal Stock", section, 30f);
        for (int index = 0; index < production.Stocks.Count; index++)
        {
            BuildingStockIconViewModel stock = production.Stocks[index];
            CreateStockIconButton(building, stock, stockRow, tooltip);
        }

        if (functions.Actions.Count > 0)
        {
            BuildingFunctionActionViewModel action = functions.Actions[0];
            string label = operation?.IsInterrupted == true
                ? "Continue packing"
                : functions.IsPacking ? "Packing" : "Pack";
            Button pack = CreateButton(
                "Pack",
                stockRow,
                label,
                () => ExecutePacking(building.Id),
                preferredHeight: 28f);
            pack.interactable = action.IsEnabled;
            SetButtonActive(pack, operation != null && !operation.IsTerminal);
        }
    }

    private void CreateProductionIconButton(
        BuildingWorldViewModel building,
        ProductionIconViewModel product,
        Transform parent,
        Text tooltip)
    {
        Button button = CreateButton(
            "Product " + product.RecipeId,
            parent,
            DigProductionIconGlyph.Resolve(product.OutputItemId.ToString()),
            () => QueueBuildingProduction(building.Id, product.RecipeId.ToString()),
            preferredHeight: 32f);
        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = product.IsOrange
                ? new Color(0.91f, 0.46f, 0.10f, 1f)
                : new Color(0.16f, 0.42f, 0.25f, 1f);
        }

        Text label = button.GetComponentInChildren<Text>();
        label.fontSize = 22;
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 10;
        label.resizeTextMaxSize = 22;
        CreateIconCount(
            button.transform,
            product.QueuedCount > 0 ? product.QueuedCount.ToString() : string.Empty,
            TextAnchor.LowerRight);
        CreateProductionProgressSegments(button.transform, product);
        string hover = product.DisplayName + " ×" + product.OutputQuantity
            + "\n" + product.Tooltip;
        DigProductionIconPointer pointer = BindIconTooltip(button, tooltip, hover);
        pointer.RightClicked = product.QueuedCount > 0
            ? () => CancelBuildingProduction(
                building.Id,
                product.RecipeId.ToString())
            : null;
    }

    private static void CreateProductionProgressSegments(
        Transform parent,
        ProductionIconViewModel product)
    {
        if (!product.HasProgress)
        {
            return;
        }

        const float gap = 0.015f;
        for (int index = 0; index < product.ProgressTotal; index++)
        {
            GameObject segment = new GameObject(
                "Material progress " + index,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            segment.transform.SetParent(parent, worldPositionStays: false);
            RectTransform rect = (RectTransform)segment.transform;
            float width = 1f / product.ProgressTotal;
            rect.anchorMin = new Vector2((index * width) + gap, 0.03f);
            rect.anchorMax = new Vector2(((index + 1) * width) - gap, 0.16f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = segment.GetComponent<Image>();
            image.color = index < product.ProgressCurrent
                ? new Color(0.30f, 0.88f, 0.36f, 1f)
                : new Color(0.08f, 0.11f, 0.13f, 0.92f);
            image.raycastTarget = false;
        }
    }

    private void CreateStockIconButton(
        BuildingWorldViewModel building,
        BuildingStockIconViewModel stock,
        Transform parent,
        Text tooltip)
    {
        Button button = CreateButton(
            "Stock " + stock.ItemId,
            parent,
            DigProductionIconGlyph.Resolve(stock.ItemId.ToString()),
            () => ToggleBuildingStock(building.Id, stock),
            preferredHeight: 28f);
        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = stock.DeliveryEnabled
                ? new Color(0.17f, 0.45f, 0.66f, 1f)
                : new Color(0.25f, 0.27f, 0.30f, 1f);
        }

        Text label = button.GetComponentInChildren<Text>();
        label.fontSize = 18;
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 9;
        label.resizeTextMaxSize = 18;
        CreateIconCount(
            button.transform,
            stock.Current + "/" + stock.Capacity,
            TextAnchor.LowerRight);
        string incoming = stock.Incoming == 0 ? string.Empty : " +" + stock.Incoming;
        string hover = stock.DisplayName + " " + stock.Current + "/"
            + stock.Capacity + incoming + "\nDelivery: "
            + (stock.DeliveryEnabled ? "enabled" : "disabled");
        BindIconTooltip(button, tooltip, hover);
    }

    private static void CreateIconCount(
        Transform parent,
        string value,
        TextAnchor alignment)
    {
        Text count = CreateText("Count", parent, value, 10, alignment);
        count.resizeTextForBestFit = true;
        count.resizeTextMinSize = 7;
        count.resizeTextMaxSize = 10;
        count.rectTransform.anchorMin = Vector2.zero;
        count.rectTransform.anchorMax = Vector2.one;
        count.rectTransform.offsetMin = new Vector2(4f, 2f);
        count.rectTransform.offsetMax = new Vector2(-4f, -2f);
        count.raycastTarget = false;
    }

    private static DigProductionIconPointer BindIconTooltip(
        Button button,
        Text tooltip,
        string value)
    {
        DigProductionIconPointer pointer =
            button.gameObject.AddComponent<DigProductionIconPointer>();
        pointer.HoverChanged = active => tooltip.text = active
            ? value
            : "Hover an icon to view required materials.";
        return pointer;
    }

    private void QueueBuildingProduction(string buildingId, string recipeId)
    {
        long tick = _simulation?.CurrentTick ?? 0;
        Result result = _terrainSession!.EnqueueBuildingProduction(
            buildingId,
            recipeId,
            tick);
        _legacyHud!.SetCommandResult(result);
        InvalidateAll();
    }

    private void CancelBuildingProduction(string buildingId, string recipeId)
    {
        BuildingProductionViewModel? current =
            _terrainSession!.LoadBuildingProduction(buildingId);
        bool hasNonTerminalOrder = current?.Products.Any(value =>
            value.QueuedCount > 0
            && string.Equals(
                value.RecipeId.ToString(),
                recipeId,
                StringComparison.Ordinal)) == true;
        if (!hasNonTerminalOrder)
        {
            return;
        }

        long tick = _simulation?.CurrentTick ?? 0;
        Result result = _terrainSession.CancelOneBuildingProduction(
            buildingId,
            recipeId,
            tick);
        _legacyHud!.SetCommandResult(result);
        InvalidateAll();
    }

    private void ToggleBuildingStock(
        string buildingId,
        BuildingStockIconViewModel stock)
    {
        long tick = _simulation?.CurrentTick ?? 0;
        Result result = _terrainSession!.SetBuildingStockDelivery(
            buildingId,
            stock.ItemId.ToString(),
            !stock.DeliveryEnabled,
            tick);
        _legacyHud!.SetCommandResult(result);
        InvalidateAll();
    }

    private static string BuildProductionSignature(
        BuildingWorldViewModel building,
        BuildingProductionViewModel production,
        PackableBuildingExecutionViewModel? operation)
    {
        string products = string.Join(",", production.Products.Select(value =>
            value.RecipeId + ":" + value.QueuedCount + ":" + value.IsOrange
                + ":" + value.ProgressCurrent + "/" + value.ProgressTotal));
        string stocks = string.Join(",", production.Stocks.Select(value =>
            value.ItemId + ":" + value.Current + ":" + value.Incoming + ":"
                + value.DeliveryEnabled));
        string packing = operation == null
            ? "none"
            : operation.Status + ":" + operation.CompletedIterations;
        return "building-production:" + building.Id + ":" + building.Version
            + ":" + products + ":" + stocks + ":" + packing;
    }
}

}
