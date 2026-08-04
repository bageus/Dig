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

            DigRepresentativeBuildingPackData pack = LoadPack();
            List<string> errors = new List<string>(
                DigRepresentativeBuildingDataValidator.Validate(pack));
            IndexProfiles(pack, errors);
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
            bool facesCamera = profile.TryResolveKind(out DigBuildingProfileKind kind)
                && kind == DigBuildingProfileKind.Tent;
            resolution = new DigBuildingVisualResolution(
                asset,
                profile.footprintSize,
                profile.pivotCell,
                hasProfile: true,
                facesCamera: facesCamera);
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

        private static DigRepresentativeBuildingPackData CreateBuiltInPack()
        {
            return new DigRepresentativeBuildingPackData
            {
                profiles = CreateBuiltInProfiles(),
            };
        }

        private static DigRepresentativeBuildingPackData LoadPack()
        {
            TextAsset? source = Resources.Load<TextAsset>(PackResourcePath);
            if (source == null)
            {
                return CreateBuiltInPack();
            }

            DigRepresentativeBuildingPackData? pack =
                JsonUtility.FromJson<DigRepresentativeBuildingPackData>(source.text);
            return pack == null || pack.profiles == null || pack.profiles.Length == 0
                ? CreateBuiltInPack()
                : pack;
        }

        private static DigRepresentativeBuildingProfileData Profile(
            string[] stableIds,
            string kind,
            Vector2Int footprint,
            Vector2 pivot,
            Vector3 visualBoundsCenter,
            Vector3 visualBoundsSize,
            Color tint,
            DigRepresentativeBuildingPartData[] parts,
            params DigRepresentativeBuildingAnchorData[] anchors)
        {
            return new DigRepresentativeBuildingProfileData
            {
                stableIds = stableIds,
                kind = kind,
                footprintSize = footprint,
                pivotCell = pivot,
                visualBoundsCenter = visualBoundsCenter,
                visualBoundsSize = visualBoundsSize,
                tint = tint,
                maxRenderers = 16,
                maxTriangles = 512,
                parts = parts,
                anchors = anchors,
            };
        }

        private static DigRepresentativeBuildingPartData Part(
            string name,
            string shape,
            Vector3 position,
            Vector3 scale,
            Vector3? rotation = null,
            string detail = "Marker")
        {
            return new DigRepresentativeBuildingPartData
            {
                name = name,
                shape = shape,
                detail = detail,
                position = position,
                scale = scale,
                rotation = rotation ?? Vector3.zero,
            };
        }

        private static DigRepresentativeBuildingAnchorData Anchor(
            string kind,
            string stableId,
            Vector3 position)
        {
            return new DigRepresentativeBuildingAnchorData
            {
                kind = kind,
                stableId = stableId,
                position = position,
            };
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
