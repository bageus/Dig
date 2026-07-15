using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Society;

namespace Dig.Application.Society
{

public interface ISocietyRepository
{
    SocietyState Get();

    void Save(SocietyState society);
}

public static class SocietyApplicationErrors
{
    public static readonly DomainError AgentNotFound = new DomainError(
        "society.application.agent_not_found",
        "The dead agent is not registered in the agent repository.");

    public static readonly DomainError AgentStillAlive = new DomainError(
        "society.application.agent_alive",
        "An AgentDied event cannot be synchronized while the agent is alive.");
}

public sealed class ResidentDeathCleanupReport
{
    public ResidentDeathCleanupReport(
        EntityId residentId,
        IEnumerable<EntityId> cancelledJobIds,
        int releasedInventoryQuantity)
    {
        if (residentId.IsEmpty)
        {
            throw new ArgumentException("Resident id cannot be empty.", nameof(residentId));
        }

        if (cancelledJobIds is null)
        {
            throw new ArgumentNullException(nameof(cancelledJobIds));
        }

        if (releasedInventoryQuantity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(releasedInventoryQuantity));
        }

        EntityId[] ordered = cancelledJobIds
            .OrderBy(id => id.ToString(), StringComparer.Ordinal)
            .ToArray();
        ResidentId = residentId;
        CancelledJobIds = new ReadOnlyCollection<EntityId>(ordered);
        ReleasedInventoryQuantity = releasedInventoryQuantity;
    }

    public EntityId ResidentId { get; }

    public IReadOnlyList<EntityId> CancelledJobIds { get; }

    public int ReleasedInventoryQuantity { get; }
}

public sealed class AgentDeathLifecycleReport
{
    public AgentDeathLifecycleReport(
        EntityId residentId,
        bool lifecycleDeathCreated,
        ResidentDeathCleanupReport? cleanup)
    {
        if (residentId.IsEmpty)
        {
            throw new ArgumentException("Resident id cannot be empty.", nameof(residentId));
        }

        ResidentId = residentId;
        LifecycleDeathCreated = lifecycleDeathCreated;
        Cleanup = cleanup;
    }

    public EntityId ResidentId { get; }

    public bool LifecycleDeathCreated { get; }

    public ResidentDeathCleanupReport? Cleanup { get; }
}
}
