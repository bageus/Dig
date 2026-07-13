using System.Collections.ObjectModel;

namespace Dig.Domain.Agents;

public sealed class AgentDecisionContext
{
    public AgentDecisionContext(
        bool foodAvailable,
        bool bedAvailable,
        bool workAvailable,
        bool restAvailable,
        bool escapeRouteAvailable,
        int threatLevel)
    {
        if (threatLevel < NeedValue.Minimum || threatLevel > NeedValue.Maximum)
        {
            throw new ArgumentOutOfRangeException(nameof(threatLevel));
        }

        FoodAvailable = foodAvailable;
        BedAvailable = bedAvailable;
        WorkAvailable = workAvailable;
        RestAvailable = restAvailable;
        EscapeRouteAvailable = escapeRouteAvailable;
        ThreatLevel = threatLevel;
    }

    public bool FoodAvailable { get; }

    public bool BedAvailable { get; }

    public bool WorkAvailable { get; }

    public bool RestAvailable { get; }

    public bool EscapeRouteAvailable { get; }

    public int ThreatLevel { get; }

    public static AgentDecisionContext AllAvailable(int threatLevel = 0)
    {
        return new AgentDecisionContext(
            foodAvailable: true,
            bedAvailable: true,
            workAvailable: true,
            restAvailable: true,
            escapeRouteAvailable: true,
            threatLevel);
    }
}

public sealed class UtilityOptionDiagnostic
{
    public UtilityOptionDiagnostic(
        AgentIntentKind intentKind,
        int baseScore,
        int finalScore,
        bool available,
        bool critical,
        bool selected,
        string reasonCode,
        string detail)
    {
        if (string.IsNullOrWhiteSpace(reasonCode))
        {
            throw new ArgumentException("Reason code is required.", nameof(reasonCode));
        }

        if (string.IsNullOrWhiteSpace(detail))
        {
            throw new ArgumentException("Decision detail is required.", nameof(detail));
        }

        IntentKind = intentKind;
        BaseScore = baseScore;
        FinalScore = finalScore;
        Available = available;
        Critical = critical;
        Selected = selected;
        ReasonCode = reasonCode;
        Detail = detail;
    }

    public AgentIntentKind IntentKind { get; }

    public int BaseScore { get; }

    public int FinalScore { get; }

    public bool Available { get; }

    public bool Critical { get; }

    public bool Selected { get; }

    public string ReasonCode { get; }

    public string Detail { get; }
}

public sealed class AgentDecision
{
    public AgentDecision(
        long tick,
        AgentIntentKind selectedIntent,
        string? selectedPlayerOrderId,
        int selectedScore,
        bool critical,
        string reasonCode,
        string explanation,
        IReadOnlyCollection<UtilityOptionDiagnostic> options)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        if (string.IsNullOrWhiteSpace(reasonCode))
        {
            throw new ArgumentException("Reason code is required.", nameof(reasonCode));
        }

        if (string.IsNullOrWhiteSpace(explanation))
        {
            throw new ArgumentException("Explanation is required.", nameof(explanation));
        }

        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        Tick = tick;
        SelectedIntent = selectedIntent;
        SelectedPlayerOrderId = selectedPlayerOrderId;
        SelectedScore = selectedScore;
        Critical = critical;
        ReasonCode = reasonCode;
        Explanation = explanation;
        Options = new ReadOnlyCollection<UtilityOptionDiagnostic>(options.ToArray());
    }

    public long Tick { get; }

    public AgentIntentKind SelectedIntent { get; }

    public string? SelectedPlayerOrderId { get; }

    public int SelectedScore { get; }

    public bool Critical { get; }

    public string ReasonCode { get; }

    public string Explanation { get; }

    public IReadOnlyList<UtilityOptionDiagnostic> Options { get; }
}
