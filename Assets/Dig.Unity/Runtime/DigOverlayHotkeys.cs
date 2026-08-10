using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    public sealed class DigOverlayHotkeys : MonoBehaviour
    {
        private const string JobsRootName = "Job Diagnostic Overlay [F3]";
        private const string RoutesRootName = "Navigation Routes [F4]";
        private Transform? _jobsRoot;
        private Transform? _routesRoot;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha3)
                || Input.GetKeyDown(KeyCode.Keypad3))
            {
                Toggle(ref _jobsRoot, JobsRootName);
            }

            if (Input.GetKeyDown(KeyCode.Alpha4)
                || Input.GetKeyDown(KeyCode.Keypad4))
            {
                Toggle(ref _routesRoot, RoutesRootName);
            }
        }

        private void Toggle(ref Transform? cachedRoot, string rootName)
        {
            if (cachedRoot == null)
            {
                cachedRoot = transform.Find(rootName);
            }

            if (cachedRoot != null)
            {
                cachedRoot.gameObject.SetActive(!cachedRoot.gameObject.activeSelf);
            }
        }
    }
}
