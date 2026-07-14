using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using Dig.Domain.Buildings;

namespace Dig.Presentation.Buildings
{

public sealed class BuildingDiagnosticView
{
    public BuildingDiagnosticView(
        string id,
        string definition,
        string status,
        string origin,
        string workPosition,
        int completedWork,
        int requiredWork,
        int durability,
        int maximumDurability,
        IReadOnlyCollection<string> footprint,
        string? reason)
    {
        Id = id;
        Definition = definition;
        Status = status;
        Origin = origin;
        WorkPosition = workPosition;
        CompletedWork = completedWork;
        RequiredWork = requiredWork;
        Durability = durability;
        MaximumDurability = maximumDurability;
        Footprint = new ReadOnlyCollection<string>(footprint.ToArray());
        Reason = reason;
    }

    public string Id { get; }

    public string Definition { get; }

    public string Status { get; }

    public string Origin { get; }

    public string WorkPosition { get; }

    public int CompletedWork { get; }

    public int RequiredWork { get; }

    public int Durability { get; }

    public int MaximumDurability { get; }

    public IReadOnlyList<string> Footprint { get; }

    public string? Reason { get; }
}

public sealed class BuildingPresenter
{
    public BuildingDiagnosticView Present(BuildingSnapshot snapshot)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        return new BuildingDiagnosticView(
            snapshot.Id.ToString(),
            snapshot.Definition.Id.ToString(),
            snapshot.Status.ToString(),
            snapshot.Origin.ToString(),
            snapshot.WorkPosition.ToString(),
            snapshot.CompletedWork,
            snapshot.Definition.RequiredWork,
            snapshot.Durability,
            snapshot.Definition.MaximumDurability,
            snapshot.Footprint.Select(cell => cell.ToString()).ToArray(),
            snapshot.DiagnosticReason);
    }
}
}
