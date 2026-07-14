using System.Linq;
using Dig.Application.Colonies;
using Dig.Domain.Colonies;
using Dig.Domain.Core;
using Dig.Infrastructure.InMemory;
using Dig.Presentation.Colonies;
using Xunit;

namespace Dig.Tests
{

public sealed class ArchitectureFoundationTests
{
    [Fact]
    public void Entity_id_round_trips_through_stable_text_format()
    {
        EntityId original = EntityId.New();

        EntityId parsed = EntityId.Parse(original.ToString());

        Assert.Equal(original, parsed);
        Assert.Equal(32, original.ToString().Length);
    }

    [Fact]
    public void Invalid_rename_does_not_mutate_authoritative_state()
    {
        ColonyState colony = CreateColony("Foundry");
        long versionBefore = colony.Version;

        Result result = colony.Rename("   ", tick: 10);

        Assert.True(result.IsFailure);
        Assert.Equal("Foundry", colony.Name);
        Assert.Equal(versionBefore, colony.Version);
        Assert.Empty(colony.PeekUncommittedEvents());
    }

    [Fact]
    public void Command_handler_changes_state_through_owner_and_publishes_fact()
    {
        ColonyState colony = CreateColony("Foundry");
        InMemoryColonyRepository repository = new InMemoryColonyRepository();
        InMemoryEventJournal journal = new InMemoryEventJournal();
        repository.Save(colony);

        RenameColonyCommandHandler handler = new RenameColonyCommandHandler(
            repository,
            journal);

        Result result = handler.Handle(new RenameColonyCommand(
            colony.Id,
            "Deep Foundry",
            tick: 42));

        Assert.True(result.IsSuccess);
        Assert.Equal("Deep Foundry", repository.Get(colony.Id)!.Name);
        ColonyRenamed renamed = Assert.IsType<ColonyRenamed>(Assert.Single(journal.Events));
        Assert.Equal(42, renamed.Tick);
        Assert.Equal("Foundry", renamed.PreviousName);
        Assert.Equal("Deep Foundry", renamed.CurrentName);
    }

    [Fact]
    public void Query_and_presenter_read_without_mutating_domain_state()
    {
        ColonyState colony = CreateColony("Foundry");
        InMemoryColonyRepository repository = new InMemoryColonyRepository();
        repository.Save(colony);
        long versionBefore = colony.Version;

        GetColonySummaryQueryHandler queryHandler = new GetColonySummaryQueryHandler(repository);
        ColonyPresenter presenter = new ColonyPresenter(queryHandler);

        Result<ColonyViewModel> result = presenter.Load(colony.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal("Foundry", result.Value.DisplayName);
        Assert.Equal(versionBefore, colony.Version);
        Assert.Empty(colony.PeekUncommittedEvents());
    }

    [Fact]
    public void Missing_colony_returns_explicit_error_instead_of_null_result()
    {
        InMemoryColonyRepository repository = new InMemoryColonyRepository();
        InMemoryEventJournal journal = new InMemoryEventJournal();
        RenameColonyCommandHandler handler = new RenameColonyCommandHandler(
            repository,
            journal);

        Result result = handler.Handle(new RenameColonyCommand(
            EntityId.New(),
            "Unknown",
            tick: 1));

        Assert.True(result.IsFailure);
        Assert.Equal(ColonyErrors.NotFound, result.Error);
        Assert.Empty(journal.Events);
    }

    private static ColonyState CreateColony(string name)
    {
        Result<ColonyState> result = ColonyState.Create(EntityId.New(), name);
        Assert.True(result.IsSuccess);
        return result.Value;
    }
}
}
