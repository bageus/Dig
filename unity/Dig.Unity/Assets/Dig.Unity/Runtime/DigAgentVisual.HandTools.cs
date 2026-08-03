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

        private void RefreshHandEquipment()
        {
            if (_equipmentMaterial == null)
            {
                return;
            }

            string? itemId = _workToolVisualKind switch
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
                root.transform.localPosition = Vector3.zero;
                root.transform.localRotation = Quaternion.identity;
                _equipmentVisual = root.AddComponent<DigAgentEquipmentVisual>();
            }

            _equipmentVisual.Configure(
                itemId,
                EquipmentAppearanceKind.Generic,
                _equipmentMaterial);
        }
    }
}
