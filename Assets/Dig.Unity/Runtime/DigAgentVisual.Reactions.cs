using System.Collections;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigAgentVisual
    {
        private Coroutine? _inventoryFullReaction;

        internal void PlayInventoryFullReaction()
        {
            if (_inventoryFullReaction != null)
            {
                StopCoroutine(_inventoryFullReaction);
            }

            _inventoryFullReaction = StartCoroutine(InventoryFullReaction());
        }

        private IEnumerator InventoryFullReaction()
        {
            Transform target = _rig == null ? transform : _rig.transform;
            Quaternion originalRotation = target.localRotation;
            Vector3 originalScale = target.localScale;
            const float duration = 0.75f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float normalized = Mathf.Clamp01(elapsed / duration);
                float envelope = Mathf.Sin(normalized * Mathf.PI);
                float shake = Mathf.Sin(normalized * Mathf.PI * 6f) * 9f * envelope;
                float shrug = 1f - (0.08f * envelope);
                target.localRotation = originalRotation * Quaternion.Euler(0f, shake, 0f);
                target.localScale = new Vector3(
                    originalScale.x * (1f + (0.06f * envelope)),
                    originalScale.y * shrug,
                    originalScale.z);
                yield return null;
            }

            target.localRotation = originalRotation;
            target.localScale = originalScale;
            _inventoryFullReaction = null;
        }
    }
}
