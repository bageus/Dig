using System;
using Dig.Presentation.Inventory;
using Dig.Presentation.World;
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
        Jobs = 2,
    }

    private DigTerrainWorkSession? _terrainSession;
    private DigAgentRenderer? _agentRenderer;
    private DigJobRenderer? _jobRenderer;
    private DigBuildingRenderer? _buildingRenderer;
    private DigWorldInteraction? _interaction;
    private DigAgentSimulationDriver? _simulation;
    private DigHudOverlay? _legacyHud;
    private Camera? _mainCamera;
    private WorldViewModel? _world;
    private Canvas? _canvas;
    private RectTransform? _rightPanel;
    private RectTransform? _rightContent;
    private RectTransform? _bottomPanel;
    private RectTransform? _bottomContent;
    private RectTransform? _statusPanel;
    private RectTransform? _minimapPanel;
    private RectTransform? _clockPanel;
    private Text? _statusText;
    private Button? _residentTabButton;
    private Button? _buildingTabButton;
    private Button? _jobTabButton;
    private RightPanelTab _rightTab;
    private string _lastRosterSignature = string.Empty;
    private string _lastContextSignature = string.Empty;
    private string _status = string.Empty;
    private float _bottomPanelHeight = 98f;
    private bool _initialized;

    internal void Initialize(
        DigTerrainWorkSession terrainSession,
        DigAgentRenderer agentRenderer,
        DigBuildingRenderer buildingRenderer,
        DigWorldInteraction interaction,
        DigAgentSimulationDriver simulation,
        DigHudOverlay legacyHud)
    {
        if (agentRenderer == null)
        {
            throw new ArgumentNullException(nameof(agentRenderer));
        }

        DigJobRenderer jobRenderer = agentRenderer.GetComponent<DigJobRenderer>()
            ?? throw new InvalidOperationException(
                "The job renderer must exist before the game HUD is initialized.");
        Initialize(
            terrainSession,
            agentRenderer,
            jobRenderer,
            buildingRenderer,
            interaction,
            simulation,
            legacyHud,
            Camera.main,
            world: null);
    }

    internal void Initialize(
        DigTerrainWorkSession terrainSession,
        DigAgentRenderer agentRenderer,
        DigJobRenderer jobRenderer,
        DigBuildingRenderer buildingRenderer,
        DigWorldInteraction interaction,
        DigAgentSimulationDriver simulation,
        DigHudOverlay legacyHud)
    {
        Initialize(
            terrainSession,
            agentRenderer,
            jobRenderer,
            buildingRenderer,
            interaction,
            simulation,
            legacyHud,
            Camera.main,
            world: null);
    }

    internal void Initialize(
        DigTerrainWorkSession terrainSession,
        DigAgentRenderer agentRenderer,
        DigJobRenderer jobRenderer,
        DigBuildingRenderer buildingRenderer,
        DigWorldInteraction interaction,
        DigAgentSimulationDriver simulation,
        DigHudOverlay legacyHud,
        Camera? mainCamera,
        WorldViewModel? world)
    {
        _terrainSession = terrainSession
            ?? throw new ArgumentNullException(nameof(terrainSession));
        _agentRenderer = agentRenderer
            ?? throw new ArgumentNullException(nameof(agentRenderer));
        _jobRenderer = jobRenderer
            ?? throw new ArgumentNullException(nameof(jobRenderer));
        _buildingRenderer = buildingRenderer
            ?? throw new ArgumentNullException(nameof(buildingRenderer));
        _interaction = interaction
            ?? throw new ArgumentNullException(nameof(interaction));
        _simulation = simulation
            ?? throw new ArgumentNullException(nameof(simulation));
        _legacyHud = legacyHud
            ?? throw new ArgumentNullException(nameof(legacyHud));
        _mainCamera = mainCamera;
        _world = world;
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
            || Contains(_minimapPanel, screenPoint)
            || Contains(_clockPanel, screenPoint)
            || Contains(_statusPanel, screenPoint)
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
        InvalidateClock();
    }

    private void LateUpdate()
    {
        if (!_initialized)
        {
            return;
        }

        ApplyResponsiveLayout();
        RefreshClock();
        RefreshRoster();
        RefreshContextPanel();
    }

    private void OnDestroy()
    {
        DisposeMinimap();
    }

    private void CreateCanvasShell()
    {
        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 100;
        _canvas.pixelPerfect = true;
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        scaler.referencePixelsPerUnit = 100f;
        gameObject.AddComponent<GraphicRaycaster>();
        EnsureEventSystem();

        _rightPanel = CreatePanel(
            "Roster Panel",
            transform,
            new Color(0.035f, 0.05f, 0.075f, 0.94f));
        CreateRightPanelShell();

        _bottomPanel = CreatePanel(
            "Context Panel",
            transform,
            new Color(0.035f, 0.05f, 0.075f, 0.95f));
        _bottomContent = CreateRect("Context Content", _bottomPanel);
        Stretch(_bottomContent, 8f, 8f, -8f, -8f);

        _statusPanel = CreatePanel(
            "Notification Ticker",
            transform,
            new Color(0.02f, 0.03f, 0.045f, 0.90f));
        _statusPanel.GetComponent<Image>().raycastTarget = false;
        _statusText = CreateText(
            "Notification",
            _statusPanel,
            string.Empty,
            16,
            TextAnchor.MiddleCenter);
        Stretch(_statusText.rectTransform, 10f, 3f, -10f, -3f);
        _statusText.raycastTarget = false;

        CreateMinimapShell();
        CreateClockShell();
        ApplyResponsiveLayout(force: true);
    }

    private void CreateRightPanelShell()
    {
        RectTransform tabs = CreateRect("Tabs", _rightPanel!);
        Anchor(tabs, 0f, 1f, 1f, 1f, 8f, -48f, -8f, -8f);
        HorizontalLayoutGroup layout = tabs.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 5f;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        _residentTabButton = CreateButton(
            "Residents Tab",
            tabs,
            "●",
            SelectResidentTab,
            preferredHeight: 38f);
        _buildingTabButton = CreateButton(
            "Buildings Tab",
            tabs,
            "■",
            SelectBuildingTab,
            preferredHeight: 38f);
        _jobTabButton = CreateButton(
            "Jobs Tab",
            tabs,
            "▲",
            SelectJobTab,
            preferredHeight: 38f);

        RectTransform viewport = CreatePanel(
            "Roster Viewport",
            _rightPanel!,
            new Color(0f, 0f, 0f, 0.12f));
        Anchor(viewport, 0f, 0f, 1f, 1f, 8f, 8f, -8f, -56f);
        viewport.gameObject.AddComponent<RectMask2D>();
        _rightContent = CreateRect("Roster Content", viewport);
        _rightContent.anchorMin = new Vector2(0f, 1f);
        _rightContent.anchorMax = new Vector2(1f, 1f);
        _rightContent.pivot = new Vector2(0.5f, 1f);
        _rightContent.offsetMin = Vector2.zero;
        _rightContent.offsetMax = Vector2.zero;
        VerticalLayoutGroup rows = _rightContent.gameObject.AddComponent<VerticalLayoutGroup>();
        rows.spacing = 4f;
        rows.padding = new RectOffset(4, 4, 4, 4);
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
            (Vector2)screenPoint,
            null);
    }
}

}