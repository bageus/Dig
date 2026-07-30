from pathlib import Path


def replace_once(path: str, old: str, new: str) -> None:
    file = Path(path)
    text = file.read_text(encoding="utf-8")
    count = text.count(old)
    if count != 1:
        raise RuntimeError(f"Expected one match in {path}, found {count}: {old[:80]!r}")
    file.write_text(text.replace(old, new), encoding="utf-8")


replace_once(
    "src/Dig.Application/Saving/SaveContracts.cs",
    "public const int CurrentVersion = 10;",
    "public const int CurrentVersion = 11;")
replace_once(
    "src/Dig.Application/Saving/SaveContracts.cs",
    "    [DataMember(Order = 16)] public CombatSaveData Combat { get; set; } = new CombatSaveData();",
    "    [DataMember(Order = 16)] public CombatSaveData Combat { get; set; } = new CombatSaveData();\n"
    "    [DataMember(Order = 17)] public LivingMaterialEcologySaveData LivingMaterials { get; set; } = new LivingMaterialEcologySaveData();")
replace_once(
    "src/Dig.Application/Saving/SaveContracts.cs",
    "        IReadOnlyDictionary<EntityId, AgentRuntimeSnapshot>? agentRuntime = null,\n"
    "        CombatState? combat = null)",
    "        IReadOnlyDictionary<EntityId, AgentRuntimeSnapshot>? agentRuntime = null,\n"
    "        CombatState? combat = null,\n"
    "        LivingMaterialEcologyState? livingMaterials = null)")
replace_once(
    "src/Dig.Application/Saving/SaveContracts.cs",
    "        Combat = combat;\n",
    "        Combat = combat;\n"
    "        LivingMaterials = livingMaterials ?? new LivingMaterialEcologyState(metadata.WorldSeed);\n")
replace_once(
    "src/Dig.Application/Saving/SaveContracts.cs",
    "    public CombatState? Combat { get; }\n",
    "    public CombatState? Combat { get; }\n"
    "    public LivingMaterialEcologyState LivingMaterials { get; }\n")

replace_once(
    "src/Dig.Application/Saving/SaveGameContext.cs",
    "        BarrelState? barrels = null,\n"
    "        CombatState? combat = null)",
    "        BarrelState? barrels = null,\n"
    "        CombatState? combat = null,\n"
    "        LivingMaterialEcologyState? livingMaterials = null)")
replace_once(
    "src/Dig.Application/Saving/SaveGameContext.cs",
    "        Combat = combat;\n",
    "        Combat = combat;\n"
    "        LivingMaterials = livingMaterials ?? new LivingMaterialEcologyState(metadata.WorldSeed);\n")
replace_once(
    "src/Dig.Application/Saving/SaveGameContext.cs",
    "    public CombatState? Combat { get; }\n",
    "    public CombatState? Combat { get; }\n"
    "    public LivingMaterialEcologyState LivingMaterials { get; }\n")

replace_once(
    "src/Dig.Application/Saving/SaveGameBuilder.cs",
    "            Combat = CombatSaveAdapter.Encode(context.Combat),",
    "            Combat = CombatSaveAdapter.Encode(context.Combat),\n"
    "            LivingMaterials = LivingMaterialEcologySaveAdapter.Encode(context.LivingMaterials),")

replace_once(
    "src/Dig.Application/Saving/SaveGameLoader.cs",
    "            Result<RestoredMiningOutputState> miningOutput = RestoreMiningOutput(\n",
    "            Result<LivingMaterialEcologyState> livingMaterials =\n"
    "                LivingMaterialEcologySaveAdapter.Decode(\n"
    "                    document.LivingMaterials,\n"
    "                    inventory.Value,\n"
    "                    document.Metadata.WorldSeed);\n"
    "            if (livingMaterials.IsFailure)\n"
    "            {\n"
    "                return Result<LoadedGameState>.Failure(livingMaterials.Error!);\n"
    "            }\n"
    "            Result<RestoredMiningOutputState> miningOutput = RestoreMiningOutput(\n")
replace_once(
    "src/Dig.Application/Saving/SaveGameLoader.cs",
    "                agentRuntime,\n"
    "                combat));",
    "                agentRuntime,\n"
    "                combat,\n"
    "                livingMaterials.Value));")

replace_once(
    "src/Dig.Infrastructure/Saving/SaveGameCompositionRoot.cs",
    "            new SaveVersionNineCombatSpatialMigration(),",
    "            new SaveVersionNineCombatSpatialMigration(),\n"
    "            new SaveVersionTenLivingMaterialsMigration(),")
