using System;
using Dig.Presentation.Inventory;
using UnityEngine;
using UnityEngine.UI;

namespace Dig.Unity
{

[DisallowMultipleComponent]
public sealed partial class DigGameHudCanvas : MonoBehaviour
{
    private enum RightPanelTab
    {
        Residents = 0,
        Buildings = 1,
    }

    private DigTerrainWorkSession? _terrainSession;
    private DigAgentRenderer? _agentRenderer;
    private DigBuildingRenderer? _buildingRenderer;
    private DigWorldInteraction? _interaction;
    private DigAgentSimulationDriver? _simulation;
    private DigHudOverlay? _legacyHud;
    private Canvas? _canvas;
    private RectTransform? _rightPanel;
    private RectTransform? _rightContent;
    private RectTransform? _bottomPanel;
    private RectTransform? _bottomContent;
    private Text? _statusText;
    private Button? _residentTabButton;
    private Button? _buildingTabButton;
    private RightPanelTab _rightTab;
    private string _lastRosterSignature = string.Empty;
    private string _lastContextSignature = string.Empty;
    private string _status = string.Empty;
    private bool _initialized;

    internal void Initialize(
        DigTerrainWorkSession terrainSession,
        DigAgentRenderer agentRenderer,
        DigBuildingRenderer buildingRenderer,
        DigWorldInteraction interaction,
        DigAgentSimulationDriver simulation,
        DigHudOverlay legacyHud)
    {
        _terrainSession = terrainSession
            ?? throw new ArgumentNullException(nameof(terrainSession));
        _agentRenderer = agentRenderer
            ?? throw new ArgumentNullException(nameof(agentRenderer));
        _buildingRenderer = buildingRenderer
            ?? throw new ArgumentNullException(nameof(buildingRenderer));
        _interaction = interaction
            ?? throw new ArgumentNullException(nameof(interaction));
        _simulation = simulation
            ?? throw new ArgumentNullException(nameof(simulation));
        _legacyHud = legacyHud
            ?? throw new ArgumentNullException(nameof(legacyHud));
        CreateCanvasShell();
        _initialized = true;
        InvalidateAll();
    }

    internal bool ContainsScreenPoint(Vector3 screenPoint)
    {
        if (!_initialized)
        {
            return false;
        }

        return Contains(_rightPanel, screenPoint)
            || (_bottomPanel!.gameObject.activeSelf
                && Contains(_bottomPanel, screenPoint));
    }

    internal void SetStatus(string status)
    {
        _status = status ?? string.Empty;
        if (_statusText != null)
        {
            _statusText.text = _status;
        }
    }

    internal void InvalidateAll()
    {
        _lastRosterSignature = string.Empty;
        _lastContextSignature = string.Empty;
    }

    private void LateUpdate()
    {
        if (!_initialized)
        {
            return;
        }

        RefreshRoster();
        RefreshContextPanel();
    }

    private void CreateCanvasShell()
    {
        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 100;
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();
        EnsureEventSystem();

        _rightPanel = CreatePanel(
            "Roster Panel",
            transform,
            new Color(0.06f, 0.08f, 0.11f, 0.78f));
        Anchor(_rightPanel, 1f, 1f, 1f, 1f, -328f, -24f, -24f, -676f);
        CreateRightPanelShell();

        _bottomPanel = CreatePanel(
            "Context Panel",
            transform,
            new Color(0.06f, 0.08f, 0.11f, 0.72f));
        Anchor(_bottomPanel, 0.5f, 0f, 0.5f, 0f, -720f, 18f, 720f, 208f);
        _bottomContent = CreateRect("Context Content", _bottomPanel);
        Stretch(_bottomContent, 18f, 18f, -18f, -18f);

        RectTransform statusPanel = CreatePanel(
            "Status Panel",
            transform,
            new Color(0.04f, 0.05f, 0.07f, 0.62f));
        Anchor(statusPanel, 0.5f, 0f, 0.5f, 0f, -480f, 214f, 480f, 254f);
        _statusText = CreateText(
            "Status",
            statusPanel,
            string.Empty,
            17,
            TextAnchor.MiddleCenter);
        Stretch(_statusText.rectTransform, 10f, 4f, -10f, -4f);
        _statusText.raycastTarget = false;
    }

    private void CreateRightPanelShell()
    {
        RectTransform tabs = CreateRect("Tabs", _rightPanel!);
        Anchor(tabs, 0f, 1f, 1f, 1f, 12f, -54f, -12f, -12f);
        HorizontalLayoutGroup layout = tabs.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        _residentTabButton = CreateButton(
            "Residents Tab",
            tabs,
            "Гномы",
            SelectResidentTab);
        _buildingTabButton = CreateButton(
            "Buildings Tab",
            tabs,
            "Строения",
            SelectBuildingTab);

        RectTransform viewport = CreatePanel(
            "Roster Viewport",
            _rightPanel!,
            new Color(0f, 0f, 0f, 0.12f));
        Anchor(viewport, 0f, 0f, 1f, 1f, 12f, 12f, -12f, -66f);
        viewport.gameObject.AddComponent<RectMask2D>();
        _rightContent = CreateRect("Roster Content", viewport);
        _rightContent.anchorMin = new Vector2(0f, 1f);
        _rightContent.anchorMax = new Vector2(1f, 1f);
        _rightContent.pivot = new Vector2(0.5f, 1f);
        _rightContent.offsetMin = Vector2.zero;
        _rightContent.offsetMax = Vector2.zero;
        VerticalLayoutGroup rows = _rightContent.gameObject.AddComponent<VerticalLayoutGroup>();
        rows.spacing = 6f;
        rows.padding = new RectOffset(6, 6, 6, 6);
        rows.childControlHeight = true;
        rows.childControlWidth = true;
        rows.childForceExpandHeight = false;
        rows.childForceExpandWidth = true;
        ContentSizeFitter fitter = _rightContent.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = _rightContent;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
    }

    private static bool Contains(RectTransform? rect, Vector3 screenPoint)
    {
        return rect != null && RectTransformUtility.RectangleContainsScreenPoint(
            rect,
            screenPoint,
            camera: null);
    }
}

}
