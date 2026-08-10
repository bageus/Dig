using UnityEngine;

namespace DigDwarfs
{
    public sealed class DwarfAttachmentSockets : MonoBehaviour
    {
        public Transform? RightHandTool;
        public Transform? LeftHandTool;
        public Transform? CarryAnchor;
        public Transform? BackAttachment;
        public Transform? HeadAccessory;

        public void AutoBind()
        {
            RightHandTool = FindNamed(transform, nameof(RightHandTool));
            LeftHandTool = FindNamed(transform, nameof(LeftHandTool));
            CarryAnchor = FindNamed(transform, nameof(CarryAnchor));
            BackAttachment = FindNamed(transform, nameof(BackAttachment));
            HeadAccessory = FindNamed(transform, nameof(HeadAccessory));
        }

        private static Transform? FindNamed(Transform root, string nodeName)
        {
            if (root == null) return null;
            if (root.name == nodeName) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var match = FindNamed(root.GetChild(i), nodeName);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            AutoBind();
        }
#endif
    }
}
