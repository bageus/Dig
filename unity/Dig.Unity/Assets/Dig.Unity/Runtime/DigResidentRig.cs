using System;
using Dig.Presentation.Agents;
using UnityEngine;

namespace Dig.Unity
{
public enum DigResidentSocketKind
{
    Head = 0,
    LeftHand = 1,
    RightHand = 2,
    Back = 3,
    Cargo = 4,
    Vfx = 5,
}

[DisallowMultipleComponent]
public sealed class DigResidentRig : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private Renderer[] _renderers = Array.Empty<Renderer>();
    private Transform? _leftArm;
    private Transform? _rightArm;
    private Transform? _leftLeg;
    private Transform? _rightLeg;
    private Transform[] _sockets = Array.Empty<Transform>();
    private MaterialPropertyBlock? _properties;
    private ResidentAppearanceViewModel? _appearance;
    private bool _selected;

    internal void Initialize(
        Renderer[] renderers,
        Transform leftArm,
        Transform rightArm,
        Transform leftLeg,
        Transform rightLeg,
        Transform[] sockets)
    {
        if (renderers == null || renderers.Length < 4 || renderers.Length > 24)
            throw new ArgumentOutOfRangeException(nameof(renderers));
        if (sockets == null || sockets.Length != 6)
            throw new ArgumentException("Resident rig requires six sockets.", nameof(sockets));
        _renderers = renderers;
        _leftArm = leftArm;
        _rightArm = rightArm;
        _leftLeg = leftLeg;
        _rightLeg = rightLeg;
        _sockets = sockets;
    }

    internal Transform ResolveSocket(DigResidentSocketKind kind)
    {
        int index = (int)kind;
        return index >= 0 && index < _sockets.Length ? _sockets[index] : transform;
    }

    internal void ApplyAppearance(ResidentAppearanceViewModel appearance)
    {
        _appearance = appearance ?? throw new ArgumentNullException(nameof(appearance));
        RefreshColors();
    }

    internal void SetSelected(bool selected)
    {
        _selected = selected;
        RefreshColors();
    }

    internal void ApplyAction(ResidentActionVisualViewModel action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        float phase = (float)(action.NormalizedProgress * Mathf.PI * 2f);
        float swing = Mathf.Sin(phase) * 28f;
        ResetPose();
        switch (action.State)
        {
            case ResidentActionVisualState.Walk:
                SetLimbPose(swing, -swing, -swing, swing);
                break;
            case ResidentActionVisualState.Dig:
                SetLimbPose(-55f, -55f, 8f, -8f);
                break;
            case ResidentActionVisualState.Carry:
                SetLimbPose(-30f, -30f, 0f, 0f);
                break;
            case ResidentActionVisualState.Build:
                SetLimbPose(-35f + swing * 0.2f, -20f, 0f, 0f);
                break;
            case ResidentActionVisualState.Pickup:
                transform.localRotation = Quaternion.Euler(18f, 0f, 0f);
                break;
            case ResidentActionVisualState.Drop:
                transform.localRotation = Quaternion.Euler(-10f, 0f, 0f);
                break;
            case ResidentActionVisualState.Hit:
                transform.localRotation = Quaternion.Euler(0f, 0f, 12f);
                break;
            case ResidentActionVisualState.Death:
                transform.localRotation = Quaternion.Euler(0f, 0f, 82f);
                break;
        }
    }

    private void SetLimbPose(float leftArm, float rightArm, float leftLeg, float rightLeg)
    {
        _leftArm!.localRotation = Quaternion.Euler(leftArm, 0f, 0f);
        _rightArm!.localRotation = Quaternion.Euler(rightArm, 0f, 0f);
        _leftLeg!.localRotation = Quaternion.Euler(leftLeg, 0f, 0f);
        _rightLeg!.localRotation = Quaternion.Euler(rightLeg, 0f, 0f);
    }

    private void ResetPose()
    {
        transform.localRotation = Quaternion.identity;
        if (_leftArm == null) return;
        SetLimbPose(0f, 0f, 0f, 0f);
    }

    private void RefreshColors()
    {
        if (_appearance == null) return;
        Color clothing = ClothingColor(_appearance.ClothingPaletteIndex);
        Color hair = HairColor(_appearance.HairPaletteIndex);
        Color skin = _appearance.BodyVariant == ResidentBodyVariant.Feminine
            ? new Color(0.82f, 0.62f, 0.48f, 1f)
            : new Color(0.76f, 0.55f, 0.40f, 1f);
        MaterialPropertyBlock properties = ResolveProperties();
        for (int index = 0; index < _renderers.Length; index++)
        {
            Renderer renderer = _renderers[index];
            string part = renderer.gameObject.name;
            Color color = part.Contains("Hair", StringComparison.Ordinal)
                || part.Contains("Headwear", StringComparison.Ordinal)
                ? hair
                : part.Contains("Head", StringComparison.Ordinal)
                    || part.Contains("Hand", StringComparison.Ordinal)
                    ? skin
                    : clothing;
            if (_selected) color = Color.Lerp(color, new Color(1f, 0.78f, 0.18f, 1f), 0.55f);
            properties.Clear();
            renderer.GetPropertyBlock(properties);
            properties.SetColor(BaseColorId, color);
            properties.SetColor(ColorId, color);
            renderer.SetPropertyBlock(properties);
        }
    }

    private MaterialPropertyBlock ResolveProperties()
    {
        if (_properties == null)
        {
            _properties = new MaterialPropertyBlock();
        }

        return _properties;
    }

    private static Color ClothingColor(int index)
    {
        Color[] colors =
        {
            new Color(0.18f, 0.52f, 0.70f), new Color(0.52f, 0.30f, 0.18f),
            new Color(0.24f, 0.58f, 0.34f), new Color(0.56f, 0.24f, 0.48f),
            new Color(0.65f, 0.50f, 0.18f), new Color(0.30f, 0.34f, 0.62f),
            new Color(0.52f, 0.56f, 0.60f), new Color(0.46f, 0.22f, 0.16f),
        };
        return colors[Mathf.Clamp(index, 0, colors.Length - 1)];
    }

    private static Color HairColor(int index)
    {
        Color[] colors =
        {
            new Color(0.12f, 0.08f, 0.05f), new Color(0.30f, 0.16f, 0.07f),
            new Color(0.55f, 0.32f, 0.10f), new Color(0.70f, 0.58f, 0.30f),
            new Color(0.55f, 0.55f, 0.52f), new Color(0.82f, 0.82f, 0.78f),
        };
        return colors[Mathf.Clamp(index, 0, colors.Length - 1)];
    }
}
}
