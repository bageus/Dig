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
            AgentViewModel? model = Model;
            bool eating = model != null && model.IsAlive && IsEating;
            if (_mealVisualActive != eating)
            {
                _mealVisualActive = eating;
                RefreshHandEquipment();
            }

            if (!eating || model == null || _rig == null || _duration > 0f)
            {
                return;
            }

            float authoritativeOffset = (float)model.ActionProgress
                * EatingBitePeriodSeconds;
            float progress = Mathf.Repeat(
                Time.unscaledTime + authoritativeOffset,
                EatingBitePeriodSeconds) / EatingBitePeriodSeconds;
            _rig.ApplyAction(new ResidentActionVisualViewModel(
                model.Id,
                ResidentActionVisualState.Eat,
                progress,
                isLooping: true,
                version: model.Version));
        }
    }
}
