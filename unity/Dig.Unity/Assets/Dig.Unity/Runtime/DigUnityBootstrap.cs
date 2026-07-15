using System;
using System.Collections.Generic;
using Dig.Presentation.Agents;
using Dig.Presentation.Inventory;
using Dig.Presentation.Jobs;
using Dig.Presentation.Navigation;
using Dig.Presentation.World;
using UnityEngine;

namespace Dig.Unity
{
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class DigUnityBootstrap : MonoBehaviour
    {
        private const int MinimumDemoDimension = 8;
        private const int MaximumDemoDimension = 64;

        [SerializeField]
        private int demoWidth = 20;

        [SerializeField]
        private int demoHeight = 14;

        [SerializeField]
        private int chunkSize = 5;

        [SerializeField]
        private bool logStartup = true;

        private string _startupStage = "not started";

        private void Awake()
        {
            DigHudOverlay hud = GetOrAdd<DigHudOverlay>(gameObject);
            hud.SetStatus("Starting runtime...");
            try
            {
                StartRuntime(hud);
            }
            catch (Exception exception)
            {
                DisableRuntimeDrivers();
                string message =
                    $"STARTUP FAILED [{_startupStage}] " +
                    $"{exception.GetType().Name}: {exception.Message}";
                hud.SetStatus(message);
                Debug.LogException(exception, this);
            }
        }

        private void StartRuntime(DigHudOverlay hud)
        {
            _startupStage = "validating demo configuration";
            ClampDemoConfiguration();

            _startupStage = "creating world";
            DigWorldSession worldSession = DigWorldSession.CreateDemo(
                demoWidth,
                demoHeight,
                chunkSize);
            WorldViewModel world = worldSession.LoadView();

            _startupStage = "creating residents";
            DigAgentSession agentSession = DigAgentSession.CreateDemo(
                world,
                worldSession.Journal);
            IReadOnlyList<AgentViewModel> agents = agentSession.LoadView();

            _startupStage = "creating work systems";
            DigTerrainWorkSession terrainSession = DigTerrainWorkSession.CreateDemo(
                worldSession,
                agents,
                worldSession.Journal);
            terrainSession.InitializeDynamicDesignations(worldSession.Journal);
            terrainSession.InitializeHauling(worldSession.Journal);
            terrainSession.PlanMovement(agents);
            IReadOnlyList<JobOverlayViewModel> jobs = terrainSession.LoadJobs();
            IReadOnlyList<WorldItemViewModel> items = terrainSession.LoadItems();
            IReadOnlyList<RouteViewModel> routes = terrainSession.LoadRoutes();
            DigStorageStatus storage = terrainSession.GetStorageStatus();

            _startupStage = "creating Unity adapters";
            Camera targetCamera = EnsureCamera();
            DigWorldRenderer worldRenderer = GetOrAdd<DigWorldRenderer>(gameObject);
            DigAgentRenderer agentRenderer = GetOrAdd<DigAgentRenderer>(gameObject);
            DigJobRenderer jobRenderer = GetOrAdd<DigJobRenderer>(gameObject);
            DigWorldItemRenderer itemRenderer = GetOrAdd<DigWorldItemRenderer>(gameObject);
            DigStockpileRenderer stockpileRenderer =
                GetOrAdd<DigStockpileRenderer>(gameObject);
            DigNavigationRouteRenderer routeRenderer =
                GetOrAdd<DigNavigationRouteRenderer>(gameObject);
            GetOrAdd<DigOverlayHotkeys>(gameObject);
            DigWorldInteraction interaction = GetOrAdd<DigWorldInteraction>(gameObject);
            DigAgentSimulationDriver simulation =
                GetOrAdd<DigAgentSimulationDriver>(gameObject);
            DigCameraController cameraController =
                GetOrAdd<DigCameraController>(targetCamera.gameObject);
            interaction.enabled = false;
            simulation.enabled = false;

            _startupStage = "framing camera and HUD";
            cameraController.Initialize(targetCamera, world);
            hud.SetWorld(world);
            hud.SetAgents(agents, agentSession.Tick);
            hud.SetJobs(jobs);
            hud.SetStorageStatus(storage);
            hud.SetSimulationControls(simulation);
            hud.SetStatus("Starting renderers...");

            _startupStage = "rendering world";
            RenderSettings.ambientLight = new Color(0.58f, 0.60f, 0.66f, 1f);
            worldRenderer.Render(world);

            _startupStage = "rendering residents";
            agentRenderer.Render(agents, movementDuration: 0f);

            _startupStage = "rendering jobs";
            jobRenderer.Initialize(agentRenderer);
            jobRenderer.Render(jobs);

            _startupStage = "rendering inventory and routes";
            itemRenderer.Render(items);
            stockpileRenderer.Render(storage);
            routeRenderer.Render(routes);

            _startupStage = "enabling interaction";
            interaction.Initialize(
                targetCamera,
                worldSession,
                worldRenderer,
                agentRenderer,
                jobRenderer,
                terrainSession,
                stockpileRenderer,
                simulation,
                hud);
            simulation.Initialize(
                worldSession,
                worldRenderer,
                agentSession,
                agentRenderer,
                terrainSession,
                jobRenderer,
                itemRenderer,
                stockpileRenderer,
                routeRenderer,
                hud);
            interaction.enabled = true;
            simulation.enabled = true;
            hud.SetStatus("Running. Click the Game view to use keyboard and mouse controls.");

            if (logStartup)
            {
                Debug.Log(
                    $"Dig Unity runtime started with {agents.Count} residents, " +
                    $"{jobs.Count} jobs and a {world.Width}x{world.Height} world.",
                    this);
            }
        }

        private void DisableRuntimeDrivers()
        {
            DigWorldInteraction interaction = GetComponent<DigWorldInteraction>();
            if (interaction != null)
            {
                interaction.enabled = false;
            }

            DigAgentSimulationDriver simulation = GetComponent<DigAgentSimulationDriver>();
            if (simulation != null)
            {
                simulation.enabled = false;
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

        private void ClampDemoConfiguration()
        {
            demoWidth = Mathf.Clamp(
                demoWidth,
                MinimumDemoDimension,
                MaximumDemoDimension);
            demoHeight = Mathf.Clamp(
                demoHeight,
                MinimumDemoDimension,
                MaximumDemoDimension);
            chunkSize = Mathf.Clamp(
                chunkSize,
                1,
                Mathf.Min(demoWidth, demoHeight));
        }

        private void OnValidate()
        {
            ClampDemoConfiguration();
        }
    }
}
