using Dig.Domain.World;
using Dig.Presentation.Agents;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigAgentVisual
    {
        private const float ToolWorkAnimationPeriodSeconds = 0.72f;
        private CellId? _workTargetCell;
        private bool _climbingWorkPose;
        private bool _toolWorkActive;
        private bool _attackWorkActive;

        internal void SetWorkTarget(
            CellId? target,
            bool climbingWork,
            bool animateToolWork,
            bool animateAttackWork = false)
        {
            bool hadWorkPose = _climbingWorkPose || _toolWorkActive || _attackWorkActive;
            bool willHaveWorkPose = target.HasValue
                && (climbingWork || animateToolWork || animateAttackWork);
            _workTargetCell = target;
            _climbingWorkPose = target.HasValue && climbingWork;
            _attackWorkActive = target.HasValue && animateAttackWork && !climbingWork;
            _toolWorkActive = target.HasValue
                && animateToolWork
                && !animateAttackWork
                && !climbingWork;
            if (hadWorkPose && !willHaveWorkPose && _duration <= 0f)
            {
                ApplyAction(isMoving: false);
            }

            ApplyWorkFacingIfIdle();
            ApplyToolWorkAnimation();
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

        private void ApplyToolWorkAnimation()
        {
            if ((!_toolWorkActive && !_attackWorkActive)
                || _rig == null
                || _duration > 0f)
            {
                return;
            }

            float progress = Mathf.Repeat(
                Time.unscaledTime,
                ToolWorkAnimationPeriodSeconds) / ToolWorkAnimationPeriodSeconds;
            _rig.ApplyAction(new ResidentActionVisualViewModel(
                Model.Id,
                _attackWorkActive
                    ? ResidentActionVisualState.Hit
                    : ResidentActionVisualState.Dig,
                progress,
                isLooping: true,
                version: Model.Version));
        }
    }
}
