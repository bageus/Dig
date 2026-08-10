using Dig.Presentation.Agents;
using UnityEngine;

namespace Dig.Unity
{
public readonly struct DigResidentVisualResolution
{
    public DigResidentVisualResolution(
        DigVisualAsset asset,
        ResidentBodyVariant bodyVariant,
        Vector3 scale,
        int maximumRenderers,
        bool hasProfile)
    {
        Asset = asset;
        BodyVariant = bodyVariant;
        Scale = scale;
        MaximumRenderers = maximumRenderers;
        HasProfile = hasProfile;
    }

    public DigVisualAsset Asset { get; }
    public ResidentBodyVariant BodyVariant { get; }
    public Vector3 Scale { get; }
    public int MaximumRenderers { get; }
    public bool HasProfile { get; }
}
}
