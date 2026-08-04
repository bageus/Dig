using UnityEngine;

namespace DigDwarfs
{
    // Optional demo component for quick scene testing.
    public sealed class DwarfSampleDriver : MonoBehaviour
    {
        [SerializeField] private DwarfAnimatorBridge bridge;
        [SerializeField] private float speed;
        [SerializeField] private bool climbing;
        [SerializeField] private bool carrying;
        [SerializeField] private DwarfActionKind actionKind;

        private void Reset()
        {
            bridge = GetComponent<DwarfAnimatorBridge>();
        }

        private void Update()
        {
            if (bridge == null) return;

            bridge.SetLocomotion(speed, climbing, carrying);
            bridge.SetAction(actionKind);

            if (Input.GetKeyDown(KeyCode.Alpha1)) bridge.TriggerAttack();
            if (Input.GetKeyDown(KeyCode.Alpha2)) bridge.TriggerHit();
            if (Input.GetKeyDown(KeyCode.Alpha3)) bridge.TriggerDeath();
        }
    }
}
