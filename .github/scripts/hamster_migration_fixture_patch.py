from pathlib import Path


def replace_once(path: str, old: str, new: str) -> None:
    file = Path(path)
    text = file.read_text(encoding="utf-8")
    count = text.count(old)
    if count != 1:
        raise RuntimeError(f"Expected one match in {path}, found {count}: {old!r}")
    file.write_text(text.replace(old, new), encoding="utf-8")


path = "tests/Dig.Tests/SaveMigrationAndCorruptionTests.cs"
replace_once(
    path,
    '            "save.v9_to_v10.combat_spatial",\n        }, first.Value.AppliedSteps);',
    '            "save.v9_to_v10.combat_spatial",\n            "save.v10_to_v11.living_materials",\n        }, first.Value.AppliedSteps);')
replace_once(
    path,
    '                "save.v9_to_v10.combat_spatial",\n            },\n            first.Value.AppliedSteps);',
    '                "save.v9_to_v10.combat_spatial",\n                "save.v10_to_v11.living_materials",\n            },\n            first.Value.AppliedSteps);')
replace_once(
    path,
    '            new SaveVersionNineCombatSpatialMigration(),\n        });',
    '            new SaveVersionNineCombatSpatialMigration(),\n            new SaveVersionTenLivingMaterialsMigration(),\n        });')

for path in (
    "tests/Dig.Tests/BarrelSaveRoundTripTests.cs",
    "tests/Dig.Tests/MushroomSaveRoundTripTests.cs",
):
    replace_once(
        path,
        '            new SaveVersionNineCombatSpatialMigration(),\n        });',
        '            new SaveVersionNineCombatSpatialMigration(),\n            new SaveVersionTenLivingMaterialsMigration(),\n        });')
