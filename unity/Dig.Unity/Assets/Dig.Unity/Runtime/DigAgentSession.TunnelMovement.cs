using System;
using System.Collections.Generic;
using Dig.Application.Agents;
using Dig.Domain.Agents;
using Dig.Domain.Core;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;

namespace Dig.Unity
{
    internal sealed partial class DigAgentSession
    {
        private readonly HashSet<EntityId> _manualTunnelOrders = new HashSet<EntityId>();
        private TunnelNavigationVolume? _tunnelVolume;
        private MoveAgentThroughTunnelCommandHandler? _tunnelMovement;

        internal TunnelNavigationVolume TunnelVolume => _tunnelVolume
            ?? throw new InvalidOperationException("Tunnel movement is not initialized.");

        internal MoveAgentThroughTunnelReport MoveResidentThroughTunnel(
            string residentId,
            SpatialCellId destination)
        {
            if (string.IsNullOrWhiteSpace(residentId))
            {
                throw new ArgumentException("Resident id is required.", nameof(residentId));
            }

            if (_tunnelMovement == null)
            {
                throw new InvalidOperationException("Tunnel movement is not initialized.");
            }

            EntityId id = EntityId.Parse(residentId);
            MoveAgentThroughTunnelReport report = _tunnelMovement.Handle(
                new MoveAgentThroughTunnelCommand(id, destination, _tick));
            if (report.Result.IsSuccess)
            {
                _manualTunnelOrders.Add(id);
            }

            return report;
        }

        private void InitializeTunnelMovement(
            int width,
            int height,
            InMemoryExecutionJournal journal)
        {
            TunnelNavigationVolume demo = TunnelNavigationVolume.CreateDemo(width, height);
            HashSet<SpatialCellId> open = new HashSet<SpatialCellId>(demo.Cells);
            HashSet<SpatialCellId> vertical = new HashSet<SpatialCellId>(demo.VerticalCells);
            int shaftX = width / 2;
            const int shaftZ = 1;
            for (int y = 0; y < height; y++)
            {
                SpatialCellId shaft = new SpatialCellId(shaftX, y, shaftZ);
                open.Add(shaft);
                vertical.Add(shaft);
            }

            IReadOnlyList<AgentState> agents = _repository.GetAll();
            for (int index = 0; index < agents.Count; index++)
            {
                SpatialCellId start = agents[index].SpatialPosition;
                open.Add(start);
                int direction = start.X <= shaftX ? 1 : -1;
                for (int x = start.X; x != shaftX; x += direction)
                {
                    open.Add(new SpatialCellId(x, start.Y, start.Z));
                }

                open.Add(new SpatialCellId(shaftX, start.Y, start.Z));
                int depthDirection = start.Z <= shaftZ ? 1 : -1;
                for (int z = start.Z; z != shaftZ; z += depthDirection)
                {
                    open.Add(new SpatialCellId(shaftX, start.Y, z));
                }

                open.Add(new SpatialCellId(shaftX, start.Y, shaftZ));
            }

            _tunnelVolume = new TunnelNavigationVolume(
                width,
                height,
                depth: 4,
                open,
                vertical);
            _tunnelMovement = new MoveAgentThroughTunnelCommandHandler(
                _repository,
                _tunnelVolume,
                journal);
        }

        private bool HasManualTunnelOrder(EntityId agentId)
        {
            return _manualTunnelOrders.Contains(agentId);
        }
    }
}
