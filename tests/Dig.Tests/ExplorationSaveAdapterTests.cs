using Dig.Application.Saving;
using Dig.Domain.Core;
using Dig.Domain.Exploration;
using Dig.Domain.Inventory;
using Dig.Domain.World;
using Dig.Infrastructure.Saving;
using Xunit;

namespace Dig.Tests
{
public sealed class ExplorationSaveAdapterTests
{
    [Fact]
    public void Json_round_trip_preserves_exploration_history_and_item_memory()
    {
        EntityId stack = EntityId.New();
        ExplorationState state = ExplorationState.Restore(new ExplorationSaveSnapshot(
            1,
            new[] { new CellId(3, 4, 1) },
            new[] { new LastKnownWorldItemMarker(
                stack, new ItemId("ore.gold"), new CellId(3, 4, 1), 42) }));
        SaveGameDocument document = new SaveGameDocument
        {
            FormatVersion = SaveFormat.CurrentVersion,
            Exploration = ExplorationSaveAdapter.Encode(state),
        };
        DataContractJsonSaveCodec codec = new DataContractJsonSaveCodec();
        SaveGameDocument decoded = codec.Deserialize(codec.Serialize(document));
        ExplorationState restored = ExplorationSaveAdapter.Decode(
            decoded.Exploration, new WorldSize(8, 8));
        Assert.Equal(CellVisibility.ExploredNotVisible,
            restored.GetVisibility(new CellId(3, 4, 1)));
        LastKnownWorldItemMarker marker = Assert.Single(restored.Markers);
        Assert.Equal(stack, marker.StackId);
        Assert.Equal(42, marker.ObservedTick);
    }

    [Fact]
    public void Version_seventeen_migrates_to_empty_exploration_section()
    {
        SaveGameDocument document = new SaveGameDocument { FormatVersion = 17 };
        SaveMigrationReport report = Dig.Infrastructure.Saving.SaveGameCompositionRoot
            .CreateMigrationPipeline().Apply(document).Value;
        Assert.Equal(SaveFormat.CurrentVersion, document.FormatVersion);
        Assert.NotNull(document.Exploration);
        Assert.Contains("save.v17_to_v18.exploration", report.AppliedSteps);
    }
}
}
