using System;
using Dig.Presentation.Creatures;
using UnityEngine;

namespace Dig.Unity
{
[Serializable]
public sealed class DigCreatureVisualProfile
{
    [SerializeField] private string stableSpeciesId = "creature.hamster";
    [SerializeField] private string rigStableId = "creature.rig.small";
    [SerializeField] private CreatureVisualFamily family = CreatureVisualFamily.SmallCreature;
    [SerializeField] private GameObject? prefab;
    [SerializeField] private Material? material;
    [SerializeField] private Color tint = Color.white;
    [SerializeField] private Vector3 scale = Vector3.one;
    [SerializeField, Range(3, 32)] private int maximumRenderers = 12;

    public string StableSpeciesId => stableSpeciesId;
    public string RigStableId => rigStableId;
    public CreatureVisualFamily Family => family;
    public Vector3 Scale => scale;
    public int MaximumRenderers => maximumRenderers;

    internal DigCreatureVisualResolution Resolve(DigVisualAsset fallback)
    {
        DigVisualAsset asset = prefab == null && material == null
            ? fallback
            : new DigVisualAsset(stableSpeciesId, prefab, material, tint, false);
        return new DigCreatureVisualResolution(
            asset, rigStableId, family, scale, maximumRenderers, true);
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(stableSpeciesId))
            throw new InvalidOperationException("Creature species id is required.");
        if (string.IsNullOrWhiteSpace(rigStableId))
            throw new InvalidOperationException("Creature rig id is required.");
        if (!Enum.IsDefined(typeof(CreatureVisualFamily), family))
            throw new InvalidOperationException("Creature family is invalid.");
        if (scale.x <= 0f || scale.y <= 0f || scale.z <= 0f)
            throw new InvalidOperationException("Creature scale must be positive.");
        if (maximumRenderers < 3 || maximumRenderers > 32)
            throw new InvalidOperationException("Creature renderer budget must be 3..32.");
        if (prefab != null && prefab.GetComponent<DigVisualPrefabRoot>() == null)
            throw new InvalidOperationException("Creature prefab requires DigVisualPrefabRoot.");
    }
}
}