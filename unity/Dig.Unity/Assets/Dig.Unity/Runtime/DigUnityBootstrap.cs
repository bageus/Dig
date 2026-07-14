using Dig.Presentation.World;
using UnityEngine;

namespace Dig.Unity
{
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class DigUnityBootstrap : MonoBehaviour
    {
        [SerializeField]
        private int demoWidth = 20;

        [SerializeField]
        private int demoHeight = 14;

        [SerializeField]
        private int chunkSize = 5;

        [SerializeField]
        private bool logStartup = true;

        private void Awake()
        {
            DigWorldSession session = DigWorldSession.CreateDemo(
                demoWidth,
                demoHeight,
                chunkSize);
            WorldViewModel world = session.LoadView();
            Camera targetCamera = EnsureCamera();
            DigWorldRenderer renderer = GetOrAdd<DigWorldRenderer>(gameObject);
            DigHudOverlay hud = GetOrAdd<DigHudOverlay>(gameObject);
            DigWorldInteraction interaction = GetOrAdd<DigWorldInteraction>(gameObject);
            DigCameraController cameraController =
                GetOrAdd<DigCameraController>(targetCamera.gameObject);

            RenderSettings.ambientLight = new Color(0.58f, 0.60f, 0.66f, 1f);
            renderer.Render(world);
            hud.SetWorld(world);
            cameraController.Initialize(targetCamera, world);
            interaction.Initialize(targetCamera, session, renderer, hud);

            if (logStartup)
            {
                Debug.Log(
                    "Dig Unity world vertical slice started. " +
                    "WorldState remains authoritative; scene objects are rebuildable views.",
                    this);
            }
        }

        private static Camera EnsureCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.10f, 0.13f, 0.17f, 1f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 200f;
            return camera;
        }

        private static T GetOrAdd<T>(GameObject target)
            where T : Component
        {
            T component = target.GetComponent<T>();
            return component == null ? target.AddComponent<T>() : component;
        }

        private void OnValidate()
        {
            demoWidth = Mathf.Max(8, demoWidth);
            demoHeight = Mathf.Max(8, demoHeight);
            chunkSize = Mathf.Clamp(chunkSize, 1, Mathf.Min(demoWidth, demoHeight));
        }
    }
}
