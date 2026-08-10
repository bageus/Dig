using System;
using Dig.Presentation.Agents;
using UnityEngine;

namespace Dig.Unity
{
[Serializable]
public sealed class DigResidentVisualProfile
{
    [SerializeField] private string stableId = "resident.default";
    [SerializeField] private ResidentBodyVariant bodyVariant = ResidentBodyVariant.Neutral;
    [SerializeField] private GameObject? prefab;
    [SerializeField] private Material? material;
    [SerializeField] private Color tint = Color.white;
    [SerializeField] private Vector3 scale = Vector3.one;
    [SerializeField, Range(10, 24)] private int maximumRenderers = 12;

    public string StableId => stableId;
    public ResidentBodyVariant BodyVariant => bodyVariant;
    public Vector3 Scale => scale;
    public int MaximumRenderers => maximumRenderers;

    internal DigResidentVisualResolution Resolve(DigVisualAsset fallback)
    {
        DigVisualAsset asset = prefab == null && material == null
            ? fallback
            : new DigVisualAsset(stableId, prefab, material, tint, false);
        return new DigResidentVisualResolution(asset, bodyVariant, scale,
            maximumRenderers, true);
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(stableId))
            throw new InvalidOperationException("Resident profile id is required.");
        if (!Enum.IsDefined(typeof(ResidentBodyVariant), bodyVariant))
            throw new InvalidOperationException("Resident body variant is invalid.");
        if (scale.x <= 0f || scale.y <= 0f || scale.z <= 0f)
            throw new InvalidOperationException("Resident scale must be positive.");
        if (maximumRenderers < 10 || maximumRenderers > 24)
            throw new InvalidOperationException("Resident renderer budget must be 10..24.");
        if (prefab != null && prefab.GetComponent<DigVisualPrefabRoot>() == null)
            throw new InvalidOperationException("Resident prefab requires DigVisualPrefabRoot.");
    }
}
}