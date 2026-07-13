using Dig.Application.Colonies;
using Dig.Application.Messaging;
using Dig.Domain.Colonies;
using Dig.Domain.Core;
using Dig.Domain.Runtime;
using Dig.Infrastructure.InMemory;
using Xunit;

namespace Dig.Tests;

public sealed class RuntimeIdentityAndPipelineTests
{
    [Fact]
    public void Same_seed_produces_same_unique_entity_identifiers()
    {
        SimulationState first = CreateState(seed: 1234);
        SimulationState second = CreateState(seed: 1234);

        EntityId[] firstIds = RegisterMany(first.Entities, count: 32);
        EntityId[] secondIds = RegisterMany(second.Entities, count: 32);

        Assert.Equal(firstIds, secondIds);
        Assert.Equal(firstIds.Length, firstIds.Distinct().Count());
        Assert.Equal(32, first.Entities.Count);
    }

    [Fact]
    public void Registry_and_random_stream_restore_continue_same_sequence()
    {
        SimulationState original = CreateState(seed: 55);
        EntityId[] existingIds = RegisterMany(original.Entities, count: 4);
        EntityRegistrySnapshot registrySnapshot = original.Entities.CaptureSnapshot();
        IReadOnlyList<RandomStreamSnapshot> randomSnapshots =
            original.RandomStreams.CaptureSnapshots();

        RandomStreamCatalog restoredRandom = RandomStreamCatalog.Restore(
            original.RandomStreams.WorldSeed,
            randomSnapshots);
        EntityRegistry restoredRegistry = EntityRegistry.Restore(
            restoredRandom,
            registrySnapshot);

        EntityId originalNext = RegisterOne(original.Entities);
        EntityId restoredNext = RegisterOne(restoredRegistry);

        Assert.Equal(originalNext, restoredNext);
        Assert.All(existingIds, id => Assert.True(restoredRegistry.Contains(id)));
    }

    [Fact]
    public void Removed_entity_disappears_and_second_removal_is_explicit_failure()
    {
        SimulationState state = CreateState(seed: 9);
        EntityId entityId = RegisterOne(state.Entities);

        Result firstRemoval = state.Entities.Remove(entityId);
        Result secondRemoval = state.Entities.Remove(entityId);

        Assert.True(firstRemoval.IsSuccess);
        Assert.False(state.Entities.Contains(entityId));
        Assert.Equal(0, state.Entities.Count);
        Assert.True(secondRemoval.IsFailure);
        Assert.Equal(EntityRegistryErrors.NotRegistered, secondRemoval.Error);
    }

    [Fact]
    public void Command_pipeline_records_result_while_events_remain_facts()
    {
        ColonyState colony = CreateColony("Foundry");
        InMemoryColonyRepository repository = new InMemoryColonyRepository();
        InMemoryExecutionJournal journal = new InMemoryExecutionJournal();
        repository.Save(colony);
        RenameColonyCommandHandler handler = new RenameColonyCommandHandler(
            repository,
            journal);
        CommandPipeline pipeline = new CommandPipeline(journal);

        Result result = pipeline.Execute(
            new RenameColonyCommand(colony.Id, "Deep Foundry", tick: 12),
            handler,
            tick: 12);

        Assert.True(result.IsSuccess);
        CommandJournalEntry command = Assert.Single(journal.Commands);
        Assert.Equal(nameof(RenameColonyCommand), command.CommandName);
        Assert.True(command.Succeeded);
        ColonyRenamed domainEvent = Assert.IsType<ColonyRenamed>(
            Assert.Single(journal.Events));
        Assert.Equal(12, domainEvent.Tick);
        Assert.Equal("Deep Foundry", domainEvent.CurrentName);
    }

    [Fact]
    public void Query_pipeline_has_no_side_effects()
    {
        ColonyState colony = CreateColony("Foundry");
        InMemoryColonyRepository repository = new InMemoryColonyRepository();
        repository.Save(colony);
        long versionBefore = colony.Version;
        QueryPipeline pipeline = new QueryPipeline();
        GetColonySummaryQueryHandler handler =
            new GetColonySummaryQueryHandler(repository);

        Result<ColonySummary> result = pipeline.Execute(
            new GetColonySummaryQuery(colony.Id),
            handler);

        Assert.True(result.IsSuccess);
        Assert.Equal("Foundry", result.Value.Name);
        Assert.Equal(versionBefore, colony.Version);
        Assert.Empty(colony.PeekUncommittedEvents());
    }

    private static SimulationState CreateState(ulong seed)
    {
        return SimulationState.Create(
            seed,
            TimeSpan.FromMilliseconds(100));
    }

    private static EntityId[] RegisterMany(
        EntityRegistry registry,
        int count)
    {
        EntityId[] result = new EntityId[count];
        for (int index = 0; index < count; index++)
        {
            result[index] = RegisterOne(registry);
        }

        return result;
    }

    private static EntityId RegisterOne(EntityRegistry registry)
    {
        Result<EntityId> result = registry.RegisterNew();
        Assert.True(result.IsSuccess);
        return result.Value;
    }

    private static ColonyState CreateColony(string name)
    {
        Result<ColonyState> result = ColonyState.Create(EntityId.New(), name);
        Assert.True(result.IsSuccess);
        return result.Value;
    }
}
