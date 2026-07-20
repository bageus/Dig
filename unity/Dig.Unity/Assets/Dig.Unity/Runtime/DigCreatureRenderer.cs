using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Presentation.Creatures;
using UnityEngine;

namespace Dig.Unity
{
[DisallowMultipleComponent]
public sealed partial class DigCreatureRenderer : MonoBehaviour
{
    private const int DefaultPopulationCap = 128;
    private const int MaximumPoolSize = 64;

    private readonly Dictionary<string, DigCreatureVisual> _creatures =
        new Dictionary<string, DigCreatureVisual>(StringComparer.Ordinal);
    private readonly Stack<DigCreatureVisual> _pool = new Stack<DigCreatureVisual>();
    private readonly CreatureVisualPresenter _presenter = new CreatureVisualPresenter();
    private Transform? _visualRoot;
    private DigCreatureVisualCatalog? _catalog;
    private Material? _material;
    private string? _selectedCreatureId;

    public int ActiveCount => _creatures.Count;
    public int PooledCount => _pool.Count;
    public string? SelectedCreatureId => _selectedCreatureId;

    public void Render(
        IReadOnlyList<CreatureVisualSnapshot> snapshots,
        Camera? camera,
        float movementDuration,
        int populationCap = DefaultPopulationCap)
    {
        if (snapshots == null) throw new ArgumentNullException(nameof(snapshots));
        EnsureResources();
        CreatureRenderReconciliationPlan plan =
            CreatureRenderReconciliationPlan.Create(
                _creatures.Keys.ToArray(),
                snapshots,
                populationCap);
        Dictionary<string, CreatureVisualSnapshot> models = snapshots.ToDictionary(
            snapshot => snapshot.CreatureId,
            StringComparer.Ordinal);

        for (int index = 0; index < plan.RemoveIds.Count; index++)
            RemoveCreature(plan.RemoveIds[index]);
        for (int index = 0; index < plan.UpdateIds.Count; index++)
            UpdateCreature(models[plan.UpdateIds[index]], camera, movementDuration);
        for (int index = 0; index < plan.CreateIds.Count; index++)
            CreateCreature(models[plan.CreateIds[index]], camera);
    }

    public bool TryGetCreature(RaycastHit hit, out DigCreatureVisual creature)
    {
        creature = hit.collider == null
            ? null!
            : hit.collider.GetComponent<DigCreatureVisual>();
        return creature != null;
    }

    public DigCreatureVisual? SelectById(string? creatureId)
    {
        if (_selectedCreatureId != null
            && _creatures.TryGetValue(
                _selectedCreatureId,
                out DigCreatureVisual? previous))
        {
            previous.SetSelected(false);
        }

        _selectedCreatureId = null;
        if (string.IsNullOrWhiteSpace(creatureId)
            || !_creatures.TryGetValue(creatureId, out DigCreatureVisual? selected))
        {
            return null;
        }

        _selectedCreatureId = creatureId;
        selected.SetSelected(true);
        return selected;
    }

    public void ClearSelection()
    {
        SelectById(null);
    }

    public bool TryResolveAnchor(
        string creatureId,
        DigCreatureAnchorKind kind,
        out Transform anchor)
    {
        if (_creatures.TryGetValue(creatureId, out DigCreatureVisual? creature))
        {
            anchor = creature.ResolveAnchor(kind);
            return true;
        }

        anchor = null!;
        return false;
    }

    private void UpdateCreature(
        CreatureVisualSnapshot snapshot,
        Camera? camera,
        float movementDuration)
    {
        DigCreatureVisual visual = _creatures[snapshot.CreatureId];
        CreatureAppearanceViewModel appearance = _presenter.PresentAppearance(snapshot);
        ConfigureCollider(visual, appearance.Family);
        if (visual.RequiresRigRebuild(appearance))
        {
            DigCreatureVisualResolution resolution = ResolveVisual(appearance);
            DigCreatureRig rig = DigCreatureRigFactory.Create(
                visual.transform,
                resolution,
                _material!,
                appearance);
            visual.ReplaceRig(rig, appearance);
        }

        CreatureActionVisualViewModel action = _presenter.PresentAction(snapshot);
        CreatureLodViewModel lod = ResolveLod(snapshot, camera);
        visual.ApplySnapshot(snapshot, appearance, action, lod, movementDuration);
        visual.SetSelected(string.Equals(
            _selectedCreatureId,
            snapshot.CreatureId,
            StringComparison.Ordinal));
    }

    private void CreateCreature(CreatureVisualSnapshot snapshot, Camera? camera)
    {
        DigCreatureVisual visual = AcquireRoot(snapshot.CreatureId);
        CreatureAppearanceViewModel appearance = _presenter.PresentAppearance(snapshot);
        ConfigureCollider(visual, appearance.Family);
        DigCreatureVisualResolution resolution = ResolveVisual(appearance);
        DigCreatureRig rig = DigCreatureRigFactory.Create(
            visual.transform,
            resolution,
            _material!,
            appearance);
        visual.ReplaceRig(rig, appearance);
        visual.Initialize(
            snapshot,
            _presenter,
            rig,
            appearance,
            _presenter.PresentAction(snapshot),
            ResolveLod(snapshot, camera));
        visual.SetSelected(string.Equals(
            _selectedCreatureId,
            snapshot.CreatureId,
            StringComparison.Ordinal));
        _creatures.Add(snapshot.CreatureId, visual);
    }

    private void RemoveCreature(string creatureId)
    {
        DigCreatureVisual visual = _creatures[creatureId];
        _creatures.Remove(creatureId);
        if (string.Equals(_selectedCreatureId, creatureId, StringComparison.Ordinal))
            _selectedCreatureId = null;
        visual.SetSelected(false);
        visual.gameObject.SetActive(false);
        if (_pool.Count < MaximumPoolSize)
            _pool.Push(visual);
        else
            Destroy(visual.gameObject);
    }
}
}