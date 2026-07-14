using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    public sealed class DigUnityBootstrap : MonoBehaviour
    {
        [SerializeField]
        private bool logStartup = true;

        private void Awake()
        {
            if (!logStartup)
            {
                return;
            }

            Debug.Log(
                "Dig Unity presentation host started. " +
                "Authoritative simulation state remains in the engine-independent core.",
                this);
        }
    }
}
