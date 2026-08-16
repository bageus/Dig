using System;
using System.Linq;
using Dig.Application.Farming;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Farming;
using Dig.Presentation.Buildings;
using UnityEngine;
using UnityEngine.UI;

namespace Dig.Unity
{

public sealed partial class DigGameHudCanvas
{
    private bool TryShowFarm(BuildingWorldViewModel building)
    {
        FarmSnapshot? farm = _terrainSession!.LoadFarmSnapshot(building.Id);
        if (farm == null)
        {
            return false;
        }

        FarmSupplyDemand[] demands = _terrainSession.LoadFarmSupplyDemands(building.Id).ToArray();
        string demandSignature = string.Join(",", demands.Select(value =>
            value.Kind + ":" + value.Quantity));
        string signature = "farm:" + building.Id + ":" + building.Version + ":"
            + farm.Mode + ":" + farm.MushroomSeedEstablished + ":"
            + farm.MushroomSlotsOccupied + ":" + farm.ResidualMushrooms + ":"
            + farm.HamsterCount + ":" + farm.GrubCount + ":" + farm.FeedCount + ":"
            + farm.EscapingHamsterCount + ":" + farm.EscapingGrubCount + ":"
            + demandSignature;
        if (!ApplyContextSignature(signature))
        {
            return true;
        }

        BeginBottomLayout(156f);
        RectTransform section = CreateSection(
            "Farm",
            _bottomContent!,
            building.Name.ToUpperInvariant(),
            preferredWidth: 1220f);

        RectTransform modeRow = CreateHorizontalRow("Farm Modes", section, 38f);
        CreateFarmModeButton(
            building.Id,
            farm,
            FarmMode.Mushrooms,
            DigProductionIconGlyph.Resolve(CampfireProductionContent.MushroomCapItemId.ToString()),
            modeRow);
        CreateFarmModeButton(
            building.Id,
            farm,
            FarmMode.Hamsters,
            DigProductionIconGlyph.Resolve(LivingMaterialContent.HamsterItemId.ToString()),
            modeRow);
        CreateFarmModeButton(
            building.Id,
            farm,
            FarmMode.Grubs,
            DigProductionIconGlyph.Resolve(LivingMaterialContent.GrubItemId.ToString()),
            modeRow);
        int harvestableMushrooms =
            farm.MushroomSlotsOccupied + farm.ResidualMushrooms;
        if (harvestableMushrooms > 0)
        {
            Button harvest = CreateButton(
                "Harvest Farm Mushroom",
                modeRow,
                "Order harvest (" + harvestableMushrooms + ")",
                () => StartFarmMushroomHarvest(building.Id),
                preferredHeight: 36f);
            harvest.interactable = _agentRenderer!.SelectedModel != null;
        }

        string status = BuildFarmStatus(farm, demands);
        Text details = CreateText(
            "Farm Status",
            section,
            status,
            15,
            TextAnchor.MiddleCenter);
        details.gameObject.AddComponent<LayoutElement>().preferredHeight = 45f;
        return true;
    }

    private void CreateFarmModeButton(
        string buildingId,
        FarmSnapshot farm,
        FarmMode mode,
        string glyph,
        Transform parent)
    {
        Button button = CreateButton(
            "Farm Mode " + mode,
            parent,
            glyph,
            () => ChangeFarmMode(buildingId, mode),
            preferredHeight: 36f);
        SetButtonActive(button, farm.Mode == mode);
        button.interactable = farm.Mode != mode;
        Text label = button.GetComponentInChildren<Text>();
        label.fontSize = 22;
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 10;
        label.resizeTextMaxSize = 22;
    }

    private void ChangeFarmMode(string buildingId, FarmMode mode)
    {
        long tick = _simulation?.CurrentTick ?? 0;
        Result result = _terrainSession!.SetFarmMode(buildingId, mode, tick);
        _legacyHud!.SetCommandResult(result);
        InvalidateAll();
    }

    private void StartFarmMushroomHarvest(string buildingId)
    {
        Dig.Presentation.Agents.AgentViewModel? worker = _agentRenderer!.SelectedModel;
        if (worker == null)
        {
            _legacyHud!.SetStatus("Select a dwarf before harvesting a farm mushroom.");
            return;
        }

        long tick = _simulation?.CurrentTick ?? 0;
        Result result = _terrainSession!.StartFarmMushroomHarvest(
            buildingId,
            EntityId.Parse(worker.Id),
            new Dig.Domain.World.CellId(worker.CellX, worker.CellY, worker.CellZ),
            tick);
        _legacyHud!.SetCommandResult(result);
        InvalidateAll();
    }

    private static string BuildFarmStatus(
        FarmSnapshot farm,
        FarmSupplyDemand[] demands)
    {
        string population;
        switch (farm.Mode)
        {
            case FarmMode.Mushrooms:
                population = "Mushrooms " + farm.MushroomSlotsOccupied + "/"
                    + FarmOperationPolicy.MushroomGrowthSlots;
                break;
            case FarmMode.Hamsters:
                population = "Hamsters " + farm.HamsterCount + "/"
                    + FarmOperationPolicy.AnimalCapacity
                    + " · protected " + FarmOperationPolicy.HamsterBreederReserve;
                break;
            case FarmMode.Grubs:
                population = "Grubs " + farm.GrubCount + "/"
                    + FarmOperationPolicy.AnimalCapacity
                    + " · protected " + FarmOperationPolicy.GrubBreederReserve;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        string feed = farm.Mode == FarmMode.Mushrooms
            ? string.Empty
            : " · Feed " + farm.FeedCount + "/" + FarmOperationPolicy.FeedCapacity;
        string pending = demands.Length == 0
            ? " · supply ready"
            : " · needs " + string.Join(", ", demands.Select(value =>
                value.Quantity + " " + value.ItemId));
        string leftovers = farm.ResidualMushrooms > 0
            ? " · old mushrooms " + farm.ResidualMushrooms
            : string.Empty;
        int escaping = farm.EscapingHamsterCount + farm.EscapingGrubCount;
        string leaving = escaping > 0 ? " · leaving " + escaping : string.Empty;
        return population + feed + pending + leftovers + leaving;
    }
}

}
