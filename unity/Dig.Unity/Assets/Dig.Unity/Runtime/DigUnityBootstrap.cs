using System.Collections.Generic;
using Dig.Presentation.Agents;
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
            DigWorldSession worldSession = DigWorldSession.CreateDemo(
                demoWidth,
                demoHeight,
                chunkSize);
            WorldViewModel world = worldSession.LoadView();
            DigAgentSession agentSession = DigAgentSession.CreateDemo(
                world,
                worldSession.Journal);
            IReadOnlyList<AgentViewModel> agents = agentSession.LoadView();
            Camera targetCamera = EnsureCamera();
            DigWorldRenderer worldRenderer = GetOrAdd<DigWorldRenderer>(gameObject);
            DigAgentRenderer agentRenderer = GetOrAdd<DigAgentRenderer>(gameObject);
            DigHudOverlay hud = GetOrAdd<DigHudOverlay>(gameObject);
            DigWorldInteraction interaction = GetOrAdd<DigWorldInteraction>(gameObject);
            DigAgentSimulationDriver simulation =
                GetOrAdd<DigAgentSimulationDriver>(gameObject);
            DigCameraController cameraController =
                GetOrAdd<DigCameraController>(targetCamera.gameObject);

            RenderSettings.ambientLight = new Color(0.58f, 0.60f, 0.66f, 1f);
            worldRenderer.Render(world);
            agentRenderer.Render(agents, movementDuration: 0f);
            hud.SetWorld(world);
            hud.SetAgents(agents, agentSession.Tick);
            cameraController.Initialize(targetCamera, world);
            interaction.Initialize(
                targetCamera,
                worldSession,
                worldRenderer,
                agentRenderer,
                hud);
            simulation.Initialize(agentSession, agentRenderer, hud);

            if (logStartup)
            {
                Debug.Log(
                    "Dig Unity settlement slice started. " +
                    "WorldState and AgentState remain authoritative; visuals are rebuildable views.",
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