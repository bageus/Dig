using UnityEngine;

namespace Dig.Unity
{

public sealed partial class DigGameHudCanvas
{
    private const float MinimumSidePanelWidth = 154f;
    private const float MaximumSidePanelWidth = 226f;
    private const float MinimumSidePanelHeight = 132f;
    private const float MaximumSidePanelHeight = 188f;
    private const float MinimumRosterWidth = 268f;
    private const float MaximumRosterWidth = 360f;
    private const float TopHudOffset = -12f;
    private int _lastLayoutWidth = -1;
    private int _lastLayoutHeight = -1;

    private void ApplyResponsiveLayout(bool force = false)
    {
        RectTransform canvasRect = (RectTransform)transform;
        int width = Mathf.RoundToInt(Mathf.Max(640f, canvasRect.rect.width));
        int height = Mathf.RoundToInt(Mathf.Max(360f, canvasRect.rect.height));
        bool dimensionsChanged = width != _lastLayoutWidth || height != _lastLayoutHeight;
        if (!force && !dimensionsChanged)
        {
            return;
        }

        if (dimensionsChanged)
        {
            _lastContextSignature = string.Empty;
        }

        _lastLayoutWidth = width;
        _lastLayoutHeight = height;
        float sideWidth = Mathf.Clamp(
            width * 0.17f,
            MinimumSidePanelWidth,
            MaximumSidePanelWidth);
        float sideHeight = ResolveBottomHudHeight(height);
        _bottomPanelHeight = sideHeight;
        float rosterWidth = Mathf.Clamp(
            width * 0.27f,
            MinimumRosterWidth,
            MaximumRosterWidth);
        if (width < 900)
        {
            sideWidth = MinimumSidePanelWidth;
            rosterWidth = MinimumRosterWidth;
        }

        const float margin = 14f;
        const float gap = 12f;
        Anchor(
            _statusPanel!,
            0f,
            1f,
            1f,
            1f,
            margin + ManagementMenuWidth + gap,
            -50f,
            -(rosterWidth + (margin * 2f)),
            TopHudOffset);
        Anchor(
            _minimapPanel!,
            0f,
            0f,
            0f,
            0f,
            margin,
            margin,
            margin + sideWidth,
            margin + sideHeight);
        Anchor(
            _clockPanel!,
            1f,
            0f,
            1f,
            0f,
            -(margin + sideWidth),
            margin,
            -margin,
            margin + sideHeight);
        Anchor(
            _rightPanel!,
            1f,
            0f,
            1f,
            1f,
            -(margin + rosterWidth),
            margin + sideHeight + gap,
            -margin,
            TopHudOffset);
        Anchor(
            _bottomPanel!,
            0f,
            0f,
            1f,
            0f,
            margin + sideWidth + gap,
            margin,
            -(margin + sideWidth + gap),
            margin + sideHeight);
    }

    internal static float ResolveBottomHudHeight(float canvasHeight)
    {
        return Mathf.Clamp(
            canvasHeight * 0.24f,
            MinimumSidePanelHeight,
            MaximumSidePanelHeight);
    }

    private void SetBottomPanelHeight(float height)
    {
        // Context builders may request denser inner content, but the outer shell
        // always follows the responsive minimap/clock height in ApplyResponsiveLayout.
        _bottomPanelHeight = Mathf.Max(76f, height);
        ApplyResponsiveLayout(force: true);
        RefreshContextHoverInfo();
    }
}

}
