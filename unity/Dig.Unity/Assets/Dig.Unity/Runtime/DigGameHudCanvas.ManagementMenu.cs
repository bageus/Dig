using System;
using Dig.Presentation.Management;
using UnityEngine;
using UnityEngine.UI;

namespace Dig.Unity
{

public sealed partial class DigGameHudCanvas
{
    private enum ManagementOverlayKind
    {
        None = 0,
        Dwarfs = 1,
        Items = 2,
        Buildings = 3,
    }

    private enum DwarfManagementTab
    {
        Standard = 0,
        Production = 1,
        Fight = 2,
        Family = 3,
        Inventory = 4,
    }

    private const float ManagementMenuWidth = 54f;
    private RectTransform? _managementMenuButton;
    private RectTransform? _managementDropdown;
    private RectTransform? _managementOverlay;
    private RectTransform? _managementTableContent;
    private ManagementOverlayKind _managementOverlayKind;
    private DwarfManagementTab _dwarfManagementTab;
    private SettlementItemGroup _itemManagementTab;
    private string _managementSignature = string.Empty;

    private void CreateManagementShell()
    {
        _managementMenuButton = CreatePanel(
            "Management Menu Button",
            transform,
            new Color(0.08f, 0.11f, 0.15f, 0.98f));
        Anchor(
            _managementMenuButton,
            0f,
            1f,
            0f,
            1f,
            14f,
            -54f,
            14f + ManagementMenuWidth,
            -12f);
        Button menuButton = _managementMenuButton.gameObject.AddComponent<Button>();
        menuButton.onClick.AddListener(ToggleManagementDropdown);
        RectTransform menuIcon = CreateRect("Menu Icon", _managementMenuButton);
        menuIcon.anchorMin = new Vector2(0.5f, 0.5f);
        menuIcon.anchorMax = new Vector2(0.5f, 0.5f);
        menuIcon.pivot = new Vector2(0.5f, 0.5f);
        menuIcon.sizeDelta = new Vector2(28f, 22f);
        menuIcon.anchoredPosition = Vector2.zero;
        CreateIconBar("Top", menuIcon, new Vector2(28f, 3f), new Vector2(0f, 8f));
        CreateIconBar("Middle", menuIcon, new Vector2(28f, 3f), Vector2.zero);
        CreateIconBar("Bottom", menuIcon, new Vector2(28f, 3f), new Vector2(0f, -8f));

        _managementDropdown = CreatePanel(
            "Management Dropdown",
            transform,
            new Color(0.035f, 0.05f, 0.075f, 0.98f));
        Anchor(_managementDropdown, 0f, 1f, 0f, 1f, 14f, -188f, 194f, -60f);
        VerticalLayoutGroup dropdownLayout =
            _managementDropdown.gameObject.AddComponent<VerticalLayoutGroup>();
        dropdownLayout.padding = new RectOffset(6, 6, 6, 6);
        dropdownLayout.spacing = 4f;
        dropdownLayout.childControlHeight = true;
        dropdownLayout.childControlWidth = true;
        dropdownLayout.childForceExpandHeight = true;
        dropdownLayout.childForceExpandWidth = true;
        CreateButton(
            "Dwarfs",
            _managementDropdown,
            "Dwarfs",
            () => OpenManagementOverlay(ManagementOverlayKind.Dwarfs));
        CreateButton(
            "Items",
            _managementDropdown,
            "Items",
            () => OpenManagementOverlay(ManagementOverlayKind.Items));
        CreateButton(
            "Buildings",
            _managementDropdown,
            "Buildings",
            () => OpenManagementOverlay(ManagementOverlayKind.Buildings));
        _managementDropdown.gameObject.SetActive(false);

        _managementOverlay = CreatePanel(
            "Management Overlay",
            transform,
            new Color(0.025f, 0.035f, 0.05f, 0.985f));
        Anchor(_managementOverlay, 0.07f, 0.09f, 0.93f, 0.91f, 0f, 0f, 0f, 0f);
        Outline outline = _managementOverlay.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.72f, 0.76f, 0.82f, 0.75f);
        outline.effectDistance = new Vector2(1f, -1f);
        _managementOverlay.gameObject.SetActive(false);
        _managementMenuButton.SetAsLastSibling();
    }

    private void ToggleManagementDropdown()
    {
        bool show = !_managementDropdown!.gameObject.activeSelf;
        _managementDropdown.gameObject.SetActive(show);
        if (show)
        {
            _managementDropdown.SetAsLastSibling();
            _managementMenuButton!.SetAsLastSibling();
        }
    }

    private void OpenManagementOverlay(ManagementOverlayKind kind)
    {
        _managementOverlayKind = kind;
        _managementDropdown!.gameObject.SetActive(false);
        _managementSignature = string.Empty;
        _managementOverlay!.gameObject.SetActive(true);
        _managementOverlay.SetAsLastSibling();
        _managementMenuButton!.SetAsLastSibling();
        RefreshManagementOverlay();
    }

    private void CloseManagementOverlay()
    {
        _managementOverlayKind = ManagementOverlayKind.None;
        _managementSignature = string.Empty;
        _managementOverlay!.gameObject.SetActive(false);
    }

    private void RefreshManagementOverlay()
    {
        if (!_initialized || _managementOverlayKind == ManagementOverlayKind.None)
        {
            return;
        }

        switch (_managementOverlayKind)
        {
            case ManagementOverlayKind.Dwarfs:
                RefreshDwarfManagement();
                break;
            case ManagementOverlayKind.Items:
                RefreshItemManagement();
                break;
            case ManagementOverlayKind.Buildings:
                RefreshBuildingManagement();
                break;
        }
    }

    private bool PointInManagementUi(Vector3 screenPoint)
    {
        return Contains(_managementMenuButton, screenPoint)
            || (_managementDropdown != null
                && _managementDropdown.gameObject.activeSelf
                && Contains(_managementDropdown, screenPoint))
            || (_managementOverlay != null
                && _managementOverlay.gameObject.activeSelf
                && Contains(_managementOverlay, screenPoint));
    }
}

}
