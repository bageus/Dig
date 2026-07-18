using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Agents;
using Dig.Application.World;
using Dig.Domain.Core;
using Dig.Domain.Navigation;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;

namespace Dig.Unity
{
    internal sealed partial class DigAgentSession
    {
        private readonly HashSet<EntityId> _manualTunnelOrders = new HashSet<EntityId>();
        private readonly HashSet<SpatialCellId> _tunnelDepthExcavations =
            new HashSet<SpatialCellId>();
        private readonly TunnelDepthExcavationPolicy _tunnelDepthExcavation =
            new TunnelDepthExcavationPolicy();
        private TunnelNavigationVolume? _tunnelVolume;
        private MoveAgentThroughTunnelCommandHandler? _tunnelMovement;
        private MoveAgentsThroughTunnelCommandHandler? _groupTunnelMovement;
        private InMemoryExecutionJournal? _tunnelJournal;

        internal TunnelNavigationVolume TunnelVolume => _tunnelVolume
            ?? throw new InvalidOperationException("Tunnel movement is not initialized.");

        internal IReadOnlyCollection<SpatialCellId> TunnelDepthExcavations =>
            _tunnelDepthExcavations.OrderBy(cell => cell).ToArray();

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

        internal MoveAgentsThroughTunnelReport MoveResidentsThroughTunnel(
            IReadOnlyCollection<string> residentIds,
            SpatialCellId destination)
        {
            if (residentIds == null)
            {
                throw new ArgumentNullException(nameof(residentIds));
            }

            if (_groupTunnelMovement == null)
            {
                throw new InvalidOperationException("Tunnel movement is not initialized.");
            }

            List<EntityId> ids = new List<EntityId>(residentIds.Count);
            foreach (string residentId in residentIds)
            {
                if (string.IsNullOrWhiteSpace(residentId))
                {
                    throw new ArgumentException(
                        "Resident ids cannot contain an empty value.",
                        nameof(residentIds));
                }

                ids.Add(EntityId.Parse(residentId));
            }

            MoveAgentsThroughTunnelReport report = _groupTunnelMovement.Handle(
                new MoveAgentsThroughTunnelCommand(ids, destination, _tick));
            if (report.Result.IsSuccess)
            {
                for (int index = 0; index < ids.Count; index++)
                {
                    _manualTunnelOrders.Add(ids[index]);
                }
            }

            return report;
        }

        internal TunnelDepthExcavationPlanResult ExcavateTunnelDepth(
            SpatialCellId source)
        {
            TunnelDepthExcavationPlanResult result =
                _tunnelDepthExcavation.Plan(TunnelVolume, source);
            if (!result.Succeeded)
            {
                return result;
            }

            SpatialCellId target = result.Plan!.Target;
            _tunnelDepthExcavations.Add(target);
            ExpandTunnelVolume(new[] { target });
            return result;
        }

        internal bool ReleaseManualTunnelOrder(string residentId)
        {
            if (string.IsNullOrWhiteSpace(residentId))
            {
                throw new ArgumentException("Resident id is required.", nameof(residentId));
            }

            return _manualTunnelOrders.Remove(EntityId.Parse(residentId));
        }

        internal void ExpandTunnelVolume(
            IReadOnlyCollection<SpatialCellId> additionalOpenCells)
        {
            if (_tunnelVolume == null || _tunnelJournal == null)
            {
                throw new InvalidOperationException("Tunnel movement is not initialized.");
            }

            _tunnelVolume = _tunnelVolume.WithAdditionalOpenCells(additionalOpenCells);
            CreateTunnelMovementHandlers();
        }

        private void InitializeTunnelMovement(
            TunnelNavigationVolume volume,
            InMemoryExecutionJournal journal)
        {
            _tunnelVolume = volume ?? throw new ArgumentNullException(nameof(volume));
            _tunnelJournal = journal ?? throw new ArgumentNullException(nameof(journal));
            CreateTunnelMovementHandlers();
        }

        private void CreateTunnelMovementHandlers()
        {
            _tunnelMovement = new MoveAgentThroughTunnelCommandHandler(
                _repository,
                _tunnelVolume!,
                _tunnelJournal!);
            _groupTunnelMovement = new MoveAgentsThroughTunnelCommandHandler(
                _repository,
                _tunnelVolume!,
                _tunnelJournal!);
        }

        private bool HasManualTunnelOrder(EntityId agentId)
        {
            return _manualTunnelOrders.Contains(agentId);
        }
    }
}
