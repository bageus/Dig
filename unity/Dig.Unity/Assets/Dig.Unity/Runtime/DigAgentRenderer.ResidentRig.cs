using System;
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
        GameObject? root = null;
        try
        {
            root = new GameObject($"Resident {model.Name}");
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
            AttachEquipmentSafely(agentVisual, model.Id);
            _agents.Add(model.Id, agentVisual);
        }
        catch (Exception exception)
        {
            if (root != null)
            {
                Destroy(root);
            }

            Debug.LogException(exception, this);
            CreatePrimitiveResidentAgent(model);
        }
    }

    private void CreatePrimitiveResidentAgent(AgentViewModel model)
    {
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visual.name = $"Resident {model.Name} (Fallback)";
        visual.transform.SetParent(_visualRoot, worldPositionStays: false);
        visual.transform.localScale = new Vector3(0.48f, 0.62f, 0.48f);
        Renderer renderer = visual.GetComponent<Renderer>();
        renderer.sharedMaterial = _normalMaterial;
        DigAgentVisual agentVisual = visual.AddComponent<DigAgentVisual>();
        agentVisual.InitializeSimple(model, _normalMaterial!, _selectedMaterial!);
        agentVisual.SetSelected(_selectedIds.Contains(model.Id));
        AttachEquipmentSafely(agentVisual, model.Id);
        _agents.Add(model.Id, agentVisual);
    }

    private void AttachEquipmentSafely(DigAgentVisual visual, string residentId)
    {
        try
        {
            _equipment.TryGetValue(residentId, out ResidentEquipmentViewModel? equipment);
            visual.SetEquipment(equipment, _equipmentMaterial!);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
        }
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