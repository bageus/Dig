using Dig.Application.Agents;
using Dig.Application.Saving;
using Dig.Domain.Agents;
using Dig.Domain.Buildings;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;
using Dig.Infrastructure.InMemory;
using Dig.Infrastructure.Saving;
using System;
using System.Collections.Generic;
using Xunit;

namespace Dig.Tests
{

public sealed class AgentSkillSaveRoundTripTests
{
    private static readonly MaterialId Rock = new MaterialId("skill-save.rock");
    private static readonly ItemId Ore = new ItemId("skill-save.ore");
    private static readonly EntityId WorkerId = EntityId.Parse(
        "f3000000000000000000000000000003");

    [Fact]
    public void Round_trip_preserves_capacity_report_and_idempotency()
    {
        SaveHarness harness = new SaveHarness();
        AgentState agent = new AgentState(
            WorkerId,
            "Save Skill Dwarf",
            AgentTestFactory.CreateNeeds(8_000, 8_000, 8_000, 10_000),
            AgentTestFactory.CreateWorkSchedule());
        Assert.True(agent.ExpandTotalSkillCapacity(15_000, tick: 9).IsSuccess);
        SkillGrantBundle bundle = new SkillGrantBundle(
            WorkerId,
            SkillGrantSourceKind.JobCompleted,
            "saved-source",
            tick: 10,
            new[] { new SkillGrant(AgentSkillCatalog.Stonework, 250) });
        Assert.True(agent.ApplySkillGrant(bundle).IsSuccess);

        Result<LoadedGameState> loaded = harness.RoundTrip(agent);

        Assert.True(loaded.IsSuccess);
        AgentSkillProgressionSnapshot progression = loaded.Value.AgentSkills[WorkerId];
        Assert.Equal(15_000, progression.TotalCapacityUnits);
        Assert.Equal(250, progression.GetLevel(AgentSkillCatalog.Stonework));
        Assert.Equal("saved-source", progression.LastReport!.SourceId);
        InMemoryAgentRepository repository = new InMemoryAgentRepository();
        AgentState restored = AgentTestFactory.CreateAgent(id: WorkerId);
        Assert.True(repository.Add(restored).IsSuccess);
        Assert.True(new LoadedAgentSkillProgressionRestorer(repository)
            .Restore(loaded.Value).IsSuccess);
        Assert.True(restored.ApplySkillGrant(bundle).Value.WasAlreadyApplied);
    }

    [Fact]
    public void Legacy_precision_uses_largest_remainder_and_reports_migration()
    {
        SaveHarness harness = new SaveHarness();
        SaveGameDocument document = harness.Build(AgentTestFactory.CreateAgent(id: WorkerId));
        AgentSkillProgressionSaveData saved = Assert.Single(document.AgentSkills.Agents);
        saved.PrecisionVersion = 0;
        saved.UnitsPerPoint = 1_000;
        saved.TotalCapacityUnits = 100_000;
        foreach (AgentSkillValueSaveData value in saved.Values)
        {
            value.Units = value.SkillId == AgentSkillCatalog.Stonework.ToString()
                ? 12_345
                : value.SkillId == AgentSkillCatalog.Logistics.ToString()
                    ? 23_456
                    : 0;
        }

        Result<LoadedGameState> loaded = harness.Load(document);

        Assert.True(loaded.IsSuccess);
        AgentSkillProgressionSnapshot progression = loaded.Value.AgentSkills[WorkerId];
        Assert.Equal(1_234, progression.GetLevel(AgentSkillCatalog.Stonework));
        Assert.Equal(2_346, progression.GetLevel(AgentSkillCatalog.Logistics));
        Assert.Equal(3_580, progression.UsedCapacityUnits);
        Assert.Equal(AgentSkillCatalog.BaseCapacityUnits,
            progression.TotalCapacityUnits);
        Assert.Contains(
            "agent-skills.precision-v0-to-v1:" + WorkerId,
            loaded.Value.MigrationReport.AppliedSteps);
        Assert.Null(progression.LastReport);
    }

