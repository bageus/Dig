using System.Collections.Generic;
using Dig.Domain.Core;
using Dig.Domain.Society;
using Dig.Domain.World;
using Dig.Presentation.Agents;
using Dig.Presentation.Runtime;
using UnityEngine;

namespace Dig.Unity
{
    public abstract partial class DigAgentSimulationDriverBase : MonoBehaviour
    {
        protected const int MaximumTicksPerFrame = 8;
        private static readonly DomainError NotInitialized = new DomainError(
            "unity.agent_simulation.not_initialized",
            "The resident simulation driver is not initialized.");

        [SerializeField]
        private float tickIntervalSeconds = 0.8f;

        private protected DigWorldSession? WorldSession;
        protected DigWorldRenderer? WorldRenderer;
        private protected DigAgentSession? AgentSession;
        protected DigAgentRenderer? AgentRenderer;
        private protected DigTerrainWorkSession? TerrainSession;
        protected DigJobRenderer? JobRenderer;
        protected DigBuildingRenderer? BuildingRenderer;
        protected DigWorldItemRenderer? ItemRenderer;
        protected DigStockpileRenderer? StockpileRenderer;
        protected DigNavigationRouteRenderer? RouteRenderer;
        protected DigHudOverlay? Hud;
        private SimulationPlaybackState? _playback;

        internal bool IsPaused => Playback.IsPaused;

        internal string PlaybackLabel => Playback.Label;

        internal long CurrentTick => AgentSession?.Tick ?? 0;

        internal ResidentSex ResolveResidentSex(string residentId)
        {
            return AgentSession?.ResolveResidentSex(residentId) ?? ResidentSex.Male;
        }

        protected float TickIntervalSeconds => tickIntervalSeconds;

        protected SimulationPlaybackState Playback =>
            _playback ??= new SimulationPlaybackState();

        internal void Initialize(
            DigWorldSession worldSession,
            DigWorldRenderer worldRenderer,
            DigAgentSession agentSession,
            DigAgentRenderer agentRenderer,
            DigTerrainWorkSession terrainSession,
            DigJobRenderer jobRenderer,
            DigBuildingRenderer buildingRenderer,
            DigWorldItemRenderer itemRenderer,
            DigStockpileRenderer stockpileRenderer,
            DigNavigationRouteRenderer routeRenderer,
            DigHudOverlay hud)
        {
            WorldSession = worldSession;
            WorldRenderer = worldRenderer;
            AgentSession = agentSession;
            AgentRenderer = agentRenderer;
            TerrainSession = terrainSession;
            JobRenderer = jobRenderer;
            BuildingRenderer = buildingRenderer;
            ItemRenderer = itemRenderer;
            StockpileRenderer = stockpileRenderer;
            RouteRenderer = routeRenderer;
            Hud = hud;
            AgentSession.SetMovementTargetFilter(
                TerrainSession.ApplyResidentMovementCadence);
            RefreshEquipmentVisuals();
        }

        internal void TogglePause()
        {
            Playback.TogglePause();
        }

        internal void StepOnce()
        {
            Playback.StepOnce();
        }

        internal void SetSpeed(SimulationPlaybackSpeed speed)
        {
            Playback.SetSpeed(speed);
        }

        internal Result MoveResident(string residentId, CellId destination)
        {
            if (AgentSession == null || AgentRenderer == null || Hud == null)
            {
                return Result.Failure(NotInitialized);
            }

            Result result = AgentSession.MoveResident(residentId, destination);
            if (result.IsFailure)
            {
                return result;
            }

            IReadOnlyList<AgentViewModel> agents = AgentSession.LoadView();
            AgentRenderer.Render(agents, movementDuration: 0.25f);
            RefreshEquipmentVisuals();
            Hud.SetAgents(agents, AgentSession.Tick);
            Hud.SetAgentSelection(
                AgentRenderer.SelectedModel,
                AgentRenderer.SelectedCount);
            return Result.Success();
        }

        protected bool IsInitialized()
        {
            return WorldSession != null
                && WorldRenderer != null
                && AgentSession != null
                && AgentRenderer != null
                && TerrainSession != null
                && JobRenderer != null
                && BuildingRenderer != null
                && ItemRenderer != null
                && StockpileRenderer != null
                && RouteRenderer != null
                && Hud != null;
        }

        protected virtual void OnValidate()
        {
            tickIntervalSeconds = Mathf.Max(0.1f, tickIntervalSeconds);
        }
    }
}