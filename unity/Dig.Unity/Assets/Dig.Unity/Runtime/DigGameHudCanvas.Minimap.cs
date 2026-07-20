using Dig.Presentation.World;
using UnityEngine;
using UnityEngine.UI;

namespace Dig.Unity
{

public sealed partial class DigGameHudCanvas
{
    private const float MinimumMinimapZoom = 0.65f;
    private const float MaximumMinimapZoom = 2.5f;
    private Camera? _minimapCamera;
    private RenderTexture? _minimapTexture;
    private RawImage? _minimapImage;
    private float _minimapBaseSize = 10f;
    private float _minimapZoom = 1f;

    private void CreateMinimapShell()
    {
        _minimapPanel = CreatePanel(
            "Minimap Panel",
            transform,
            new Color(0.025f, 0.04f, 0.06f, 0.96f));
        RectTransform viewport = CreatePanel(
            "Minimap Viewport",
            _minimapPanel,
            new Color(0.015f, 0.02f, 0.03f, 1f));
        Anchor(viewport, 0f, 0f, 1f, 1f, 7f, 31f, -7f, -7f);
        RectTransform imageRect = CreateRect("Minimap Image", viewport);
        Stretch(imageRect, 2f, 2f, -2f, -2f);
        _minimapImage = imageRect.gameObject.AddComponent<RawImage>();
        _minimapImage.color = Color.white;
        _minimapImage.raycastTarget = true;
        DigMinimapPointer pointer = imageRect.gameObject.AddComponent<DigMinimapPointer>();
        pointer.Scrolled = ZoomMinimap;

        RectTransform controls = CreateRect("Minimap Controls", _minimapPanel);
        Anchor(controls, 0f, 0f, 1f, 0f, 7f, 5f, -7f, 28f);
        HorizontalLayoutGroup layout = controls.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 5f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = true;
        layout.childForceExpandWidth = false;
        Button zoomOut = CreateButton(
            "Minimap Zoom Out",
            controls,
            "−",
            () => ZoomMinimap(-1f),
            preferredHeight: 22f);
        zoomOut.GetComponent<LayoutElement>().preferredWidth = 34f;
        Text title = CreateText(
            "Minimap Label",
            controls,
            "MAP",
            13,
            TextAnchor.MiddleCenter);
        LayoutElement titleLayout = title.gameObject.AddComponent<LayoutElement>();
        titleLayout.flexibleWidth = 1f;
        titleLayout.preferredHeight = 22f;
        Button zoomIn = CreateButton(
            "Minimap Zoom In",
            controls,
            "+",
            () => ZoomMinimap(1f),
            preferredHeight: 22f);
        zoomIn.GetComponent<LayoutElement>().preferredWidth = 34f;

        InitializeMinimapCamera();
    }

    private void InitializeMinimapCamera()
    {
        if (_minimapCamera != null
            || _mainCamera == null
            || _world == null
            || _minimapImage == null)
        {
            return;
        }

        _minimapTexture = new RenderTexture(
            384,
            256,
            16,
            RenderTextureFormat.ARGB32)
        {
            name = "Dig HUD Minimap",
            filterMode = FilterMode.Bilinear,
        };
        _minimapTexture.Create();
        GameObject cameraObject = new GameObject("HUD Minimap Camera");
        _minimapCamera = cameraObject.AddComponent<Camera>();
        _minimapCamera.CopyFrom(_mainCamera);
        _minimapCamera.targetTexture = _minimapTexture;
        _minimapCamera.orthographic = true;
        _minimapCamera.clearFlags = CameraClearFlags.SolidColor;
        _minimapCamera.backgroundColor = new Color(0.02f, 0.03f, 0.045f, 1f);
        _minimapCamera.depth = -100f;
        _minimapCamera.enabled = true;
        _minimapImage.texture = _minimapTexture;
        FrameMinimap(_world);
    }

    private void FrameMinimap(WorldViewModel world)
    {
        if (_minimapCamera == null || _mainCamera == null)
        {
            return;
        }

        Quaternion rotation = _mainCamera.transform.rotation;
        Vector3 focus = DigSideViewProjection.WorldCenter(world.Width, world.Height);
        float aspect = 384f / 256f;
        float halfHeight = (world.Height * 0.5f) + 2f;
        float halfWidth = (world.Width * 0.5f) + 2f;
        _minimapBaseSize = Mathf.Max(halfHeight, halfWidth / aspect);
        _minimapCamera.transform.rotation = rotation;
        _minimapCamera.transform.position = focus - (rotation * Vector3.forward * 80f);
        ApplyMinimapZoom();
    }

    private void ZoomMinimap(float direction)
    {
        if (Mathf.Abs(direction) < 0.001f)
        {
            return;
        }

        _minimapZoom = Mathf.Clamp(
            _minimapZoom + (Mathf.Sign(direction) * 0.15f),
            MinimumMinimapZoom,
            MaximumMinimapZoom);
        ApplyMinimapZoom();
    }

    private void ApplyMinimapZoom()
    {
        if (_minimapCamera != null)
        {
            _minimapCamera.orthographicSize = _minimapBaseSize / _minimapZoom;
        }
    }

    private void DisposeMinimap()
    {
        if (_minimapCamera != null)
        {
            Destroy(_minimapCamera.gameObject);
            _minimapCamera = null;
        }

        if (_minimapTexture != null)
        {
            _minimapTexture.Release();
            Destroy(_minimapTexture);
            _minimapTexture = null;
        }
    }
}

}
