using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Core;
using Dig.Domain.Inventory;

namespace Dig.Presentation.Inventory
{

public sealed class ResidentWorkRateViewModel
{
    public ResidentWorkRateViewModel(
        string residentId,
        string? equippedItemId,
        int miningBaseIntervalTicks,
        int miningIntervalTicks,
        int constructionBaseIntervalTicks,
        int constructionIntervalTicks)
    {
        if (string.IsNullOrWhiteSpace(residentId))
        {
            throw new ArgumentException("Resident id is required.", nameof(residentId));
        }

        ValidateInterval(miningBaseIntervalTicks, nameof(miningBaseIntervalTicks));
        ValidateInterval(miningIntervalTicks, nameof(miningIntervalTicks));
        ValidateInterval(constructionBaseIntervalTicks, nameof(constructionBaseIntervalTicks));
        ValidateInterval(constructionIntervalTicks, nameof(constructionIntervalTicks));
        ResidentId = residentId.Trim();
        EquippedItemId = string.IsNullOrWhiteSpace(equippedItemId)
            ? null
            : equippedItemId.Trim();
        MiningBaseIntervalTicks = miningBaseIntervalTicks;
        MiningIntervalTicks = miningIntervalTicks;
        ConstructionBaseIntervalTicks = constructionBaseIntervalTicks;
        ConstructionIntervalTicks = constructionIntervalTicks;
    }

    public string ResidentId { get; }
    public string? EquippedItemId { get; }
    public int MiningBaseIntervalTicks { get; }
    public int MiningIntervalTicks { get; }
    public int ConstructionBaseIntervalTicks { get; }
    public int ConstructionIntervalTicks { get; }
    public double MiningSpeedMultiplier =>
        (double)MiningBaseIntervalTicks / MiningIntervalTicks;
    public double ConstructionSpeedMultiplier =>
        (double)ConstructionBaseIntervalTicks / ConstructionIntervalTicks;

    private static void ValidateInterval(int value, string parameterName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}

public sealed class ResidentWorkRatePresenter
{
    private readonly EquipmentRates _rates;
    private readonly int _miningBaseIntervalTicks;
    private readonly int _constructionBaseIntervalTicks;

    public ResidentWorkRatePresenter(
        EquipmentRates rates,
        int miningBaseIntervalTicks,
        int constructionBaseIntervalTicks)
    {
        _rates = rates ?? throw new ArgumentNullException(nameof(rates));
        if (miningBaseIntervalTicks <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(miningBaseIntervalTicks));
        }

        if (constructionBaseIntervalTicks <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(constructionBaseIntervalTicks));
        }

        _miningBaseIntervalTicks = miningBaseIntervalTicks;
        _constructionBaseIntervalTicks = constructionBaseIntervalTicks;
    }

    public IReadOnlyList<ResidentWorkRateViewModel> Present(
        IEnumerable<string> residentIds,
        params InventorySnapshot[] snapshots)
    {
        if (residentIds == null)
        {
            throw new ArgumentNullException(nameof(residentIds));
        }

        if (snapshots == null || snapshots.Any(snapshot => snapshot is null))
        {
            throw new ArgumentNullException(nameof(snapshots));
        }

        string[] residents = residentIds
            .Select(value => value?.Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Cast<string>()
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        List<ResidentWorkRateViewModel> models = new List<ResidentWorkRateViewModel>();
        foreach (string residentId in residents)
        {
            EntityId resident = EntityId.Parse(residentId);
            ItemStackSnapshot[] equipped = snapshots
                .SelectMany(snapshot => snapshot.Stacks)
                .Where(stack => stack.Location == ItemLocation.EquippedBy(resident))
                .ToArray();
            if (equipped.Length > 1)
            {
                throw new InvalidOperationException(
                    "A resident cannot have more than one equipped item.");
            }

            int mining = _rates.ResolveIntervalTicks(
                resident,
                EquipmentWorkKind.Mining,
                _miningBaseIntervalTicks,
                snapshots);
            int construction = _rates.ResolveIntervalTicks(
                resident,
                EquipmentWorkKind.Construction,
                _constructionBaseIntervalTicks,
                snapshots);
            models.Add(new ResidentWorkRateViewModel(
                residentId,
                equipped.Length == 0 ? null : equipped[0].ItemId.ToString(),
                _miningBaseIntervalTicks,
                mining,
                _constructionBaseIntervalTicks,
                construction));
        }

        return new ReadOnlyCollection<ResidentWorkRateViewModel>(models);
    }
}

}