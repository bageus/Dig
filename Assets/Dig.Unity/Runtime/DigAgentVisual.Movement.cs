using Dig.Domain.Navigation;
using Dig.Domain.World;
using Dig.Presentation.Agents;
using UnityEngine;

namespace Dig.Unity
{
public sealed partial class DigAgentVisual
{
    private const float ClimbWallDepthOffset = 0.16f;
    private bool _isClimbing;
    private bool _climbingAscending;
    private TunnelNavigationVolume? _tunnelVolume;
    private TunnelTraversalKind _activeTraversalKind;

    internal void SetTunnelNavigationVolume(TunnelNavigationVolume? volume)
    {
        _tunnelVolume = volume;
    }

    private void PrepareTraversalKind()
    {
        CellId from = new CellId(_previousX, _previousY, _previousZ);
        CellId to = new CellId(_currentX, _currentY, _currentZ);
        _activeTraversalKind = _tunnelVolume?.ClassifyTraversal(from, to)
            ?? TunnelTraversalKind.Invalid;
        if (_activeTraversalKind == TunnelTraversalKind.Invalid
            && _previousX == _currentX
            && _previousZ == _currentZ
            && _previousY != _currentY)
        {
            _activeTraversalKind = TunnelTraversalKind.VerticalClimb;
        }
    }

    private void Update()
    {
        if (_duration <= 0f)
        {
            if (_productionWaitPose)
            {
                FaceTowardMainCamera();
            }

            ApplyToolWorkAnimation();
            return;
        }
        _elapsed = Mathf.Min(_duration, _elapsed + Time.deltaTime);
        double progress = _elapsed / _duration;
        AgentInterpolatedSpatialPosition position = AgentSpatialPositionInterpolator.Interpolate(
            _previousVisualX, _previousVisualY, _previousVisualZ,
            _currentVisualX, _currentVisualY, _currentVisualZ, progress);
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
            _activeTraversalKind = TunnelTraversalKind.Invalid;
            transform.position = ToWorld(
                _currentVisualX, _currentVisualY, _currentVisualZ);
            ApplyAction(isMoving: false);
            ApplyWorkFacingIfIdle();
        }
    }

    private void Face(Vector3 direction)
    {
        _isClimbing = _activeTraversalKind == TunnelTraversalKind.VerticalClimb
            || _activeTraversalKind == TunnelTraversalKind.ShaftGapTraverse;
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
