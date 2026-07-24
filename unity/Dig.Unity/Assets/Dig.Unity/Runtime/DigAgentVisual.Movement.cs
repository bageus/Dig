using Dig.Presentation.Agents;
using UnityEngine;

namespace Dig.Unity
{
public sealed partial class DigAgentVisual
{
    private const float ClimbWallDepthOffset = 0.16f;
    private bool _isClimbing;
    private bool _climbingAscending;

    private void Update()
    {
        if (_duration <= 0f) return;
        _elapsed = Mathf.Min(_duration, _elapsed + Time.deltaTime);
        double progress = _elapsed / _duration;
        AgentInterpolatedSpatialPosition position = AgentSpatialPositionInterpolator.Interpolate(
            _previousVisualX, _previousY, _previousZ,
            _currentVisualX, _currentY, _currentZ, progress);
        Vector3 world = ToWorld((float)position.X, (float)position.Y, (float)position.Z);
        if (_isClimbing)
        {
            float wallBlend = Mathf.Sin(Mathf.PI * (float)progress);
            world += Vector3.back * (ClimbWallDepthOffset * wallBlend);
            FaceAwayFromMainCamera();
            _rig?.ApplyClimbPose((float)progress, _climbingAscending);
        }

        transform.position = world;
        if (_elapsed >= _duration)
        {
            _duration = 0f;
            _isClimbing = false;
            transform.position = ToWorld(_currentVisualX, _currentY, _currentZ);
            ApplyAction(isMoving: false);
            ApplyWorkFacingIfIdle();
        }
    }

    private void Face(Vector3 direction)
    {
        _isClimbing = _previousX == _currentX
            && _previousZ == _currentZ
            && _previousY != _currentY;
        if (_isClimbing)
        {
            _climbingAscending = _currentY < _previousY;
            FaceAwayFromMainCamera();
            _rig?.ApplyClimbPose(0f, _climbingAscending);
            return;
        }

        Vector3 planar = new Vector3(direction.x, 0f, direction.z);
        if (planar.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(planar.normalized, Vector3.up);
    }

    private void FaceAwayFromMainCamera()
    {
        Camera? mainCamera = Camera.main;
        if (mainCamera == null)
        {
            transform.rotation = Quaternion.LookRotation(Vector3.back, Vector3.up);
            return;
        }

        Vector3 away = transform.position - mainCamera.transform.position;
        away.y = 0f;
        if (away.sqrMagnitude <= 0.001f)
        {
            away = Vector3.back;
        }

        transform.rotation = Quaternion.LookRotation(away.normalized, Vector3.up);
    }
}
}