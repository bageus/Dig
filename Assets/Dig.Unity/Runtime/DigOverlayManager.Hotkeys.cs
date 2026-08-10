using Dig.Presentation.Overlays;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigOverlayManager
    {
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F2))
            {
                CycleVisibilityProfile();
            }

            if (Input.GetKeyDown(KeyCode.F3)
                || Input.GetKeyDown(KeyCode.Alpha3)
                || Input.GetKeyDown(KeyCode.Keypad3))
            {
                ToggleLayer(OverlayLayerKind.Jobs);
            }

            if (Input.GetKeyDown(KeyCode.F4)
                || Input.GetKeyDown(KeyCode.Alpha4)
                || Input.GetKeyDown(KeyCode.Keypad4))
            {
                ToggleLayer(OverlayLayerKind.Routes);
            }
        }

        private void CycleVisibilityProfile()
        {
            OverlayVisibilityProfile next = visibilityProfile switch
            {
                OverlayVisibilityProfile.Release => OverlayVisibilityProfile.Debug,
                OverlayVisibilityProfile.Debug => OverlayVisibilityProfile.All,
                _ => OverlayVisibilityProfile.Release,
            };
            _visibilityOverrides.Clear();
            SetVisibilityProfile(next);
        }
    }
}
