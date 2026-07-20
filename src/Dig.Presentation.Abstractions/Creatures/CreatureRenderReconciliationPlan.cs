using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Dig.Presentation.Creatures
{
public sealed class CreatureRenderReconciliationPlan
{
    private CreatureRenderReconciliationPlan(
        IReadOnlyList<string> createIds,
        IReadOnlyList<string> updateIds,
        IReadOnlyList<string> removeIds)
    {
        CreateIds = createIds;
        UpdateIds = updateIds;
        RemoveIds = removeIds;
    }

    public IReadOnlyList<string> CreateIds { get; }
    public IReadOnlyList<string> UpdateIds { get; }
    public IReadOnlyList<string> RemoveIds { get; }

    public static CreatureRenderReconciliationPlan Create(
        IReadOnlyCollection<string> renderedIds,
        IReadOnlyList<CreatureVisualSnapshot> snapshots,
        int populationCap)
    {
        if (renderedIds == null) throw new ArgumentNullException(nameof(renderedIds));
        if (snapshots == null) throw new ArgumentNullException(nameof(snapshots));
        if (populationCap < 1) throw new ArgumentOutOfRangeException(nameof(populationCap));
        if (snapshots.Count > populationCap)
            throw new InvalidOperationException("Creature visual population cap was exceeded.");

        HashSet<string> rendered = new HashSet<string>(renderedIds, StringComparer.Ordinal);
        HashSet<string> incoming = new HashSet<string>(StringComparer.Ordinal);
        List<string> creates = new List<string>();
        List<string> updates = new List<string>();
        for (int index = 0; index < snapshots.Count; index++)
        {
            string id = snapshots[index].CreatureId;
            if (!incoming.Add(id))
                throw new InvalidOperationException("Duplicate creature visual id: " + id);
            if (rendered.Contains(id)) updates.Add(id);
            else creates.Add(id);
        }

        List<string> removes = rendered
            .Where(id => !incoming.Contains(id))
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();
        creates.Sort(StringComparer.Ordinal);
        updates.Sort(StringComparer.Ordinal);
        return new CreatureRenderReconciliationPlan(
            new ReadOnlyCollection<string>(creates),
            new ReadOnlyCollection<string>(updates),
            new ReadOnlyCollection<string>(removes));
    }
}
}