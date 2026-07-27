using Dig.Domain.World;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigAgentVisual
    {
        private CellId? _workTargetCell;
        private bool _climbingWorkPose;

        internal void SetWorkTarget(CellId? target, bool climbingWork)
        {
            bool restoreAction = _climbingWorkPose && !climbingWork;
            _workTargetCell = target;
            _climbingWorkPose = target.HasValue && climbingWork;
            if (restoreAction && _duration <= 0f)
            {
                ApplyAction(isMoving: false);
            }

            ApplyWorkFacingIfIdle();
        }

        private void ApplyWorkFacingIfIdle()
        {
            if (!_workTargetCell.HasValue || _duration > 0f)
            {
                return;
            }

            if (_climbingWorkPose)
            {
                FaceAwayFromMainCamera();
                bool ascending = _workTargetCell.Value.Y < Model.CellY;
                _rig?.ApplyClimbPose(0.25f, ascending);
                return;
            }

            Vector3 direction = DigTunnelProjection.CellWorldPosition(
                _workTargetCell.Value) - transform.position;
            Vector3 planar = new Vector3(direction.x, 0f, direction.z);
            if (planar.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(
                    planar.normalized,
                    Vector3.up);
                return;
            }

            FaceAwayFromMainCamera();
        }
    }
}
