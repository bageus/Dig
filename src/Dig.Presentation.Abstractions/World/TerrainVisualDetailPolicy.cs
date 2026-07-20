using System;

namespace Dig.Presentation.World
{

public enum TerrainVisualDetailLevel
{
    Marker = 0,
    Reduced = 1,
    Full = 2,
}

public sealed class TerrainVisualDetailPolicy
{
    public const float DefaultReducedExitPixelsPerCell = 8f;
    public const float DefaultReducedEnterPixelsPerCell = 12f;
    public const float DefaultFullExitPixelsPerCell = 22f;
    public const float DefaultFullEnterPixelsPerCell = 28f;

    private readonly float _reducedExit;
    private readonly float _reducedEnter;
    private readonly float _fullExit;
    private readonly float _fullEnter;

    public TerrainVisualDetailPolicy(
        float reducedExitPixelsPerCell = DefaultReducedExitPixelsPerCell,
        float reducedEnterPixelsPerCell = DefaultReducedEnterPixelsPerCell,
        float fullExitPixelsPerCell = DefaultFullExitPixelsPerCell,
        float fullEnterPixelsPerCell = DefaultFullEnterPixelsPerCell)
    {
        if (!IsFiniteNonNegative(reducedExitPixelsPerCell)
            || !IsFiniteNonNegative(reducedEnterPixelsPerCell)
            || !IsFiniteNonNegative(fullExitPixelsPerCell)
            || !IsFiniteNonNegative(fullEnterPixelsPerCell))
        {
            throw new ArgumentOutOfRangeException(
                nameof(reducedExitPixelsPerCell),
                "Terrain visual detail thresholds must be finite and non-negative.");
        }

        if (reducedExitPixelsPerCell >= reducedEnterPixelsPerCell
            || reducedEnterPixelsPerCell > fullExitPixelsPerCell
            || fullExitPixelsPerCell >= fullEnterPixelsPerCell)
        {
            throw new ArgumentException(
                "Terrain visual detail thresholds must satisfy "
                + "reduced-exit < reduced-enter <= full-exit < full-enter.");
        }

        _reducedExit = reducedExitPixelsPerCell;
        _reducedEnter = reducedEnterPixelsPerCell;
        _fullExit = fullExitPixelsPerCell;
        _fullEnter = fullEnterPixelsPerCell;
    }

    public TerrainVisualDetailLevel Resolve(
        float pixelsPerCell,
        TerrainVisualDetailLevel current)
    {
        if (!IsFiniteNonNegative(pixelsPerCell))
        {
            throw new ArgumentOutOfRangeException(nameof(pixelsPerCell));
        }

        if (!Enum.IsDefined(typeof(TerrainVisualDetailLevel), current))
        {
            throw new ArgumentOutOfRangeException(nameof(current));
        }

        switch (current)
        {
            case TerrainVisualDetailLevel.Full:
                if (pixelsPerCell >= _fullExit)
                {
                    return TerrainVisualDetailLevel.Full;
                }

                return pixelsPerCell >= _reducedExit
                    ? TerrainVisualDetailLevel.Reduced
                    : TerrainVisualDetailLevel.Marker;

            case TerrainVisualDetailLevel.Reduced:
                if (pixelsPerCell >= _fullEnter)
                {
                    return TerrainVisualDetailLevel.Full;
                }

                return pixelsPerCell >= _reducedExit
                    ? TerrainVisualDetailLevel.Reduced
                    : TerrainVisualDetailLevel.Marker;

            default:
                if (pixelsPerCell >= _fullEnter)
                {
                    return TerrainVisualDetailLevel.Full;
                }

                return pixelsPerCell >= _reducedEnter
                    ? TerrainVisualDetailLevel.Reduced
                    : TerrainVisualDetailLevel.Marker;
        }
    }

    private static bool IsFiniteNonNegative(float value)
    {
        return value >= 0f && !float.IsNaN(value) && !float.IsInfinity(value);
    }
}

}
