using System;
using System.Collections.Generic;
using Dig.Domain.World;
using UnityEngine;

namespace Dig.Unity
{
public sealed partial class DigAgentVisual
{
    internal void PlayRoute(IReadOnlyList<SpatialCellId> cells, float stepDuration)
    {
        if (cells == null) throw new ArgumentNullException(nameof(cells));
        if (cells.Count == 0) return;
        _route = cells;
        _routeIndex = 0;
        _routeElapsed = 0f;
        _routeStepDuration = Mathf.Max(0.03f, stepDuration);
        _duration = 0f;
        transform.position = ToWorld(cells[0]);
        ApplyAction(isMoving: cells.Count > 1);
        if (cells.Count > 1) Face(ToWorld(cells[1]) - transform.position);
    }

    private void Update()
    {
        if (_route != null)
        {
            UpdateRoute();
            return;
        }
        if (_duration <= 0f) return;
        _elapsed = Mathf.Min(_duration, _elapsed + Time.deltaTime);
        double progress = _elapsed / _duration;
        AgentInterpolatedSpatialPosition position = AgentSpatialPositionInterpolator.Interpolate(
            _previousX, _previousY, _previousZ,
            _currentX, _currentY, _currentZ, progress);
        transform.position = ToWorld((float)position.X, (float)position.Y, (float)position.Z);
        if (_elapsed >= _duration)
        {
            _duration = 0f;
            ApplyAction(isMoving: false);
        }
    }

    private void UpdateRoute()
    {
        if (_route == null || _routeIndex + 1 >= _route.Count)
        {
            _route = null;
            ApplyAction(isMoving: false);
            return;
        }
        SpatialCellId from = _route[_routeIndex];
        SpatialCellId to = _route[_routeIndex + 1];
        _routeElapsed = Mathf.Min(_routeStepDuration, _routeElapsed + Time.deltaTime);
        float progress = _routeElapsed / _routeStepDuration;
        transform.position = Vector3.Lerp(ToWorld(from), ToWorld(to), progress);
        if (_routeElapsed < _routeStepDuration) return;
        _routeIndex++;
        _routeElapsed = 0f;
        if (_routeIndex + 1 < _route.Count)
        {
            Face(ToWorld(_route[_routeIndex + 1]) - transform.position);
        }
        else
        {
            transform.position = ToWorld(_route[_route.Count - 1]);
            _route = null;
            ApplyAction(isMoving: false);
        }
    }

    private void Face(Vector3 direction)
    {
        if (Mathf.Abs(direction.y) > 0.001f
            && Mathf.Abs(direction.x) < 0.001f
            && Mathf.Abs(direction.z) < 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(Vector3.back, Vector3.up);
            return;
        }
        Vector3 planar = new Vector3(direction.x, 0f, direction.z);
        if (planar.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(planar.normalized, Vector3.up);
    }
}
}
