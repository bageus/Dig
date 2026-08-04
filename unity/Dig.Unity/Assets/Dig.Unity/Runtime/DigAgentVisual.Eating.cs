using Dig.Presentation.Agents;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigAgentVisual
    {
        private const float EatingBitePeriodSeconds = 0.82f;
        private bool _mealVisualActive;

        private void LateUpdate()
        {
            bool eating = Model != null && Model.IsAlive && IsEating;
            if (_mealVisualActive != eating)
            {
                _mealVisualActive = eating;
                RefreshHandEquipment();
            }

            if (!eating || _rig == null || _duration > 0f)
            {
                return;
            }

            float authoritativeOffset = (float)Model.ActionProgress
                * EatingBitePeriodSeconds;
            float progress = Mathf.Repeat(
                Time.unscaledTime + authoritativeOffset,
                EatingBitePeriodSeconds) / EatingBitePeriodSeconds;
            _rig.ApplyAction(new ResidentActionVisualViewModel(
                Model.Id,
                ResidentActionVisualState.Eat,
                progress,
                isLooping: true,
                version: Model.Version));
        }
    }
}