    [Fact]
    public void Legacy_precision_cannot_inflate_capacity_to_accept_invalid_sum()
    {
        SaveHarness harness = new SaveHarness();
        SaveGameDocument document = harness.Build(
            AgentTestFactory.CreateAgent(id: WorkerId));
        AgentSkillProgressionSaveData saved = Assert.Single(document.AgentSkills.Agents);
        saved.PrecisionVersion = 0;
        saved.UnitsPerPoint = 1_000;
        saved.TotalCapacityUnits = 100_000;
        foreach (AgentSkillValueSaveData value in saved.Values)
        {
            value.Units = value.SkillId == AgentSkillCatalog.Stonework.ToString()
                ? 70_000
                : value.SkillId == AgentSkillCatalog.Logistics.ToString()
                    ? 40_000
                    : 0;
        }

        Result<LoadedGameState> loaded = harness.Load(document);

        Assert.True(loaded.IsFailure);
        Assert.Equal(SaveErrors.InvalidDocument, loaded.Error);
    }

    [Fact]
    public void Missing_skill_value_is_rejected_as_invalid_document()
    {
        SaveHarness harness = new SaveHarness();
        SaveGameDocument document = harness.Build(
            AgentTestFactory.CreateAgent(id: WorkerId));
        AgentSkillProgressionSaveData saved = Assert.Single(document.AgentSkills.Agents);
        saved.Values[0] = null!;

        Result<LoadedGameState> loaded = harness.Load(document);

        Assert.True(loaded.IsFailure);
        Assert.Equal(SaveErrors.InvalidDocument, loaded.Error);
    }

    [Fact]
    public void Save_service_load_restores_progression_into_live_repository()
    {
        SaveHarness harness = new SaveHarness();
        AgentState source = AgentTestFactory.CreateAgent(id: WorkerId);
        Assert.True(source.ApplySkillGrant(new SkillGrantBundle(
            WorkerId,
            SkillGrantSourceKind.JobCompleted,
            "service-round-trip",
            tick: 10,
            new[] { new SkillGrant(AgentSkillCatalog.Stonework, 375) })).IsSuccess);
        InMemoryAgentRepository targetRepository = new InMemoryAgentRepository();
        AgentState target = AgentTestFactory.CreateAgent(id: WorkerId);
        Assert.True(targetRepository.Add(target).IsSuccess);

        Result<LoadedGameState> loaded = harness.SaveAndLoadThroughService(
            source,
            targetRepository);

        Assert.True(loaded.IsSuccess, loaded.Error?.ToString());
        Assert.Equal(375, target.CreateSnapshot(10)
            .GetSkillLevel(AgentSkillCatalog.Stonework));
        Assert.True(target.ApplySkillGrant(new SkillGrantBundle(
            WorkerId,
            SkillGrantSourceKind.JobCompleted,
            "service-round-trip",
            tick: 11,
            new[] { new SkillGrant(AgentSkillCatalog.Stonework, 375) }))
            .Value.WasAlreadyApplied);
    }

    [Fact]
    public void Round_trip_preserves_automatic_planning_preference()
    {
        SaveHarness harness = new SaveHarness();
        AgentState source = AgentTestFactory.CreateAgent(id: WorkerId);
        Assert.True(source.SetAutomaticPlanningEnabled(false, tick: 5).IsSuccess);
        InMemoryAgentRepository targetRepository = new InMemoryAgentRepository();
        AgentState target = AgentTestFactory.CreateAgent(id: WorkerId);
        Assert.True(targetRepository.Add(target).IsSuccess);

        Result<LoadedGameState> loaded = harness.SaveAndLoadThroughService(
            source,
            targetRepository);

        Assert.True(loaded.IsSuccess, loaded.Error?.ToString());
        Assert.False(loaded.Value.AgentAutomaticPlanning[WorkerId]);
        Assert.False(target.AutomaticPlanningEnabled);
    }

