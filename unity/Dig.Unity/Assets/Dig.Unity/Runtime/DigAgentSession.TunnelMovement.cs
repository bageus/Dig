using System;
using System.Collections.Generic;
using Dig.Application.Agents;
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
            TunnelNavigationVolume volume,
            InMemoryExecutionJournal journal)
        {
            _tunnelVolume = volume ?? throw new ArgumentNullException(nameof(volume));
            _tunnelMovement = new MoveAgentThroughTunnelCommandHandler(
                _repository,
                _tunnelVolume,
                journal ?? throw new ArgumentNullException(nameof(journal)));
        }

        private bool HasManualTunnelOrder(EntityId agentId)
        {
            return _manualTunnelOrders.Contains(agentId);
        }
    }
}
