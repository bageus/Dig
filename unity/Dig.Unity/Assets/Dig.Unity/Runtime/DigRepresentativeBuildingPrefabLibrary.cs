using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dig.Presentation.Buildings;
using UnityEngine;

namespace Dig.Unity
{
    internal sealed partial class DigRepresentativeBuildingPrefabLibrary : IDisposable
    {
        private const string PackResourcePath =
            "Dig/VisualCatalogs/RepresentativeBuildings";

        private static DigRepresentativeBuildingPrefabLibrary? _shared;
        private static int _referenceCount;

        private readonly Dictionary<string, DigRepresentativeBuildingProfileData> _profiles =
            new Dictionary<string, DigRepresentativeBuildingProfileData>(StringComparer.Ordinal);
        private readonly Dictionary<string, GameObject> _templates =
            new Dictionary<string, GameObject>(StringComparer.Ordinal);
        private readonly DigRepresentativeBuildingMeshFactory _meshes =
            new DigRepresentativeBuildingMeshFactory();
        private readonly GameObject _templateRoot;
        private readonly Material _material;
        private readonly IReadOnlyList<string> _validationErrors;
        private bool _disposed;

        private DigRepresentativeBuildingPrefabLibrary()
        {
            _templateRoot = new GameObject("Dig Representative Building Templates")
            {
                hideFlags = HideFlags.HideAndDontSave,
            };
            _templateRoot.SetActive(false);
            _material = CreateMaterial();

            TextAsset? source = Resources.Load<TextAsset>(PackResourcePath);
            DigRepresentativeBuildingPackData? pack = source == null
                ? null
                : JsonUtility.FromJson<DigRepresentativeBuildingPackData>(source.text);
            List<string> errors = new List<string>(
                DigRepresentativeBuildingDataValidator.Validate(pack));
            if (source == null)
            {
                errors.Insert(0, $"Missing Resources/{PackResourcePath}.json.");
            }

            if (pack != null)
            {
                IndexProfiles(pack, errors);
            }

            _validationErrors = new ReadOnlyCollection<string>(errors);
        }

        internal IReadOnlyList<string> ValidationErrors => _validationErrors;

        internal int TemplateCount => _templates.Count;

        internal static DigRepresentativeBuildingPrefabLibrary Acquire()
        {
            _shared ??= new DigRepresentativeBuildingPrefabLibrary();
            _referenceCount++;
            return _shared;
        }

        internal bool TryResolve(
            string stableId,
            BuildingVisualState state,
            out DigBuildingVisualResolution resolution)
        {
            if (_disposed
                || string.IsNullOrWhiteSpace(stableId)
                || !_profiles.TryGetValue(
                    stableId,
                    out DigRepresentativeBuildingProfileData? profile))
            {
                resolution = default;
                return false;
            }

            string canonicalId = profile.stableIds[0].Trim();
            string key = $"{canonicalId}:{state}";
            if (!_templates.TryGetValue(key, out GameObject? template))
            {
                template = BuildTemplate(canonicalId, profile, state);
                _templates.Add(key, template);
            }

            DigVisualAsset asset = new DigVisualAsset(
                $"representative:{key}",
                template,
                _material,
                profile.tint,
                isFallback: false);
            resolution = new DigBuildingVisualResolution(
                asset,
                profile.footprintSize,
                profile.pivotCell,
                hasProfile: true);
            return true;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            if (_shared == this && _referenceCount > 1)
            {
                _referenceCount--;
                return;
            }

            _disposed = true;
            _referenceCount = 0;
            _shared = null;
            _templates.Clear();
            _profiles.Clear();
            _meshes.Dispose();
            if (_material != null)
            {
                UnityEngine.Object.Destroy(_material);
            }

            if (_templateRoot != null)
            {
                UnityEngine.Object.Destroy(_templateRoot);
            }
        }

        private void IndexProfiles(
            DigRepresentativeBuildingPackData pack,
            ICollection<string> errors)
        {
            for (int profileIndex = 0; profileIndex < pack.profiles.Length; profileIndex++)
            {
                DigRepresentativeBuildingProfileData? profile = pack.profiles[profileIndex];
                if (profile == null || profile.stableIds == null)
                {
                    continue;
                }

                for (int idIndex = 0; idIndex < profile.stableIds.Length; idIndex++)
                {
                    string id = profile.stableIds[idIndex] ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(id) || _profiles.ContainsKey(id.Trim()))
                    {
                        continue;
                    }

                    _profiles.Add(id.Trim(), profile);
                }
            }

            if (_profiles.Count == 0 && errors.Count == 0)
            {
                errors.Add("Representative building pack produced no usable aliases.");
            }
        }

        private static Material CreateMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            if (shader == null)
            {
                throw new InvalidOperationException(
                    "No supported representative building shader was found.");
            }

            return new Material(shader)
            {
                name = "Dig Representative Building Shared",
                color = Color.white,
                enableInstancing = true,
                hideFlags = HideFlags.HideAndDontSave,
            };
        }
    }
}
