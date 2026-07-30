using System;

namespace Dig.Application.Saving
{

public sealed class SaveVersionElevenLivingMaterialsMigration : ISaveMigration
{
    public string Id => "save.v11_to_v12.living_materials";
    public int FromVersion => 11;
    public int ToVersion => 12;

    public void Apply(SaveGameDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (document.FormatVersion != FromVersion)
        {
            throw new InvalidOperationException(
                "Migration received the wrong source version.");
        }

        document.LivingMaterials ??= new LivingMaterialEcologySaveData
        {
            WorldSeed = document.Metadata?.WorldSeed ?? 0,
        };
        document.FormatVersion = ToVersion;
    }
}

}
