using Dig.Presentation.Agents;
using Dig.Presentation.Inventory;
using UnityEngine;

namespace Dig.Unity
{
public sealed partial class DigAgentRenderer
{
    private const string DefaultResidentVisualId = "resident.default";
    private readonly ResidentVisualPresenter _residentVisualPresenter = new ResidentVisualPresenter();
    private DigResidentVisualCatalog? _residentVisualCatalog;

    private void CreateResidentAgent(AgentViewModel model)
    {
        GameObject root = new GameObject($"Resident {model.Name}");
        root.transform.SetParent(_visualRoot, false);
        CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
        collider.center = new Vector3(0f, 0.74f, 0f);
        collider.height = 1.52f;
        collider.radius = 0.34f;

        DigResidentVisualResolution resolution = ResolveResidentVisual();
        ResidentAppearanceViewModel appearance = _residentVisualPresenter.PresentAppearance(
            model.Id, resolution.BodyVariant);
        DigResidentRig rig = DigResidentRigFactory.Create(root.transform,
            resolution.Asset, _normalMaterial!, appearance,
            resolution.MaximumRenderers);
        rig.transform.localScale = resolution.Scale;
        DigAgentVisual agentVisual = root.AddComponent<DigAgentVisual>();
        agentVisual.Initialize(model, _normalMaterial!, _selectedMaterial!, rig, appearance);
        agentVisual.SetSelected(_selectedIds.Contains(model.Id));
        _equipment.TryGetValue(model.Id, out ResidentEquipmentViewModel? equipment);
        agentVisual.SetEquipment(equipment, _equipmentMaterial!);
        _agents.Add(model.Id, agentVisual);
    }

    private DigResidentVisualResolution ResolveResidentVisual()
    {
        if (_residentVisualCatalog == null)
            _residentVisualCatalog = Resources.Load<DigResidentVisualCatalog>(
                "VisualCatalogs/Residents");
        if (_residentVisualCatalog != null)
            return _residentVisualCatalog.ResolveResident(DefaultResidentVisualId);
        return new DigResidentVisualResolution(
            DigVisualAsset.CreateRuntimeFallback(DefaultResidentVisualId,
                new Color(0.25f, 0.72f, 0.82f, 1f)),
            ResidentBodyVariant.Neutral, Vector3.one, 12, hasProfile: false);
    }
}
}