using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Application.Messaging;
using Dig.Domain.Content;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.Production;
using Dig.Domain.Technology;

namespace Dig.Application.Production;

public sealed class CancelProductionOrderHandler
    : ICommandHandler<CancelProductionOrderCommand, Result>
{
    private readonly IProductionRepository _productionRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IJobRepository _jobRepository;
    private readonly IEventSink _eventSink;

    public CancelProductionOrderHandler(
        IProductionRepository productionRepository,
        IInventoryRepository inventoryRepository,
        IJobRepository jobRepository,
        IEventSink eventSink)
    {
        _productionRepository = productionRepository;
        _inventoryRepository = inventoryRepository;
        _jobRepository = jobRepository;
        _eventSink = eventSink;
    }

    public Result Handle(CancelProductionOrderCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        ProductionState production = _productionRepository.Get();
        InventoryState inventory = _inventoryRepository.Get();
        JobSystem jobs = _jobRepository.Get();
        Result cancelled = production.Cancel(
            command.OrderId,
            command.Reason,
            command.Tick);
        if (cancelled.IsFailure)
        {
            return cancelled;
        }

        inventory.ReleaseReservations(command.OrderId, command.Tick);
        JobSnapshot? job = jobs.Get(command.JobId);
        if (job is not null && !job.IsTerminal)
        {
            Result jobCancelled = jobs.Cancel(
                command.JobId,
                new JobBlockReason("production_cancelled", command.Reason),
                command.Tick);
            if (jobCancelled.IsFailure)
            {
                return jobCancelled;
            }
        }

        _productionRepository.Save(production);
        _inventoryRepository.Save(inventory);
        _jobRepository.Save(jobs);
        _eventSink.Append(production.DequeueUncommittedEvents());
        _eventSink.Append(inventory.DequeueUncommittedEvents());
        _eventSink.Append(jobs.DequeueUncommittedEvents());
        return Result.Success();
    }
}

public sealed class UnlockTechnologyHandler
    : ICommandHandler<UnlockTechnologyCommand, Result>
{
    private readonly ProductionContentCatalog _content;
    private readonly ITechnologyRepository _technologyRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IEventSink _eventSink;

    public UnlockTechnologyHandler(
        ProductionContentCatalog content,
        ITechnologyRepository technologyRepository,
        IInventoryRepository inventoryRepository,
        IEventSink eventSink)
    {
        _content = content;
        _technologyRepository = technologyRepository;
        _inventoryRepository = inventoryRepository;
        _eventSink = eventSink;
    }

    public Result Handle(UnlockTechnologyCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        TechnologyState technology = _technologyRepository.Get();
        InventoryState inventory = _inventoryRepository.Get();
        TechnologyDefinition definition = _content.GetTechnology(command.TechnologyId);
        Result allowed = technology.CanUnlock(definition);
        if (allowed.IsFailure)
        {
            return allowed;
        }

        ItemConsumptionRequest[] requirements = definition.ResearchItems
            .Select(value => new ItemConsumptionRequest(value.ItemId, value.Quantity))
            .ToArray();
        Result consumed = inventory.ConsumeAvailableAt(
            command.ResearchLocation,
            requirements,
            command.Tick);
        if (consumed.IsFailure)
        {
            return consumed;
        }

        Result unlocked = technology.Unlock(definition, command.Tick);
        if (unlocked.IsFailure)
        {
            throw new InvalidOperationException(
                "Validated technology could not unlock after research consumption.");
        }

        _technologyRepository.Save(technology);
        _inventoryRepository.Save(inventory);
        _eventSink.Append(technology.DequeueUncommittedEvents());
        _eventSink.Append(inventory.DequeueUncommittedEvents());
        return Result.Success();
    }
}

public sealed class GetProductionOrdersHandler
    : IQueryHandler<GetProductionOrdersQuery, IReadOnlyList<ProductionOrderSnapshot>>
{
    private readonly IProductionRepository _repository;

    public GetProductionOrdersHandler(IProductionRepository repository)
    {
        _repository = repository;
    }

    public IReadOnlyList<ProductionOrderSnapshot> Handle(GetProductionOrdersQuery query)
    {
        if (query is null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        return _repository.Get().GetAll();
    }
}
