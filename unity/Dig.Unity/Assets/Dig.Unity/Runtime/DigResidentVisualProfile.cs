using System;
using UnityEngine;

namespace Dig.Unity
{
[Serializable]
public sealed class DigResidentVisualProfile
{
    [SerializeField] private string stableId = "resident.default";
    [SerializeField] private GameObject? prefab;
    [SerializeField] private Material? material;
    [SerializeField] private Vector3 scale = Vector3.one;
    [SerializeField, Range(4, 24)] private int maximumRenderers = 12;

    public string StableId => stableId;
    public GameObject? Prefab => prefab;
    public Material? Material => material;
    public Vector3 Scale => scale;
    public int MaximumRenderers => maximumRenderers;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(stableId))
            throw new InvalidOperationException("Resident profile id is required.");
        if (scale.x <= 0f || scale.y <= 0f || scale.z <= 0f)
            throw new InvalidOperationException("Resident scale must be positive.");
        if (maximumRenderers < 4 || maximumRenderers > 24)
            throw new InvalidOperationException("Resident renderer budget must be 4..24.");
        if (prefab != null && prefab.GetComponent<DigVisualPrefabRoot>() == null)
            throw new InvalidOperationException("Resident prefab requires DigVisualPrefabRoot.");
    }
}
}
