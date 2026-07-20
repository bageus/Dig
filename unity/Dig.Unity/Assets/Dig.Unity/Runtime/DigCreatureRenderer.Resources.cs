using Dig.Presentation.Creatures;
using Dig.Presentation.Rendering;
using UnityEngine;

namespace Dig.Unity
{
public sealed partial class DigCreatureRenderer
{
    private bool _catalogLoadAttempted;
    private DigRenderMaterialLibrary? _materialLibrary;

    private void EnsureResources()
    {
        if (_visualRoot == null)
        {
            _visualRoot = new GameObject("Creature Visuals").transform;
            _visualRoot.SetParent(transform, worldPositionStays: false);
        }

        if (!_catalogLoadAttempted)
        {
            _catalog = Resources.Load<DigCreatureVisualCatalog>(
                "VisualCatalogs/Creatures");
            _catalogLoadAttempted = true;
        }

        if (_material != null) return;
        if (_materialLibrary == null)
            _materialLibrary = GetComponent<DigRenderMaterialLibrary>();
        if (_materialLibrary == null)
            _materialLibrary = gameObject.AddComponent<DigRenderMaterialLibrary>();
        _material = _materialLibrary.Resolve(RenderMaterialSemantic.Creature,
            RenderSurfaceKind.Lit, new Color(0.48f, 0.42f, 0.28f, 1f));
    }

    private DigCreatureVisualResolution ResolveVisual(
        CreatureAppearanceViewModel appearance)
    {
        if (_catalog != null)
        {
            return _catalog.ResolveCreature(
                appearance.SpeciesId,
                appearance.Family,
                appearance.RigId);
        }

        DigVisualAsset fallback = DigVisualAsset.CreateRuntimeFallback(
            appearance.SpeciesId,
            new Color(0.48f, 0.42f, 0.28f, 1f));
        return new DigCreatureVisualResolution(
            fallback,
            appearance.RigId,
            appearance.Family,
            Vector3.one,
            maximumRenderers: 12,
            hasProfile: false);
    }

    private DigCreatureVisual AcquireRoot(string creatureId)
    {
        DigCreatureVisual visual;
        if (_pool.Count > 0)
        {
            visual = _pool.Pop();
            visual.gameObject.SetActive(true);
        }
        else
        {
            GameObject root = new GameObject("Creature " + creatureId);
            root.transform.SetParent(_visualRoot, worldPositionStays: false);
            root.layer = 0;
            root.AddComponent<SphereCollider>();
            visual = root.AddComponent<DigCreatureVisual>();
        }

        visual.gameObject.name = "Creature " + creatureId;
        visual.transform.SetParent(_visualRoot, worldPositionStays: false);
        return visual;
    }

    private void ConfigureCollider(
        DigCreatureVisual visual,
        CreatureVisualFamily family)
    {
        SphereCollider collider = visual.GetComponent<SphereCollider>();
        switch (family)
        {
            case CreatureVisualFamily.LargeDemon:
                collider.center = new Vector3(0f, 0.82f, 0f);
                collider.radius = 0.78f;
                break;
            case CreatureVisualFamily.Plant:
                collider.center = new Vector3(0f, 0.62f, 0f);
                collider.radius = 0.52f;
                break;
            case CreatureVisualFamily.Biped:
                collider.center = new Vector3(0f, 0.66f, 0f);
                collider.radius = 0.44f;
                break;
            default:
                collider.center = new Vector3(0f, 0.48f, 0f);
                collider.radius = 0.46f;
                break;
        }
    }

    private CreatureLodViewModel ResolveLod(
        CreatureVisualSnapshot snapshot,
        Camera? camera)
    {
        if (camera == null)
            return _presenter.PresentLod(0d, isVisible: true);
        Vector3 position = DigTunnelProjection.ResidentWorldPosition(
            snapshot.CellX,
            snapshot.CellY,
            snapshot.CellZ);
        Vector3 viewport = camera.WorldToViewportPoint(position);
        bool visible = viewport.z > 0f
            && viewport.x >= -0.1f && viewport.x <= 1.1f
            && viewport.y >= -0.1f && viewport.y <= 1.1f;
        double distance = Vector3.Distance(camera.transform.position, position);
        return _presenter.PresentLod(distance, visible);
    }
}
}
