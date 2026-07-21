using Dig.Presentation.Agents;
using UnityEngine;

namespace Dig.Unity
{
public sealed partial class DigAgentVisual
{
    private void Update()
    {
        if (_duration <= 0f) return;
        _elapsed = Mathf.Min(_duration, _elapsed + Time.deltaTime);
        double progress = _elapsed / _duration;
        AgentInterpolatedSpatialPosition position = AgentSpatialPositionInterpolator.Interpolate(
            _previousVisualX, _previousY, _previousZ,
            _currentVisualX, _currentY, _currentZ, progress);
        transform.position = ToWorld((float)position.X, (float)position.Y, (float)position.Z);
        if (_elapsed >= _duration)
        {
            _duration = 0f;
            transform.position = ToWorld(_currentVisualX, _currentY, _currentZ);
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
