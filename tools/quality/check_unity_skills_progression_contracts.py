#!/usr/bin/env python3
from pathlib import Path
import sys


ROOT = Path(__file__).resolve().parents[2]


def require(path: str, tokens: tuple[str, ...], errors: list[str]) -> None:
    source = ROOT / path
    if not source.is_file():
        errors.append(f"missing {path}")
        return
    text = source.read_text(encoding="utf-8-sig")
    for token in tokens:
        if token not in text:
            errors.append(f"{path} is missing {token!r}")


def main() -> int:
    errors: list[str] = []
    require(
        "src/Dig.Domain/Agents/AgentSkillCatalog.cs",
        ("skill.stonework", "skill.one_handed_combat", "UniversityCapacityUnits"),
        errors,
    )
    require(
        "src/Dig.Domain/Agents/AgentSkillSet.Grants.cs",
        ("ProportionalSkillAllocator.Allocate", "_appliedSources", "overflow"),
        errors,
    )
    require(
        "src/Dig.Domain/Content/DefaultSkillProgressionContent.cs",
        (
            "MushroomHarvest",
            "Cooking",
            "Metallurgy",
            "Alchemy",
            "Service",
            "SkillProgressionContentCatalog.ValidateAndCreate",
        ),
        errors,
    )
    require(
        "src/Dig.Application/Agents/AgentSkillGrantService.cs",
        (
            "Validate(SkillGrantBundle",
            "ApplyConfirmed(\n        SkillGrantBundle bundle)",
            "new SkillProgressionResultConfirmed(bundle)",
        ),
        errors,
    )
    require(
        "src/Dig.Application/Combat/CompleteHealingJobHandler.cs",
        ("SkillGrantSourceKind.ServiceCompleted", "ApplyConfirmed(skillBundle)"),
        errors,
    )
    require(
        "src/Dig.Application/Combat/ResolveCombatAttackHandler.cs",
        (
            "ValidateSourceIntent",
            '":weapon:"',
            '":shield:"',
            "CombatApplicationErrors.CombatIntentInactive",
        ),
        errors,
    )
    require(
        "src/Dig.Application/World/CaveRoomSkillAccessPolicy.cs",
        ("CaveRoomPresetKind.Medium", "StoneworkThresholdUnits(3)"),
        errors,
    )
    require(
        "src/Dig.Application/Production/ProductionExecutionUseCases.cs",
        ("ProductionWorkContext.ForRecipe", "workerAgent.CreateSnapshot"),
        errors,
    )
    require(
        "unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigWorldSession.Deposits.cs",
        (
            "DefaultSkillGrantProfileIds.Metallurgy",
            "DefaultSkillGrantProfileIds.Alchemy",
            "ResolveExcavationSkillGrantProfile",
        ),
        errors,
    )
    require(
        "unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigTerrainWorkSession.Composition.cs",
        ("ResolveExcavationSkillGrantProfile(targetCell)",),
        errors,
    )
    require(
        "tests/Dig.Tests/TerrainDepositSkillIntegrationTests.cs",
        (
            "Completed_deposit_job_uses_profile_carried_by_definition",
            "AgentSkillCatalog.Metallurgy",
            "AgentSkillCatalog.Alchemy",
        ),
        errors,
    )
    require(
        "unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigUnityBootstrap.cs",
        ("agentSession.SkillGrants",),
        errors,
    )
    require(
        "unity/Dig.Unity/Assets/Dig.Unity/Runtime/DigGameHudCanvas.SkillInspector.cs",
        (
            "skills.All",
            "skills.TopFive",
            '"Top Skill "',
            "Color.Lerp",
            "FormatSkillReport",
            "FormatDiagnosticUnits",
            "useShortName ? 9 : 7",
            "thresholds 20/40/60",
        ),
        errors,
    )
    require(
        "unity/Dig.Unity/Assets/Dig.Unity/Tests/PlayMode/SkillInspectorPlayModeTests.cs",
        (
            "Inspector_renders_twelve_skills_capacity_thresholds_and_gradient",
            "Top_five_uses_value_then_stable_id_from_same_snapshot",
            "Roster_renders_exactly_five_highest_skills_in_snapshot_order",
            "BuildSkillInspector",
            "BuildTopSkillList",
            "University 100/100",
        ),
        errors,
    )
    require(
        "src/Dig.Application/Saving/AgentSkillsSaveData.cs",
        ("PrecisionVersion", "UnitsPerPoint", "AppliedSourceKeys", "MigrationSteps"),
        errors,
    )
    if errors:
        print("Unity skills/progression contracts failed:", file=sys.stderr)
        for error in errors:
            print(f"- {error}", file=sys.stderr)
        return 1
    print("PASS: Unity skills/progression contracts")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
