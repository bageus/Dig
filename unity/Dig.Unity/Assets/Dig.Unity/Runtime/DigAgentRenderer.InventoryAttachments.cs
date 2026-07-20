using System;
using System.Collections.Generic;
using System.Linq;
using Dig.Domain.Inventory;
using Dig.Presentation.Inventory;
using UnityEngine;

namespace Dig.Unity
{

public sealed partial class DigAgentRenderer
{
    private readonly Dictionary<string, DigResidentInventoryAttachmentVisual>
        _inventoryAttachmentVisuals =
            new Dictionary<string, DigResidentInventoryAttachmentVisual>(
                StringComparer.Ordinal);

    internal void RenderInventoryAttachments(
        IReadOnlyList<ResidentInventoryAttachmentViewModel> attachments)
    {
        if (attachments == null)
        {
            throw new ArgumentNullException(nameof(attachments));
        }

        ResidentInventoryAttachmentViewModel[] ordered = attachments
            .OrderBy(model => model.ResidentId, StringComparer.Ordinal)
            .ThenBy(model => model.Group)
            .ToArray();
        if (ordered.GroupBy(model => AttachmentKey(model.ResidentId, model.Group))
            .Any(group => group.Count() > 1))
        {
            throw new InvalidOperationException(
                "A resident can have only one active attachment per expansion group.");
        }

        Dictionary<string, ResidentInventoryAttachmentViewModel> models = ordered
            .ToDictionary(
                model => AttachmentKey(model.ResidentId, model.Group),
                StringComparer.Ordinal);
        RemoveDestroyedAttachmentEntries();
        foreach (KeyValuePair<string, DigAgentVisual> agent in _agents)
        {
            ApplyAttachment(
                agent.Key,
                agent.Value,
                InventoryExpansionGroup.Cargo,
                models);
            ApplyAttachment(
                agent.Key,
                agent.Value,
                InventoryExpansionGroup.Weapon,
                models);
        }
    }

    private void ApplyAttachment(
        string residentId,
        DigAgentVisual agent,
        InventoryExpansionGroup group,
        IReadOnlyDictionary<string, ResidentInventoryAttachmentViewModel> models)
    {
        string key = AttachmentKey(residentId, group);
        if (!models.TryGetValue(key, out ResidentInventoryAttachmentViewModel? model))
        {
            if (_inventoryAttachmentVisuals.TryGetValue(
                    key,
                    out DigResidentInventoryAttachmentVisual? hidden))
            {
                hidden.Clear();
                hidden.gameObject.SetActive(false);
            }

            return;
        }

        if (!_inventoryAttachmentVisuals.TryGetValue(
                key,
                out DigResidentInventoryAttachmentVisual? visual)
            || visual == null)
        {
            GameObject root = new GameObject(group + " Inventory Attachment");
            root.transform.SetParent(agent.transform, worldPositionStays: false);
            visual = root.AddComponent<DigResidentInventoryAttachmentVisual>();
            _inventoryAttachmentVisuals[key] = visual;
        }

        visual.gameObject.SetActive(true);
        visual.Configure(model, ResolveItemVisual(model.ItemId));
    }

    private void RemoveDestroyedAttachmentEntries()
    {
        string[] stale = _inventoryAttachmentVisuals
            .Where(pair => pair.Value == null)
            .Select(pair => pair.Key)
            .ToArray();
        for (int index = 0; index < stale.Length; index++)
        {
            _inventoryAttachmentVisuals.Remove(stale[index]);
        }
    }

    private static string AttachmentKey(
        string residentId,
        InventoryExpansionGroup group)
    {
        return residentId + ":" + group;
    }
}
}
