using System;
using System.Collections.Generic;
using Dig.Presentation.Agents;

namespace Dig.Unity
{
    public sealed partial class DigAgentRenderer
    {
        internal void RestoreSelection(
            IReadOnlyList<string> agentIds,
            string? primaryAgentId)
        {
            if (agentIds == null)
            {
                throw new ArgumentNullException(nameof(agentIds));
            }

            _selectedIds.Clear();
            _selectionOrder.Clear();
            for (int index = 0; index < agentIds.Count; index++)
            {
                string id = agentIds[index];
                if (_agents.TryGetValue(id, out DigAgentVisual? visual)
                    && visual.Model.IsAlive
                    && _selectedIds.Add(id))
                {
                    _selectionOrder.Add(id);
                }
            }

            foreach (KeyValuePair<string, DigAgentVisual> pair in _agents)
            {
                pair.Value.SetSelected(_selectedIds.Contains(pair.Key));
            }

            _primarySelected = null;
            if (primaryAgentId != null
                && _selectedIds.Contains(primaryAgentId)
                && _agents.TryGetValue(primaryAgentId, out DigAgentVisual? primary))
            {
                _primarySelected = primary;
            }
            else
            {
                ResolvePrimarySelection();
            }

            PublishSelectionSnapshot();
        }

        internal IReadOnlyList<AgentViewModel> GetSelectedModels()
        {
            List<AgentViewModel> models = new List<AgentViewModel>(_selectionOrder.Count);
            for (int index = 0; index < _selectionOrder.Count; index++)
            {
                if (_agents.TryGetValue(
                    _selectionOrder[index],
                    out DigAgentVisual? visual)
                    && visual.Model.IsAlive)
                {
                    models.Add(visual.Model);
                }
            }

            return models;
        }
    }
}
