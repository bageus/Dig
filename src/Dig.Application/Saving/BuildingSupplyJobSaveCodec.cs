using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Inventory;
using Dig.Domain.Jobs;
using Dig.Domain.World;

namespace Dig.Application.Saving
{

public sealed class BuildingSupplyJobSaveCodec : IJobDefinitionSaveCodec
{
    public const string StableTypeId = "job.building_supply.v1";

    public string TypeId => StableTypeId;

    public bool CanEncode(JobDefinition definition) =>
        definition is BuildingSupplyJobDefinition;

    public JobDefinitionSaveData Encode(JobDefinition definition)
    {
        BuildingSupplyJobDefinition job = definition as BuildingSupplyJobDefinition
            ?? throw new ArgumentException("Expected a building supply job.", nameof(definition));
        List<SavePropertyData> properties = new List<SavePropertyData>
        {
            Property("building.id", job.BuildingId),
            Property("work.x", job.WorkPosition.X),
            Property("work.y", job.WorkPosition.Y),
            Property("work.z", job.WorkPosition.Z),
            Property("allocation.count", job.Allocations.Count),
            Property("request.count", job.RequestedItems.Count),
            Property("transit.count", job.TransitStackIds.Count),
            Property("deposit.count", job.DepositStackIds.Count),
        };
        for (int index = 0; index < job.Allocations.Count; index++)
        {
            ItemReservationAllocation allocation = job.Allocations[index];
            properties.Add(Property($"allocation.{index}.stack", allocation.StackId));
            properties.Add(Property($"allocation.{index}.item", allocation.ItemId));
            properties.Add(Property($"allocation.{index}.quantity", allocation.Quantity));
        }

        for (int index = 0; index < job.RequestedItems.Count; index++)
        {
            ItemConsumptionRequest request = job.RequestedItems[index];
            properties.Add(Property($"request.{index}.item", request.ItemId));
            properties.Add(Property($"request.{index}.quantity", request.Quantity));
        }

        AddIds(properties, "transit", job.TransitStackIds);
        AddIds(properties, "deposit", job.DepositStackIds);
        return new JobDefinitionSaveData
        {
            TypeId = TypeId,
            JobId = job.Id.ToString(),
            Priority = job.Priority,
            CreatedTick = job.CreatedTick,
            MaximumRetries = job.RetryPolicy.MaximumRetries,
            RetryDelayTicks = job.RetryPolicy.RetryDelayTicks,
            Dependencies = job.Dependencies.Select(value => value.ToString()).ToList(),
            Properties = properties,
        };
    }

    public JobDefinition Decode(JobDefinitionSaveData data)
    {
        IReadOnlyDictionary<string, string> values = data.Properties.ToDictionary(
            value => value.Key,
            value => value.Value,
            StringComparer.Ordinal);
        int allocationCount = ParseInt(values, "allocation.count");
        ItemReservationAllocation[] allocations = Enumerable.Range(0, allocationCount)
            .Select(index => new ItemReservationAllocation(
                EntityId.Parse(Get(values, $"allocation.{index}.stack")),
                new ItemId(Get(values, $"allocation.{index}.item")),
                ParseInt(values, $"allocation.{index}.quantity")))
            .ToArray();
        EntityId id = EntityId.Parse(data.JobId);
        EntityId buildingId = EntityId.Parse(Get(values, "building.id"));
        CellId workPosition = new CellId(
            ParseInt(values, "work.x"),
            ParseInt(values, "work.y"),
            ParseInt(values, "work.z"));
        EntityId[] transit = ParseIds(values, "transit");
        EntityId[] deposit = ParseIds(values, "deposit");
        JobRetryPolicy retry = new JobRetryPolicy(
            data.MaximumRetries,
            data.RetryDelayTicks);
        EntityId[] dependencies = data.Dependencies
            .Select(EntityId.Parse)
            .ToArray();
        if (allocations.Length > 0)
        {
            return new BuildingSupplyJobDefinition(
                id,
                buildingId,
                workPosition,
                allocations,
                transit,
                deposit,
                data.Priority,
                data.CreatedTick,
                retry,
                dependencies);
        }

        int requestCount = ParseInt(values, "request.count");
        ItemConsumptionRequest[] requests = Enumerable.Range(0, requestCount)
            .Select(index => new ItemConsumptionRequest(
                new ItemId(Get(values, $"request.{index}.item")),
                ParseInt(values, $"request.{index}.quantity")))
            .ToArray();
        return new BuildingSupplyJobDefinition(
            id,
            buildingId,
            workPosition,
            requests,
            transit,
            deposit,
            data.Priority,
            data.CreatedTick,
            retry,
            dependencies);
    }

    private static void AddIds(
        ICollection<SavePropertyData> values,
        string prefix,
        IReadOnlyList<EntityId> ids)
    {
        for (int index = 0; index < ids.Count; index++)
        {
            values.Add(Property($"{prefix}.{index}", ids[index]));
        }
    }

    private static EntityId[] ParseIds(
        IReadOnlyDictionary<string, string> values,
        string prefix)
    {
        int count = ParseInt(values, $"{prefix}.count");
        return Enumerable.Range(0, count)
            .Select(index => EntityId.Parse(Get(values, $"{prefix}.{index}")))
            .ToArray();
    }

    private static SavePropertyData Property(string key, object value)
    {
        return new SavePropertyData
        {
            Key = key,
            Value = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty,
        };
    }

    private static string Get(IReadOnlyDictionary<string, string> values, string key)
    {
        if (!values.TryGetValue(key, out string? value) || string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Missing building supply property '{key}'.");
        }

        return value;
    }

    private static int ParseInt(IReadOnlyDictionary<string, string> values, string key)
    {
        if (!int.TryParse(Get(values, key), NumberStyles.Integer,
            CultureInfo.InvariantCulture, out int value) || value < 0)
        {
            throw new InvalidOperationException($"Invalid building supply property '{key}'.");
        }

        return value;
    }
}

}
