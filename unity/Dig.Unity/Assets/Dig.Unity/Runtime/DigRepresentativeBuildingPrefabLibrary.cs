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

        private static DigRepresentativeBuildingPackData CreateBuiltInPack()
        {
            return new DigRepresentativeBuildingPackData
            {
                profiles = new[]
                {
                    Profile(
                        new[] { "kitchen.campfire", "building.campfire" },
                        "Campfire",
                        Vector2Int.one,
                        Vector2.zero,
                        new Color(0.92f, 0.42f, 0.12f, 1f),
                        new[]
                        {
                            Part("Hearth", "Box", new Vector3(0f, 0.08f, 0f), new Vector3(0.82f, 0.16f, 0.82f)),
                            Part("Flame", "Octahedron", new Vector3(0f, 0.48f, 0f), new Vector3(0.34f, 0.68f, 0.34f)),
                        },
                        Anchor("Worker", "worker.primary", new Vector3(0f, 0f, -0.62f))),
                    Profile(
                        new[] { "building.furnace", "building.forge", "demo.workshop.box" },
                        "Furnace",
                        Vector2Int.one,
                        Vector2.zero,
                        new Color(0.64f, 0.30f, 0.16f, 1f),
                        new[]
                        {
                            Part("Body", "Box", new Vector3(0f, 0.42f, 0f), new Vector3(0.82f, 0.84f, 0.76f)),
                            Part("Chimney", "Box", new Vector3(0.22f, 1.06f, 0.12f), new Vector3(0.28f, 0.72f, 0.28f)),
                        },
                        Anchor("Worker", "worker.primary", new Vector3(0f, 0f, -0.72f))),
                    Profile(
                        new[] { "building.arsenal", "building.storage" },
                        "Storage",
                        new Vector2Int(3, 2),
                        new Vector2(1f, 0.5f),
                        new Color(0.40f, 0.48f, 0.56f, 1f),
                        new[]
                        {
                            Part("Foundation", "Box", new Vector3(1f, 0.12f, 0.5f), new Vector3(2.82f, 0.24f, 1.82f)),
                            Part("Rack", "Box", new Vector3(1f, 0.82f, 0.52f), new Vector3(1.9f, 1.40f, 1.52f)),
                        },
                        Anchor("Storage", "storage.primary", new Vector3(1f, 0.82f, 0.50f))),
                },
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
            Color tint,
            DigRepresentativeBuildingPartData[] parts,
            DigRepresentativeBuildingAnchorData anchor)
        {
            return new DigRepresentativeBuildingProfileData
            {
                stableIds = stableIds,
                kind = kind,
                footprintSize = footprint,
                pivotCell = pivot,
                tint = tint,
                maxRenderers = 8,
                maxTriangles = 256,
                parts = parts,
                anchors = new[] { anchor },
            };
        }

        private static DigRepresentativeBuildingPartData Part(
            string name,
            string shape,
            Vector3 position,
            Vector3 scale)
        {
            return new DigRepresentativeBuildingPartData
            {
                name = name,
                shape = shape,
                detail = "Marker",
                position = position,
                scale = scale,
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
