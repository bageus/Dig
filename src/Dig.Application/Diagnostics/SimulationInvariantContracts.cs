using System.Collections.ObjectModel;
using Dig.Domain.Core;

namespace Dig.Application.Diagnostics;

public sealed class SimulationInvariantViolation
{
    public SimulationInvariantViolation(
        string code,
        string detail,
        EntityId? entityId = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Invariant code is required.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(detail))
        {
            throw new ArgumentException("Invariant detail is required.", nameof(detail));
        }

        Code = code.Trim();
        Detail = detail.Trim();
        EntityId = entityId;
    }

    public string Code { get; }

    public string Detail { get; }

    public EntityId? EntityId { get; }
}

public sealed class SimulationInvariantReport
{
    public SimulationInvariantReport(
        long tick,
        IReadOnlyCollection<SimulationInvariantViolation> violations)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        Tick = tick;
        Violations = new ReadOnlyCollection<SimulationInvariantViolation>(violations
            .OrderBy(value => value.Code, StringComparer.Ordinal)
            .ThenBy(value => value.EntityId?.ToString(), StringComparer.Ordinal)
            .ThenBy(value => value.Detail, StringComparer.Ordinal)
            .ToArray());
    }

    public long Tick { get; }

    public IReadOnlyList<SimulationInvariantViolation> Violations { get; }

    public bool IsValid => Violations.Count == 0;

    public void ThrowIfInvalid()
    {
        if (IsValid)
        {
            return;
        }

        string details = string.Join(
            Environment.NewLine,
            Violations.Select(value =>
                $"[{value.Code}] {value.EntityId?.ToString() ?? "global"}: {value.Detail}"));
        throw new InvalidOperationException(
            $"Simulation invariants failed at tick {Tick}:{Environment.NewLine}{details}");
    }
}
