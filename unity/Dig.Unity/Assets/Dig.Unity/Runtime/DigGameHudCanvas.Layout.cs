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
    private int _lastLayoutWidth = -1;
    private int _lastLayoutHeight = -1;

    private void ApplyResponsiveLayout(bool force = false)
    {
        int width = Screen.width;
        int height = Screen.height;
        if (!force && width == _lastLayoutWidth && height == _lastLayoutHeight)
        {
            return;
        }

        _lastLayoutWidth = width;
        _lastLayoutHeight = height;
        float sideWidth = Mathf.Clamp(
            width * 0.17f,
            MinimumSidePanelWidth,
            MaximumSidePanelWidth);
        float sideHeight = Mathf.Clamp(
            height * 0.24f,
            MinimumSidePanelHeight,
            MaximumSidePanelHeight);
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
            margin,
            -50f,
            -(rosterWidth + (margin * 2f)),
            -12f);
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
            -60f);
        Anchor(
            _bottomPanel!,
            0f,
            0f,
            1f,
            0f,
            margin + sideWidth + gap,
            margin,
            -(margin + sideWidth + gap),
            margin + _bottomPanelHeight);
    }

    private void SetBottomPanelHeight(float height)
    {
        _bottomPanelHeight = Mathf.Max(76f, height);
        ApplyResponsiveLayout(force: true);
    }
}

}