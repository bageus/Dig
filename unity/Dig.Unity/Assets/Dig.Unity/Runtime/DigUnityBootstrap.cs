using System;
using System.Collections.Generic;
using Dig.Domain.World;
using Dig.Presentation.Agents;
using Dig.Presentation.Buildings;
using Dig.Presentation.Creatures;
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
            DigGameHudCanvas gameHud = CreateStartupGameHud(hud);
            try
            {
                StartRuntime(hud, gameHud);
            }
            catch (Exception exception)
            {
                DisableRuntimeDrivers();
                string message =
                    $"STARTUP FAILED [{_startupStage}] "
                    + $"{exception.GetType().Name}: {exception.Message}";
                hud.SetStatus(message);
                Debug.LogException(exception, this);
            }
        }

        private void StartRuntime(DigHudOverlay hud, DigGameHudCanvas gameHud)
        {
            _startupStage = "validating demo configuration";
            ClampDemoConfiguration();

            _startupStage = "configuring side-view world root";
            ConfigureSideViewRoot();

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
            agentSession.InitializeHudSchedule(worldSession.Journal);
            IReadOnlyList<AgentViewModel> agents = agentSession.LoadView();

            _startupStage = "creating work systems";
            DigTerrainWorkSession terrainSession = DigTerrainWorkSession.CreateDemo(
                worldSession,
                agents,
                worldSession.Journal);
            terrainSession.InitializeDynamicDesignations(worldSession.Journal);
            terrainSession.InitializeHauling(worldSession.Journal);
            terrainSession.PlanMovement(agents);
            terrainSession.InitializeBuildingDemo(worldSession.Journal);
            terrainSession.InitializeToolAwareJobAssignment(worldSession.Journal);
            IReadOnlyList<JobOverlayViewModel> jobs = terrainSession.LoadJobs();
            IReadOnlyList<WorldItemViewModel> items = terrainSession.LoadAllWorldItems();
            IReadOnlyList<RouteViewModel> routes = terrainSession.LoadRoutes();
            IReadOnlyList<BuildingWorldViewModel> buildings = terrainSession.LoadBuildings();
            DigStorageStatus storage = terrainSession.GetStorageStatus();

            _startupStage = "creating Unity adapters";
            Camera targetCamera = EnsureCamera();
            DigWorldRenderer worldRenderer = GetOrAdd<DigWorldRenderer>(gameObject);
            DigAgentRenderer agentRenderer = GetOrAdd<DigAgentRenderer>(gameObject);
            DigCreatureRenderer creatureRenderer = GetOrAdd<DigCreatureRenderer>(gameObject);
            DigJobRenderer jobRenderer = GetOrAdd<DigJobRenderer>(gameObject);
            DigBuildingRenderer buildingRenderer = GetOrAdd<DigBuildingRenderer>(gameObject);
            DigWorldItemRenderer itemRenderer = GetOrAdd<DigWorldItemRenderer>(gameObject);
            DigBuildingBoxGhostRenderer ghostRenderer =
                GetOrAdd<DigBuildingBoxGhostRenderer>(gameObject);
            DigStockpileRenderer stockpileRenderer =
                GetOrAdd<DigStockpileRenderer>(gameObject);
            DigNavigationRouteRenderer routeRenderer =
                GetOrAdd<DigNavigationRouteRenderer>(gameObject);
            DigTunnelDemoRenderer tunnelRenderer =
                GetOrAdd<DigTunnelDemoRenderer>(gameObject);
            DigCaveRoomPreviewRenderer caveRoomPreviewRenderer =
                GetOrAdd<DigCaveRoomPreviewRenderer>(gameObject);
            DigCaveRoomFloorRenderer caveRoomFloorRenderer =
                GetOrAdd<DigCaveRoomFloorRenderer>(gameObject);
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
            hud.SetToolAssignmentControls(terrainSession, jobRenderer);
            hud.SetBuildingControls(terrainSession, buildingRenderer, jobRenderer);
            hud.SetStatus("Starting renderers...");

            _startupStage = "rendering world and layered tunnels";
            RenderSettings.ambientLight = new Color(0.58f, 0.60f, 0.66f, 1f);
            worldRenderer.SetProtectedCells(worldSession.ProtectedCells);
            worldRenderer.SetTunnelCutaway(agentSession.TunnelVolume);
            worldRenderer.SetTerrainDepthVolume(
                agentSession.TunnelVolume,
                worldSession.SolidMaterialId.ToString(),
                worldSession.SolidHardness,
                Array.Empty<SpatialCellId>());
            worldRenderer.SetTerrainDeposits(worldSession.LoadTerrainDeposits());
            worldRenderer.Render(world);
            tunnelRenderer.Initialize(agentSession.TunnelVolume);
            caveRoomPreviewRenderer.Clear();

            _startupStage = "rendering residents and creatures";
            agentRenderer.Render(agents, movementDuration: 0f);
            creatureRenderer.Render(
                Array.Empty<CreatureVisualSnapshot>(),
                targetCamera,
                movementDuration: 0f);

            _startupStage = "rendering jobs and buildings";
            jobRenderer.Initialize(agentRenderer);
            jobRenderer.Render(jobs);
            buildingRenderer.Render(buildings);

            _startupStage = "rendering inventory and routes";
            itemRenderer.Render(items);
            ghostRenderer.Clear();
            stockpileRenderer.Render(storage);
            routeRenderer.Render(routes);

            _startupStage = "enabling interaction";
            interaction.Initialize(
                targetCamera,
                cameraController,
                worldSession,
                worldRenderer,
                agentRenderer,
                jobRenderer,
                buildingRenderer,
                itemRenderer,
                ghostRenderer,
                terrainSession,
                stockpileRenderer,
                simulation,
                hud);
            interaction.SetTunnelMovement(tunnelRenderer);
            interaction.SetCaveRoomRenderers(
                caveRoomPreviewRenderer,
                caveRoomFloorRenderer);
            simulation.Initialize(
                worldSession,
                worldRenderer,
                agentSession,
                agentRenderer,
                terrainSession,
                jobRenderer,
                buildingRenderer,
                itemRenderer,
                stockpileRenderer,
                routeRenderer,
                hud);

            _startupStage = "binding uGUI game HUD";
            gameHud.Initialize(
                terrainSession,
                agentRenderer,
                jobRenderer,
                buildingRenderer,
                interaction,
                simulation,
                hud,
                targetCamera,
                world);

            interaction.enabled = true;
            simulation.enabled = true;
            hud.SetStatus(
                "Select a dwarf, building, or job. Tunnel planning is the default mode.");
            if (logStartup)
            {
                Debug.Log(
                    $"Dig Unity runtime started with {agents.Count} residents, "
                    + $"{jobs.Count} jobs, {buildings.Count} buildings and a "
                    + $"{world.Width}x{world.Height}x4 rock volume.",
                    this);
            }
        }

        private static DigGameHudCanvas CreateStartupGameHud(DigHudOverlay hud)
        {
            GameObject canvasObject = new GameObject(
                "Dig Game HUD Canvas",
                typeof(RectTransform));
            DigGameHudCanvas gameHud = canvasObject.AddComponent<DigGameHudCanvas>();
            gameHud.InitializeStartup(hud);
            hud.AttachGameHudCanvas(gameHud);
            return gameHud;
        }

        private void ConfigureSideViewRoot()
        {
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);
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