    [Fact]
    public void Save_without_preference_defaults_to_automatic_planning()
    {
        SaveHarness harness = new SaveHarness();
        SaveGameDocument document = harness.Build(
            AgentTestFactory.CreateAgent(id: WorkerId));
        Assert.Single(document.AgentSkills.Agents).AutomaticPlanningEnabled = null;

        Result<LoadedGameState> loaded = harness.Load(document);

        Assert.True(loaded.IsSuccess);
        Assert.True(loaded.Value.AgentAutomaticPlanning[WorkerId]);
    }

    private sealed class SaveHarness
    {
        private readonly MaterialCatalog _materials;
        private readonly ItemCatalog _items;
        private readonly WorldState _world;
        private readonly InventoryState _inventory;
        private readonly JobSystem _jobs = new JobSystem();

        public SaveHarness()
        {
            _materials = new MaterialCatalog(new[]
            {
                new MaterialDefinition(Rock, isSolid: true, hardness: 100),
            });
            _items = new ItemCatalog(new[]
            {
                new ItemDefinition(Ore, "Ore", 100, isTool: false),
            });
            _world = WorldState.CreateFilled(
                new WorldSize(2, 2),
                chunkSize: 1,
                _materials,
                Rock,
                explored: true).Value;
            _inventory = new InventoryState(_items);
        }

        public SaveGameDocument Build(AgentState agent)
        {
            return Builder().Build(new SaveGameContext(
                new SaveMetadataData
                {
                    SlotId = "skills",
                    DisplayName = "Skills",
                    SavedAtUtc = "2026-07-21T10:00:00Z",
                    SimulationTick = 10,
                    WorldSeed = 7,
                    GeneratorVersion = 1,
                },
                _world,
                _inventory,
                _jobs,
                new BuildingsState(),
                new[] { agent }));
        }

        public Result<LoadedGameState> RoundTrip(AgentState agent)
        {
            DataContractJsonSaveCodec codec = new DataContractJsonSaveCodec();
            SaveGameDocument decoded = codec.Deserialize(codec.Serialize(Build(agent)));
            return Load(decoded);
        }

        public Result<LoadedGameState> Load(SaveGameDocument document)
        {
            return Loader().Load(document, _materials, _items);
        }

        public Result<LoadedGameState> SaveAndLoadThroughService(
            AgentState agent,
            IAgentRepository targetRepository)
        {
            InMemorySaveSlotStore store = new InMemorySaveSlotStore();
            SaveGameService service = new SaveGameService(Builder(), Loader(), store);
            service.Save(CreateContext(agent));
            return service.Load("skills", _materials, _items, targetRepository);
        }

        private static SaveGameBuilder Builder()
        {
            return new SaveGameBuilder(new JobDefinitionSaveRegistry(
                new IJobDefinitionSaveCodec[]
                {
                    new DigJobDefinitionSaveCodec(),
                }));
        }

        private static SaveGameLoader Loader()
        {
            return new SaveGameLoader(
                new SaveMigrationPipeline(System.Array.Empty<ISaveMigration>()),
                new JobDefinitionSaveRegistry(
                    new IJobDefinitionSaveCodec[]
                    {
                        new DigJobDefinitionSaveCodec(),
                    }));
        }

        private SaveGameContext CreateContext(AgentState agent)
        {
            return new SaveGameContext(
                new SaveMetadataData
                {
                    SlotId = "skills",
                    DisplayName = "Skills",
                    SavedAtUtc = "2026-07-21T10:00:00Z",
                    SimulationTick = 10,
                    WorldSeed = 7,
                    GeneratorVersion = 1,
                },
                _world,
                _inventory,
                _jobs,
                new BuildingsState(),
                new[] { agent });
        }
    }

    private sealed class InMemorySaveSlotStore : ISaveSlotStore
    {
        private SaveGameDocument? _document;

        public void Save(string slotId, SaveGameDocument document)
        {
            _document = document;
        }

        public SaveGameDocument Load(string slotId)
        {
            return _document ?? throw new InvalidOperationException("Save is missing.");
        }

        public IReadOnlyList<SaveSlotInfo> List()
        {
            return Array.Empty<SaveSlotInfo>();
        }
    }
}

}
