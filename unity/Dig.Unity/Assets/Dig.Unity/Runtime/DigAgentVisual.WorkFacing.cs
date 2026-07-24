using Dig.Domain.World;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigAgentVisual
    {
        private CellId? _workTargetCell;

        internal void SetWorkTarget(CellId? target)
        {
            _workTargetCell = target;
            ApplyWorkFacingIfIdle();
        }

        private void ApplyWorkFacingIfIdle()
        {
            if (!_workTargetCell.HasValue || _duration > 0f)
            {
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
