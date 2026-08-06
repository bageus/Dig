using System;

namespace Dig.Application.Saving
{

public sealed partial class SaveGameLoader
{
    private static void ValidateMetadata(SaveMetadataData metadata)
    {
        if (metadata is null
            || string.IsNullOrWhiteSpace(metadata.SlotId)
            || string.IsNullOrWhiteSpace(metadata.DisplayName)
            || string.IsNullOrWhiteSpace(metadata.SavedAtUtc)
            || metadata.SimulationTick < 0
            || metadata.GeneratorVersion <= 0)
        {
            throw new InvalidOperationException("Save metadata is invalid.");
        }
    }

    private static SaveMetadataData CopyMetadata(SaveMetadataData metadata)
    {
        return new SaveMetadataData
        {
            SlotId = metadata.SlotId,
            DisplayName = metadata.DisplayName,
            SavedAtUtc = metadata.SavedAtUtc,
            SimulationTick = metadata.SimulationTick,
            WorldSeed = metadata.WorldSeed,
            GeneratorVersion = metadata.GeneratorVersion,
        };
    }
}

}
