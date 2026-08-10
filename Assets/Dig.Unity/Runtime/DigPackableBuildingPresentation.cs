using System;
using System.Collections.Generic;
using Dig.Application.Buildings;
using Dig.Domain.Content;
using Dig.Presentation.Buildings;

namespace Dig.Unity
{

internal sealed partial class DigTerrainWorkSession
{
    private PackableBuildingExecutionPresenter? _packableBuildingExecutionPresenter;

    public IReadOnlyList<PackableBuildingExecutionViewModel> LoadPackableBuildingExecutions(
        long tick)
    {
        if (_packableBuildingExecutions == null)
        {
            return Array.Empty<PackableBuildingExecutionViewModel>();
        }

        EnsurePackableBuildingExecutionPresenter();
        return _packableBuildingExecutionPresenter!.Load(
            _packableBuildingExecutions,
            tick);
    }

    internal PackableBuildingExecutionRegistry CapturePackableBuildingExecutions()
    {
        return _packableBuildingExecutions
            ?? throw new InvalidOperationException(
                "Packable building executions are not initialized.");
    }

    internal void RestorePackableBuildingExecutions(
        PackableBuildingExecutionRegistry executions)
    {
        _packableBuildingExecutions = executions
            ?? throw new ArgumentNullException(nameof(executions));
        _campfireIterationProgression = null;
        InitializePackableBuildingIterationProgression();
        EnsurePackableBuildingExecutionPresenter();
    }

    private void EnsurePackableBuildingExecutionPresenter()
    {
        _packableBuildingExecutionPresenter ??=
            new PackableBuildingExecutionPresenter(
                CampfireBuildingBoxContent.Catalog,
                new PackableBuildingVisualCatalog(new[]
                {
                    new PackableBuildingVisualProfile(
                        CampfireBuildingBoxContent.CampfireBuildingId,
                        activeBuildingVisualId: "visual.campfire.active",
                        worldBoxVisualId: "visual.campfire.box.world",
                        inventoryBoxVisualId: "visual.campfire.box.inventory",
                        plannedSiteVisualId: "visual.campfire.site.planned",
                        partialUnpackVisualId: "visual.campfire.unpack.partial",
                        partialPackVisualId: "visual.campfire.pack.partial",
                        iterationEffectId: "effect.campfire.iteration"),
                }));
    }
}

}