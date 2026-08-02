using System;
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
        protected DigCreatureRenderer? CreatureRenderer;
        protected DigMushroomRenderer? MushroomRenderer;
        protected DigBarrelRenderer? BarrelRenderer;
        private protected DigTerrainWorkSession? TerrainSession;
        protected DigJobRenderer? JobRenderer;
        protected DigBuildingRenderer? BuildingRenderer;
        protected DigBuildingInternalStockRenderer? BuildingInternalStockRenderer;
        protected DigWorldItemRenderer? ItemRenderer;
        protected DigStockpileRenderer? StockpileRenderer;
        protected DigNavigationRouteRenderer? RouteRenderer;
        protected DigWorldOverlayRenderer? WorldOverlayRenderer;
        private protected DigPresentationEffectRuntime? EffectRuntime;
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
            DigCreatureRenderer creatureRenderer,
            DigMushroomRenderer mushroomRenderer,
            DigBarrelRenderer barrelRenderer,
            DigJobRenderer jobRenderer,
            DigBuildingRenderer buildingRenderer,
            DigBuildingInternalStockRenderer buildingInternalStockRenderer,
            DigWorldItemRenderer itemRenderer,
            DigStockpileRenderer stockpileRenderer,
            DigNavigationRouteRenderer routeRenderer,
            DigWorldOverlayRenderer worldOverlayRenderer,
            DigHudOverlay hud)
        {
            WorldSession = worldSession;
            WorldRenderer = worldRenderer;
            AgentSession = agentSession;
            AgentRenderer = agentRenderer;
            CreatureRenderer = creatureRenderer;
            MushroomRenderer = mushroomRenderer;
            BarrelRenderer = barrelRenderer;
            TerrainSession = terrainSession;
            JobRenderer = jobRenderer;
            BuildingRenderer = buildingRenderer;
            BuildingInternalStockRenderer = buildingInternalStockRenderer;
            ItemRenderer = itemRenderer;
            StockpileRenderer = stockpileRenderer;
            RouteRenderer = routeRenderer;
            WorldOverlayRenderer = worldOverlayRenderer;
            EffectRuntime = GetComponent<DigPresentationEffectRuntime>();
            Hud = hud;
            AgentSession.BindCombatInventory(TerrainSession.InventoryRepository);
            AgentSession.SetMovementModeResolver(
                TerrainSession.ResolveResidentMovementMode);
            TerrainSession.BindManualMovementSource(
                AgentSession.HasManualTunnelMovement);
            TerrainSession.BindDirectCommandCombatDisengage(
                AgentSession.DisengageResidentForDirectOrder);
            try
            {
                RefreshEquipmentVisuals();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
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
            AgentRenderer.RenderWithMovementModes(
                agents,
                movementDuration: 0.25f,
                AgentSession.LoadMovementModes());
            AgentRenderer.RenderCombatHealthBars(
                AgentSession.LoadResidentCombatHealthBars(),
                Camera.main);
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
                && CreatureRenderer != null
                && MushroomRenderer != null
                && BarrelRenderer != null
                && JobRenderer != null
                && BuildingRenderer != null
                && BuildingInternalStockRenderer != null
                && ItemRenderer != null
                && StockpileRenderer != null
                && RouteRenderer != null
                && WorldOverlayRenderer != null
                && EffectRuntime != null
                && Hud != null;
        }

        protected virtual void OnValidate()
        {
            tickIntervalSeconds = Mathf.Max(0.1f, tickIntervalSeconds);
        }
    }
}
