using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace Dig.Unity
{
[CreateAssetMenu(fileName = "VfxCatalog", menuName = "Dig/Visual Catalogs/VFX")]
public sealed class DigVfxCatalog : ScriptableObject
{
    [SerializeField] private DigVfxProfile[] profiles = Array.Empty<DigVfxProfile>();
    private Dictionary<string, DigVfxProfile>? _lookup;

    public bool TryResolve(string stableId, out DigVfxProfile profile)
    {
        EnsureLookup();
        DigVfxProfile? found;
        if (!string.IsNullOrWhiteSpace(stableId)
            && _lookup!.TryGetValue(stableId, out found))
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
        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        for (int index = 0; index < profiles.Length; index++)
        {
            DigVfxProfile? profile = profiles[index];
            if (profile == null)
            {
                errors.Add($"VFX profile {index} is null.");
                continue;
            }
            try { profile.Validate(); }
            catch (InvalidOperationException exception)
            {
                errors.Add($"VFX profile {index}: {exception.Message}");
            }
            if (!ids.Add(profile.StableId))
                errors.Add($"Duplicate VFX profile id '{profile.StableId}'.");
        }
        return new ReadOnlyCollection<string>(errors);
    }

    private void EnsureLookup()
    {
        if (_lookup != null) return;
        _lookup = new Dictionary<string, DigVfxProfile>(StringComparer.Ordinal);
        for (int index = 0; index < profiles.Length; index++)
        {
            DigVfxProfile? profile = profiles[index];
            if (profile == null || string.IsNullOrWhiteSpace(profile.StableId)
                || _lookup.ContainsKey(profile.StableId)) continue;
            _lookup.Add(profile.StableId, profile);
        }
    }

    private void OnEnable() => _lookup = null;
    private void OnValidate() => _lookup = null;
}
}
