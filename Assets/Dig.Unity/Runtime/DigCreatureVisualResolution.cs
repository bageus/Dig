using Dig.Presentation.Creatures;
using UnityEngine;

namespace Dig.Unity
{
public readonly struct DigCreatureVisualResolution
{
    public DigCreatureVisualResolution(
        DigVisualAsset asset,
        string rigStableId,
        CreatureVisualFamily family,
        Vector3 scale,
        int maximumRenderers,
        bool hasProfile)
    {
        Asset = asset;
        RigStableId = rigStableId;
        Family = family;
        Scale = scale;
        MaximumRenderers = maximumRenderers;
        HasProfile = hasProfile;
    }

    public DigVisualAsset Asset { get; }
    public string RigStableId { get; }
    public CreatureVisualFamily Family { get; }
    public Vector3 Scale { get; }
    public int MaximumRenderers { get; }
    public bool HasProfile { get; }
}
}