using System;
using Dig.Presentation.Rendering;
using UnityEngine;

namespace Dig.Unity
{
[Serializable]
public sealed class DigVfxProfile
{
    [SerializeField] private string stableId = "vfx.missing";
    [SerializeField] private VfxCategory category;
    [SerializeField] private GameObject? prefab;
    [SerializeField, Range(1, 32)] private int maximumInstances = 8;
    [SerializeField, Range(1, 512)] private int maximumParticles = 64;

    public string StableId => stableId;
    public VfxCategory Category => category;
    public GameObject? Prefab => prefab;
    public int MaximumInstances => maximumInstances;
    public int MaximumParticles => maximumParticles;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(stableId))
            throw new InvalidOperationException("VFX profile id is required.");
        if (!Enum.IsDefined(typeof(VfxCategory), category))
            throw new InvalidOperationException("VFX category is invalid.");
        if (maximumInstances < 1 || maximumInstances > 32)
            throw new InvalidOperationException("VFX instance budget must be 1..32.");
        if (maximumParticles < 1 || maximumParticles > 512)
            throw new InvalidOperationException("VFX particle budget must be 1..512.");
        if (prefab != null && prefab.GetComponent<DigVisualPrefabRoot>() == null)
            throw new InvalidOperationException("VFX prefab requires DigVisualPrefabRoot.");
    }
}
}
