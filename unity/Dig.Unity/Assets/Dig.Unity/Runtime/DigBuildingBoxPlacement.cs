using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Application.Buildings;
using Dig.Application.Inventory;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
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

            _buildingBoxDefinition = definition ?? throw new ArgumentNullException(nameof(definition));
            BuildingPlacementValidator validator = new BuildingPlacementValidator();
            _buildingBoxPlacementPresenter = new BuildingBoxPlacementPresenter(validator);
            _buildingBoxPlacementHandler = new ConfirmBuildingBoxPlacementHandler(
                catalog ?? throw new ArgumentNullException(nameof(catalog)),
                _worldSession.Repository,
                _buildingsRepository,
                _buildingInventoryRepository,
                _jobRepository,
                validator,
                journal ?? throw new ArgumentNullException(nameof(journal)));
            _buildingInventoryPresenter = new InventoryWorldPresenter(
                new GetInventorySnapshotQueryHandler(_buildingInventoryRepository),
                WorldItemInteractionKind.BuildingBox);
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
            BuildingBoxPolicy policy = _buildingBoxDefinition!.BoxPolicy!;
            if (stack == null
                || stack.ItemId != policy.BoxItemId
                || stack.Quantity != 1
                || stack.AvailableQuantity != 1)
            {
                return Result<BuildingBoxPlacementModeState>.Failure(
                    PlacementSourceUnavailable);
            }

            return Result<BuildingBoxPlacementModeState>.Success(
                new BuildingBoxPlacementModeState(stack.StackId, _buildingBoxDefinition.Id));
        }

        internal BuildingBoxGhostViewModel PreviewBuildingBoxPlacement(
            BuildingBoxPlacementModeState mode,
            CellId origin)
        {
            EnsureBuildingBoxPlacementInitialized();
            InventoryState inventory = _buildingInventoryRepository!.Get();
            ItemStackSnapshot? stack = inventory.GetStack(mode.SourceStackId);
            ItemDefinition? item = stack == null ? null : inventory.Catalog.Get(stack.ItemId);
            return _buildingBoxPlacementPresenter!.Preview(
                stack,
                item,
                _buildingBoxDefinition!,
                origin,
                mode.Orientation,
                _worldSession.LoadSnapshot(),
                _buildingsRepository!.Get().GetOccupiedCells(),
                GetBuildingPlacementReachableCells());
        }

        internal Result ConfirmBuildingBoxPlacement(
            BuildingBoxGhostViewModel preview,
            long tick)
        {
            EnsureBuildingBoxPlacementInitialized();
            Result<BuildingBoxPlacementConfirmationDraft> drafted =
                _buildingBoxPlacementPresenter!.CreateConfirmationDraft(preview);
            if (drafted.IsFailure)
            {
                return Result.Failure(drafted.Error!);
            }

            BuildingBoxPlacementConfirmationDraft draft = drafted.Value;
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
                    GetBuildingPlacementReachableCells(),
                    priority: 625,
                    tick));
        }

        internal string BuildingBoxName => _buildingBoxDefinition?.Name ?? "BuildingBox";

        private IReadOnlyCollection<CellId> GetBuildingPlacementReachableCells()
        {
            return _worldSession.LoadView().Chunks
                .SelectMany(chunk => chunk.Cells)
                .Where(cell => !cell.IsSolid)
                .Select(cell => new CellId(cell.X, cell.Y))
                .Distinct()
                .OrderBy(cell => cell)
                .ToArray();
        }

        private void EnsureBuildingBoxPlacementInitialized()
        {
            if (_buildingBoxDefinition == null
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
