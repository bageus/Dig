using System;
using Dig.Domain.Inventory;
using Dig.Presentation.Jobs;
using UnityEngine;

namespace Dig.Unity
{
    public sealed partial class DigAgentVisual
    {
        internal const string PickaxeVisualId = "visual.work_tool.pickaxe";
        internal const string AxeVisualId = "visual.work_tool.axe";
        internal const string HammerVisualId = "visual.work_tool.hammer";
        internal const string MealVisualId = "visual.meal.portion";

        private bool IsEating => Model != null
            && string.Equals(
                Model.ActiveIntent,
                "Eat",
                StringComparison.OrdinalIgnoreCase);

        private void RefreshHandEquipment()
        {
            if (_equipmentMaterial == null)
            {
                return;
            }

            string? itemId = IsEating
                ? MealVisualId
                : _workToolVisualKind switch
                {
                    ResidentWorkToolVisualKind.Pickaxe => PickaxeVisualId,
                    ResidentWorkToolVisualKind.Axe => AxeVisualId,
                    ResidentWorkToolVisualKind.Hammer => HammerVisualId,
                    _ => _equipmentModel?.ItemId,
                };
            if (string.IsNullOrWhiteSpace(itemId))
            {
                _equipmentVisual?.Clear();
                return;
            }

            if (_equipmentVisual == null)
            {
                GameObject root = new GameObject("Right Hand Equipment");
                root.transform.SetParent(
                    ResolveSocket(DigResidentSocketKind.RightHand),
                    worldPositionStays: false);
                root.transform.SetLocalPositionAndRotation(
                    Vector3.zero,
                    Quaternion.identity);
                _equipmentVisual = root.AddComponent<DigAgentEquipmentVisual>();
            }

            _equipmentVisual.Configure(
                itemId,
                EquipmentAppearanceKind.Generic,
                _equipmentMaterial);
        }
    }
}
