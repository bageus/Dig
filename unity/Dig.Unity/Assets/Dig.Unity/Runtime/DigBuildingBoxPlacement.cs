using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Buildings;
using Dig.Application.Inventory;
using Dig.Domain.Buildings;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Dig.Presentation.Agents;
using Dig.Presentation.Buildings;
using Dig.Presentation.Inventory;
using Dig.Presentation.World;

namespace Dig.Unity
{
    internal sealed partial class DigTerrainWorkSession
    {
        private static readonly DomainError PlacementNotInitialized = new DomainError(
            "unity.building_box.placement_not_initialized",
            "BuildingBox placement is not initialized.");
        private static readonly DomainError PlacementSourceUnavailable = new DomainError(
            "unity.building_box.source_unavailable",
            "The selected BuildingBox is missing or unavailable.");

        private BuildingCatalog? _buildingBoxCatalog;
        private BuildingDefinition? _buildingBoxDefinition;
        private BuildingBoxPlacementPresenter? _buildingBoxPlacementPresenter;
        private ConfirmBuildingBoxPlacementHandler? _buildingBoxPlacementHandler;
        private long _nextPlacementSequence;

        private void InitializeBuildingBoxWorldInput(
            BuildingCatalog catalog,
            BuildingDefinition definition,
            InMemoryExecutionJournal journal)
        {
            if (_buildingsRepository == null || _buildingInventoryRepository == null)
            {
                throw new InvalidOperationException("Building demo state must be initialized first.");
            }

            _buildingBoxCatalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _buildingBoxDefinition = definition ?? throw new ArgumentNullException(nameof(definition));
            BuildingPlacementValidator validator = new BuildingPlacementValidator();
            _buildingBoxPlacementPresenter = new BuildingBoxPlacementPresenter(validator);
            _buildingBoxPlacementHandler = new ConfirmBuildingBoxPlacementHandler(
                _buildingBoxCatalog,
                _worldSession.Repository,
                _buildingsRepository,
                _buildingInventoryRepository,
                _jobRepository,
                validator,
                journal ?? throw new ArgumentNullException(nameof(journal)));
            _buildingInventoryPresenter = new InventoryWorldPresenter(
                new GetInventorySnapshotQueryHandler(_buildingInventoryRepository),
                _buildingInventoryRepository.Get().Catalog);
            InitializeResidentInventoryPresentation();
            InitializeBuildingBoxPickupExecution(journal);
            InitializeBuildingBoxAssemblyExecution(journal);
        }

        internal Result<BuildingBoxPlacementModeState> BeginBuildingBoxPlacement(
            string stackId)
        {
            EnsureBuildingBoxPlacementInitialized();
            if (string.IsNullOrWhiteSpace(stackId))
            {
                throw new ArgumentException("Stack id is required.", nameof(stackId));
            }

            ItemStackSnapshot? stack = _buildingInventoryRepository!.Get().GetStack(
                EntityId.Parse(stackId));
            BuildingDefinition? definition = stack == null
                ? null
                : ResolveBuildingBoxDefinition(stack.ItemId);
            BuildingBoxPolicy? policy = definition?.BoxPolicy;
            if (stack == null
                || policy == null
                || stack.ItemId != policy.BoxItemId
                || stack.Quantity != 1
                || stack.AvailableQuantity != 1)
            {
                return Result<BuildingBoxPlacementModeState>.Failure(
                    PlacementSourceUnavailable);
            }

            return Result<BuildingBoxPlacementModeState>.Success(
                new BuildingBoxPlacementModeState(stack.StackId, definition!.Id));
        }

        internal BuildingBoxGhostViewModel PreviewBuildingBoxPlacement(
            BuildingBoxPlacementModeState mode,
            CellId origin)
        {
            return PreviewBuildingBoxPlacement(
                mode,
                origin,
                Array.Empty<AgentViewModel>());
        }

        internal BuildingBoxGhostViewModel PreviewBuildingBoxPlacement(
            BuildingBoxPlacementModeState mode,
            CellId origin,
            IReadOnlyList<AgentViewModel> agents)
        {
            EnsureBuildingBoxPlacementInitialized();
            if (agents == null)
            {
                throw new ArgumentNullException(nameof(agents));
            }

            InventoryState inventory = _buildingInventoryRepository!.Get();
            ItemStackSnapshot? stack = inventory.GetStack(mode.SourceStackId);
            ItemDefinition? item = stack == null ? null : inventory.Catalog.Get(stack.ItemId);
            BuildingDefinition definition = _buildingBoxCatalog!.Get(mode.DefinitionId);
            return _buildingBoxPlacementPresenter!.Preview(
                stack,
                item,
                definition,
                origin,
                mode.Orientation,
                _worldSession.LoadSnapshot(),
                _buildingsRepository!.Get().GetOccupiedCells(),
                GetBuildingPlacementReachableCells(mode.SourceStackId, agents),
                BuildingPlacementBlockedCells);
        }

        internal Result ConfirmBuildingBoxPlacement(
            BuildingBoxGhostViewModel preview,
            long tick)
        {
            return ConfirmBuildingBoxPlacement(
                preview,
                tick,
                Array.Empty<AgentViewModel>());
        }

        internal Result ConfirmBuildingBoxPlacement(
            BuildingBoxGhostViewModel preview,
            long tick,
            IReadOnlyList<AgentViewModel> agents)
        {
            EnsureBuildingBoxPlacementInitialized();
            if (agents == null)
            {
                throw new ArgumentNullException(nameof(agents));
            }

            Result<BuildingBoxPlacementConfirmationDraft> drafted =
                _buildingBoxPlacementPresenter!.CreateConfirmationDraft(preview);
            if (drafted.IsFailure)
            {
                return Result.Failure(drafted.Error!);
            }

            BuildingBoxPlacementConfirmationDraft draft = drafted.Value;
            IReadOnlyCollection<CellId> reachable =
                GetBuildingPlacementReachableCells(draft.SourceStackId, agents);
            if (draft.PlacementKind == BuildingBoxPlacementKind.RelocateBox)
            {
                return CreateBuildingBoxRelocation(
                    draft.SourceStackId,
                    draft.Origin,
                    reachable,
                    tick);
            }

            long sequence = checked(_nextPlacementSequence + 1);
            _nextPlacementSequence = sequence;
            return _buildingBoxPlacementHandler!.Handle(
                new ConfirmBuildingBoxPlacementCommand(
                    DemoId('7', sequence),
                    DemoId('8', sequence),
                    draft.SourceStackId,
                    draft.DefinitionId,
                    draft.Origin,
                    draft.Orientation,
                    reachable,
                    priority: 625,
                    tick: tick,
                    ecologyBlockedCells: BuildingPlacementBlockedCells));
        }

        internal string BuildingBoxName => _buildingBoxDefinition?.Name ?? "BuildingBox";

        private BuildingDefinition? ResolveBuildingBoxDefinition(ItemId boxItemId)
        {
            return _buildingBoxCatalog!.FindByBoxItemId(boxItemId);
        }

        private void EnsureBuildingBoxPlacementInitialized()
        {
            if (_buildingBoxCatalog == null
                || _buildingBoxDefinition == null
                || _buildingBoxPlacementPresenter == null
                || _buildingBoxPlacementHandler == null
                || _buildingInventoryRepository == null
                || _buildingsRepository == null)
            {
                throw new InvalidOperationException(PlacementNotInitialized.ToString());
            }
        }
    }
}
