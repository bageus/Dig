using Dig.Domain.World;
using UnityEngine;

namespace Dig.Unity
{

public sealed partial class DigAgentVisual
{
    private bool _productionWaitPose;

    internal void SetProductionWaitPose(bool active, float offsetX)
    {
        if (active)
        {
            _productionWaitPose = true;
            SetFreeformDestination(
                new CellId(_currentX, _currentY, _currentZ),
                offsetX);
            if (_duration <= 0f)
            {
                FaceTowardMainCamera();
            }

            return;
        }

        if (!_productionWaitPose)
        {
            return;
        }

        _productionWaitPose = false;
        _freeformDestinationCell = null;
        _freeformDestinationOffsetX = 0f;
        _freeformDestinationOffsetZ = 0f;
        _currentVisualX = _currentX;
        _previousVisualX = _currentVisualX;
        if (_duration <= 0f)
        {
            transform.position = ToWorld(
                _currentVisualX, _currentVisualY, _currentVisualZ);
        }
    }

    private void FaceTowardMainCamera()
    {
        Camera? mainCamera = Camera.main;
        if (mainCamera == null)
        {
            transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            return;
        }

        Vector3 toward = mainCamera.transform.position - transform.position;
        toward.y = 0f;
        if (toward.sqrMagnitude <= 0.001f)
        {
            toward = Vector3.forward;
        }

        transform.rotation = Quaternion.LookRotation(toward.normalized, Vector3.up);
    }
}

}
