using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Dig.Application.Agents;
using Dig.Application.Inventory;
using Dig.Application.Jobs;
using Dig.Domain.Buildings;
using Dig.Domain.Inventory;
using Dig.Domain.Storage;

namespace Dig.Headless.Soak
{

internal static class HeadlessSoakStateHasher
{
    public static string Compute(
        long tick,
        int entityCount,
        IAgentRepository agents,
        IInventoryRepository inventoryRepository,
        IStorageRepository storageRepository,
        IJobRepository jobRepository,
        IBuildingFacilitiesRepository facilitiesRepository)
    {
        StringBuilder value = new StringBuilder();
        value.Append("tick=").Append(tick)
            .Append(";entities=").Append(entityCount).AppendLine();

        foreach (Dig.Domain.Agents.AgentSnapshot agent in agents.GetAll()
            .Select(item => item.CreateSnapshot(tick))
            .OrderBy(item => item.Id.ToString(), StringComparer.Ordinal))
        {
            value.Append("agent|").Append(agent.Id)
                .Append('|').Append(agent.IsAlive)
                .Append('|').Append(agent.SpatialPosition)
                .Append('|').Append(agent.Needs.Nutrition.Points)
                .Append('|').Append(agent.Needs.Alertness.Points)
                .Append('|').Append(agent.Needs.Mood.Points)
                .Append('|').Append(agent.Needs.Health.Points)
                .Append('|').Append(agent.ScheduledActivity)
                .Append('|').Append(agent.ActiveAction?.IntentKind.ToString() ?? "none")
                .Append('|').Append(agent.ActiveAction?.ElapsedTicks ?? 0)
                .Append('|').Append(agent.ActiveAction?.Target?.ToString() ?? "none")
                .Append('|').Append(agent.SkillProgression?.SchemaVersion ?? 0)
                .Append('|').Append(agent.SkillProgression?.PrecisionVersion ?? 0)
                .Append('|').Append(agent.SkillProgression?.TotalCapacityUnits ?? 0);
            foreach (Dig.Domain.Agents.AgentSkillValue skill in agent.Skills
                .OrderBy(item => item.Id))
            {
                value.Append('|').Append(skill.Id).Append(':').Append(skill.Level);
            }

            foreach (string source in agent.SkillProgression?.AppliedSourceKeys
                ?? Array.Empty<string>())
            {
                value.Append('|').Append("source:").Append(source);
            }

            value.AppendLine();
        }

        InventorySnapshot inventory = inventoryRepository.Get().CreateSnapshot();
        foreach (ItemStackSnapshot stack in inventory.Stacks
            .OrderBy(item => item.StackId.ToString(), StringComparer.Ordinal))
        {
            value.Append("stack|").Append(stack.StackId)
                .Append('|').Append(stack.ItemId)
                .Append('|').Append(stack.Quantity)
                .Append('|').Append(stack.Location)
                .Append('|').Append(stack.ReservedQuantity);
            foreach (ItemQuantityReservationSnapshot reservation in stack.Reservations
                .OrderBy(item => item.JobId.ToString(), StringComparer.Ordinal))
            {
                value.Append('|').Append(reservation.JobId)
                    .Append(':').Append(reservation.Quantity);
            }

            value.AppendLine();
        }

        foreach (Dig.Domain.Jobs.JobSnapshot job in jobRepository.Get().GetAll()
            .OrderBy(item => item.Id.ToString(), StringComparer.Ordinal))
        {
            value.Append("job|").Append(job.Id)
                .Append('|').Append(job.Definition.Description)
                .Append('|').Append(job.Status)
                .Append('|').Append(job.Stage)
                .Append('|').Append(job.AssignedAgentId?.ToString() ?? "none")
                .Append('|').Append(job.RetryCount)
                .AppendLine();
        }

        foreach (StorageReservationSnapshot reservation in storageRepository.Get()
            .GetReservations()
            .OrderBy(item => item.JobId.ToString(), StringComparer.Ordinal))
        {
            value.Append("storage|").Append(reservation.JobId)
                .Append('|').Append(reservation.ZoneId)
                .Append('|').Append(reservation.ItemId)
                .Append('|').Append(reservation.Quantity)
                .AppendLine();
        }

        foreach (BuildingFacilityReservation reservation in facilitiesRepository.Get()
            .GetReservations()
            .OrderBy(item => item.FacilityId.ToString(), StringComparer.Ordinal))
        {
            value.Append("facility|").Append(reservation.FacilityId)
                .Append('|').Append(reservation.AgentId)
                .AppendLine();
        }

        byte[] bytes = Encoding.UTF8.GetBytes(value.ToString());
        return Convert.ToHexString(SHA256.HashData(bytes));
    }
}

}
