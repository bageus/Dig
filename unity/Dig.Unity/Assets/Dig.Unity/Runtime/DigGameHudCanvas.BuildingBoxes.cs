using Dig.Presentation.Buildings;
using Dig.Presentation.Inventory;
using UnityEngine;
using UnityEngine.UI;

namespace Dig.Unity
{
    public sealed partial class DigGameHudCanvas
    {
        private readonly BuildingBoxFunctionsPresenter _buildingBoxFunctionsPresenter =
            new BuildingBoxFunctionsPresenter();

        private void ShowBuildingBoxFunctions(WorldItemViewModel item)
        {
            BuildingBoxFunctionsViewModel functions = _buildingBoxFunctionsPresenter.Present(
                item,
                _interaction!.ActiveBuildingPlacementStackId);
            BuildingBoxFunctionActionViewModel action = functions.UnpackAction;
            string signature = $"building-box:{functions.StackId}:"
                + $"{action.IsEnabled}:{action.IsActive}:"
                + $"{_interaction.BuildingPlacementValid}:"
                + _interaction.BuildingPlacementReasonCode;
            if (!ApplyContextSignature(signature))
            {
                return;
            }

            BeginBottomLayout(142f);
            RectTransform section = CreateSection(
                "BuildingBox Functions",
                _bottomContent!,
                "BUILDING BOX",
                preferredWidth: 900f);
            string detailsText = action.IsActive
                ? PlacementDescription()
                : "Packed building is ready for placement";
            Text details = CreateText(
                "Details",
                section,
                detailsText,
                18,
                TextAnchor.MiddleCenter);
            details.gameObject.AddComponent<LayoutElement>().preferredHeight = 32f;

            Button unpack = CreateButton(
                "Unpack",
                section,
                action.IsActive ? "Cancel unpacking" : "Unpack",
                ActivateBuildingBoxAction,
                preferredHeight: 44f);
            unpack.interactable = action.IsEnabled;
            SetButtonActive(unpack, action.IsActive);
        }

        private string PlacementDescription()
        {
            return _interaction!.BuildingPlacementValid
                ? "Valid position — LMB confirms"
                : "Invalid: "
                    + (_interaction.BuildingPlacementReasonCode ?? "no cell");
        }

        private void ActivateBuildingBoxAction()
        {
            _interaction!.ActivateSelectedBuildingBoxFromHud();
            InvalidateAll();
        }
    }
}
