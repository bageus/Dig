using UnityEngine;

namespace DigDwarfs
{
    [DisallowMultipleComponent]
    public sealed class DwarfAnimatorBridge : MonoBehaviour
    {
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsClimbingHash = Animator.StringToHash("IsClimbing");
        private static readonly int IsCarryingHash = Animator.StringToHash("IsCarrying");
        private static readonly int ActionKindHash = Animator.StringToHash("ActionKind");
        private static readonly int AttackTriggerHash = Animator.StringToHash("AttackTrigger");
        private static readonly int HitTriggerHash = Animator.StringToHash("HitTrigger");
        private static readonly int DieTriggerHash = Animator.StringToHash("DieTrigger");

        [SerializeField] private Animator animator = null!;
        [SerializeField] private DwarfAttachmentSockets sockets = null!;

        public Animator Animator => animator;
        public DwarfAttachmentSockets Sockets => sockets;

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (sockets == null)
            {
                sockets = GetComponent<DwarfAttachmentSockets>();
            }

            if (animator != null)
            {
                animator.applyRootMotion = false;
            }
        }

        public void SetLocomotion(float speedNormalized, bool isClimbing, bool isCarrying)
        {
            if (animator == null) return;
            animator.SetFloat(SpeedHash, speedNormalized);
            animator.SetBool(IsClimbingHash, isClimbing);
            animator.SetBool(IsCarryingHash, isCarrying);
        }

        public void SetAction(DwarfActionKind actionKind)
        {
            if (animator == null) return;
            animator.SetInteger(ActionKindHash, (int)actionKind);
        }

        public void ClearAction()
        {
            if (animator == null) return;
            animator.SetInteger(ActionKindHash, 0);
        }

        public void TriggerAttack()
        {
            if (animator == null) return;
            animator.SetTrigger(AttackTriggerHash);
        }

        public void TriggerHit()
        {
            if (animator == null) return;
            animator.SetTrigger(HitTriggerHash);
        }

        public void TriggerDeath()
        {
            if (animator == null) return;
            animator.SetTrigger(DieTriggerHash);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (sockets == null)
            {
                sockets = GetComponent<DwarfAttachmentSockets>();
            }
        }
#endif
    }
}