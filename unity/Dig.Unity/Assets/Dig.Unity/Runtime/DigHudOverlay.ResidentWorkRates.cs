using System;
using System.Collections.Generic;
using Dig.Presentation.Inventory;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigHudOverlay
    {
        private readonly Dictionary<string, ResidentWorkRateViewModel> _residentRates =
            new Dictionary<string, ResidentWorkRateViewModel>(StringComparer.Ordinal);

        internal void SetResidentWorkRates(
            IReadOnlyList<ResidentWorkRateViewModel> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            _residentRates.Clear();
            for (int index = 0; index < values.Count; index++)
            {
                ResidentWorkRateViewModel model = values[index];
                if (_residentRates.ContainsKey(model.ResidentId))
                {
                    throw new InvalidOperationException(
                        "A resident cannot have duplicate rate projections.");
                }

                _residentRates.Add(model.ResidentId, model);
            }
        }

        private void DrawResidentWorkRates()
        {
            if (_selectedAgent == null)
            {
                return;
            }

            GUILayout.Space(6f);
            GUILayout.Label("EFFICIENCY");
            if (!_residentRates.TryGetValue(
                    _selectedAgent.Id,
                    out ResidentWorkRateViewModel? rates))
            {
                GUILayout.Label("Efficiency snapshot unavailable.");
                return;
            }

            GUILayout.Label($"Equipped: {rates.EquippedItemId ?? "none"}");
            GUILayout.Label(
                $"Mining: {rates.MiningBaseIntervalTicks}→{rates.MiningIntervalTicks} ticks "
                + $"| {rates.MiningSpeedMultiplier:0.##}x");
            GUILayout.Label(
                $"Construction: {rates.ConstructionBaseIntervalTicks}→"
                + $"{rates.ConstructionIntervalTicks} ticks "
                + $"| {rates.ConstructionSpeedMultiplier:0.##}x");
        }
    }
}