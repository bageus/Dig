using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dig.Presentation.Rendering;
using UnityEngine;

namespace Dig.Unity
{
[CreateAssetMenu(fileName = "RenderMaterialCatalog",
    menuName = "Dig/Visual Catalogs/Render Materials")]
public sealed class DigRenderMaterialCatalog : ScriptableObject
{
    [SerializeField] private DigRenderMaterialProfile[] profiles =
        Array.Empty<DigRenderMaterialProfile>();
    private Dictionary<string, DigRenderMaterialProfile>? _lookup;

    public bool TryResolve(RenderMaterialSemantic semantic,
        RenderSurfaceKind surface, out DigRenderMaterialProfile profile)
    {
        EnsureLookup();
        DigRenderMaterialProfile? found;
        if (_lookup!.TryGetValue(Key(semantic, surface), out found))
        {
            profile = found;
            return true;
        }
        profile = null!;
        return false;
    }

    public IReadOnlyList<string> ValidateCatalog()
    {
        List<string> errors = new List<string>();
        HashSet<string> keys = new HashSet<string>(StringComparer.Ordinal);
        for (int index = 0; index < profiles.Length; index++)
        {
            DigRenderMaterialProfile? profile = profiles[index];
            if (profile == null)
            {
                errors.Add($"Render material profile {index} is null.");
                continue;
            }
            try { profile.Validate(); }
            catch (InvalidOperationException exception)
            {
                errors.Add($"Render material profile {index}: {exception.Message}");
            }
            if (!keys.Add(profile.StableKey))
                errors.Add($"Duplicate render material profile '{profile.StableKey}'.");
        }
        return new ReadOnlyCollection<string>(errors);
    }

    private void EnsureLookup()
    {
        if (_lookup != null) return;
        _lookup = new Dictionary<string, DigRenderMaterialProfile>(StringComparer.Ordinal);
        for (int index = 0; index < profiles.Length; index++)
        {
            DigRenderMaterialProfile? profile = profiles[index];
            if (profile == null || _lookup.ContainsKey(profile.StableKey)) continue;
            _lookup.Add(profile.StableKey, profile);
        }
    }

    private static string Key(RenderMaterialSemantic semantic,
        RenderSurfaceKind surface) => semantic + ":" + surface;

    private void OnEnable() => _lookup = null;
    private void OnValidate() => _lookup = null;
}
}
