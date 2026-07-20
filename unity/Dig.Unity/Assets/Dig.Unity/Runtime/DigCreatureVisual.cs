using System;
using Dig.Presentation.Creatures;
using UnityEngine;

namespace Dig.Unity
{
[DisallowMultipleComponent]
public sealed class DigCreatureVisual : MonoBehaviour
{
    private CreatureVisualPresenter? _presenter;
    private DigCreatureRig? _rig;
    private CreatureAppearanceViewModel? _appearance;
    private Vector3 _previousPosition;
    private Vector3 _targetPosition;
    private float _elapsed;
    private float _duration;
    private bool _selected;

    public CreatureVisualSnapshot Model { get; private set; } = null!;

    internal void Initialize(
        CreatureVisualSnapshot snapshot,
        CreatureVisualPresenter presenter,
        DigCreatureRig rig,
        CreatureAppearanceViewModel appearance,
        CreatureActionVisualViewModel action,
        CreatureLodViewModel lod)
    {
        Model = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
        _rig = rig ?? throw new ArgumentNullException(nameof(rig));
        _appearance = appearance ?? throw new ArgumentNullException(nameof(appearance));
        _previousPosition = ToWorld(snapshot);
        _targetPosition = _previousPosition;
        transform.SetPositionAndRotation(_targetPosition, Quaternion.identity);
        _rig.ApplyAppearance(appearance);
        _rig.ApplyAction(action);
        _rig.ApplyLod(lod);
        _rig.SetSelected(_selected);
    }

    internal bool RequiresRigRebuild(CreatureAppearanceViewModel appearance)
    {
        if (appearance == null) throw new ArgumentNullException(nameof(appearance));
        return _appearance == null
            || !string.Equals(_appearance.RigId, appearance.RigId, StringComparison.Ordinal)
            || _appearance.Family != appearance.Family;
    }

    internal void ReplaceRig(
        DigCreatureRig rig,
        CreatureAppearanceViewModel appearance)
    {
        if (rig == null) throw new ArgumentNullException(nameof(rig));
        if (appearance == null) throw new ArgumentNullException(nameof(appearance));
        if (_rig != null && _rig != rig)
            Destroy(_rig.gameObject);
        _rig = rig;
        _appearance = appearance;
        _rig.ApplyAppearance(appearance);
        _rig.SetSelected(_selected);
    }

    internal void ApplySnapshot(
        CreatureVisualSnapshot snapshot,
        CreatureAppearanceViewModel appearance,
        CreatureActionVisualViewModel action,
        CreatureLodViewModel lod,
        float movementDuration)
    {
        if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
        if (!string.Equals(snapshot.CreatureId, Model.CreatureId, StringComparison.Ordinal))
            throw new ArgumentException("Creature snapshot id cannot change.", nameof(snapshot));
        Model = snapshot;
        _appearance = appearance ?? throw new ArgumentNullException(nameof(appearance));
        Vector3 next = ToWorld(snapshot);
        if ((_targetPosition - next).sqrMagnitude > 0.0001f)
        {
            _previousPosition = transform.position;
            _targetPosition = next;
            _elapsed = 0f;
            _duration = Mathf.Max(0.01f, movementDuration);
            Face(_targetPosition - transform.position);
        }
        _rig!.ApplyAppearance(appearance);
        _rig.ApplyAction(action);
        _rig.ApplyLod(lod);
    }

    internal void SetSelected(bool selected)
    {
        _selected = selected;
        _rig?.SetSelected(selected);
    }

    internal Transform ResolveAnchor(DigCreatureAnchorKind kind)
    {
        return _rig == null ? transform : _rig.ResolveAnchor(kind);
    }

    private void Update()
    {
        if (_duration <= 0f) return;
        _elapsed = Mathf.Min(_duration, _elapsed + Time.deltaTime);
        float progress = _elapsed / _duration;
        transform.position = Vector3.Lerp(_previousPosition, _targetPosition, progress);
        if (_elapsed >= _duration) _duration = 0f;
    }

    private void Face(Vector3 direction)
    {
        Vector3 planar = new Vector3(direction.x, 0f, direction.z);
        if (planar.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(planar.normalized, Vector3.up);
    }

    private static Vector3 ToWorld(CreatureVisualSnapshot snapshot)
    {
        return DigTunnelProjection.ResidentWorldPosition(
            snapshot.CellX,
            snapshot.CellY,
            snapshot.CellZ);
    }
}
}