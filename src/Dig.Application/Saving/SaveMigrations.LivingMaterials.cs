using System;

namespace Dig.Application.Saving
{

public sealed class SaveVersionTenLivingMaterialsMigration : ISaveMigration
{
    public string Id => "save.v10_to_v11.living_materials";
    public int FromVersion => 10;
    public int ToVersion => 11;

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
