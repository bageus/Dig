using Dig.Presentation.Agents;
using Dig.Presentation.Inventory;
using UnityEngine;

namespace Dig.Unity
{
public sealed partial class DigAgentRenderer
{
    private const string DefaultResidentVisualId = "resident.default";
    private readonly ResidentVisualPresenter _residentVisualPresenter =
        new ResidentVisualPresenter();
    private DigResidentVisualCatalog? _residentVisualCatalog;

    private void CreateResidentAgent(AgentViewModel model)
    {
        GameObject root = new GameObject($"Resident {model.Name}");
        root.transform.SetParent(_visualRoot, worldPositionStays: false);
        root.layer = 0;
        CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
        collider.center = new Vector3(0f, 0.74f, 0f);
        collider.height = 1.52f;
        collider.radius = 0.34f;
        collider.direction = 1;

        ResidentAppearanceViewModel appearance =
            _residentVisualPresenter.PresentAppearance(model.Id);
        DigVisualAsset asset = ResolveResidentAsset();
        DigResidentRig rig = DigResidentRigFactory.Create(
            root.transform,
            asset,
            _normalMaterial!,
            appearance);
        DigAgentVisual agentVisual = root.AddComponent<DigAgentVisual>();
        agentVisual.Initialize(
            model,
            _normalMaterial!,
            _selectedMaterial!,
            rig,
            appearance);
        agentVisual.SetSelected(_selectedIds.Contains(model.Id));
        _equipment.TryGetValue(model.Id, out ResidentEquipmentViewModel? equipment);
        agentVisual.SetEquipment(equipment, _equipmentMaterial!);
        _agents.Add(model.Id, agentVisual);
    }

    private DigVisualAsset ResolveResidentAsset()
    {
        if (_residentVisualCatalog == null)
        {
            _residentVisualCatalog = Resources.Load<DigResidentVisualCatalog>(
                "VisualCatalogs/Residents");
        }

        return _residentVisualCatalog == null
            ? DigVisualAsset.CreateRuntimeFallback(
                DefaultResidentVisualId,
                new Color(0.25f, 0.72f, 0.82f, 1f))
            : _residentVisualCatalog.Resolve(DefaultResidentVisualId);
    }
}
}
