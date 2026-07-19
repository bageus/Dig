using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;

namespace Dig.Domain.Agents
{

public sealed class AgentDecisionContext
{
    public AgentDecisionContext(
        bool foodAvailable,
        bool bedAvailable,
        bool workAvailable,
        bool restAvailable,
        bool escapeRouteAvailable,
        int threatLevel,
        double travelCostMultiplier = 1d)
    {
        if (threatLevel < NeedValue.Minimum || threatLevel > NeedValue.Maximum)
        {
            throw new ArgumentOutOfRangeException(nameof(threatLevel));
        }

        if (travelCostMultiplier < 1d
            || double.IsNaN(travelCostMultiplier)
            || double.IsInfinity(travelCostMultiplier))
        {
            throw new ArgumentOutOfRangeException(nameof(travelCostMultiplier));
        }

        FoodAvailable = foodAvailable;
        BedAvailable = bedAvailable;
        WorkAvailable = workAvailable;
        RestAvailable = restAvailable;
        EscapeRouteAvailable = escapeRouteAvailable;
        ThreatLevel = threatLevel;
        TravelCostMultiplier = travelCostMultiplier;
    }

    public bool FoodAvailable { get; }

    public bool BedAvailable { get; }

    public bool WorkAvailable { get; }

    public bool RestAvailable { get; }

    public bool EscapeRouteAvailable { get; }

    public int ThreatLevel { get; }

    public double TravelCostMultiplier { get; }

    public AgentDecisionContext WithTravelCostMultiplier(double multiplier)
    {
        return Math.Abs(multiplier - TravelCostMultiplier) < 0.0000001d
            ? this
            : new AgentDecisionContext(
                FoodAvailable,
                BedAvailable,
                WorkAvailable,
                RestAvailable,
                EscapeRouteAvailable,
                ThreatLevel,
                multiplier);
    }

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

public readonly struct UtilityOptionDiagnostic
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
    private string? _explanation;

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
        Validate(tick, reasonCode, options);
        if (string.IsNullOrWhiteSpace(explanation))
        {
            throw new ArgumentException("Explanation is required.", nameof(explanation));
        }

        Tick = tick;
        SelectedIntent = selectedIntent;
        SelectedPlayerOrderId = selectedPlayerOrderId;
        SelectedScore = selectedScore;
        Critical = critical;
        ReasonCode = reasonCode.Trim();
        _explanation = explanation.Trim();
        Options = new ReadOnlyCollection<UtilityOptionDiagnostic>(options.ToArray());
    }

    private AgentDecision(
        long tick,
        AgentIntentKind selectedIntent,
        string? selectedPlayerOrderId,
        int selectedScore,
        bool critical,
        string reasonCode,
        UtilityOptionDiagnostic[] ownedOptions)
    {
        Validate(tick, reasonCode, ownedOptions);
        Tick = tick;
        SelectedIntent = selectedIntent;
        SelectedPlayerOrderId = selectedPlayerOrderId;
        SelectedScore = selectedScore;
        Critical = critical;
        ReasonCode = reasonCode;
        Options = new ReadOnlyCollection<UtilityOptionDiagnostic>(ownedOptions);
    }

    public long Tick { get; }

    public AgentIntentKind SelectedIntent { get; }

    public string? SelectedPlayerOrderId { get; }

    public int SelectedScore { get; }

    public bool Critical { get; }

    public string ReasonCode { get; }

    public string Explanation => _explanation ??= BuildExplanation();

    public IReadOnlyList<UtilityOptionDiagnostic> Options { get; }

    internal static AgentDecision CreateOwned(
        long tick,
        AgentIntentKind selectedIntent,
        string? selectedPlayerOrderId,
        int selectedScore,
        bool critical,
        string reasonCode,
        UtilityOptionDiagnostic[] ownedOptions)
    {
        return new AgentDecision(
            tick,
            selectedIntent,
            selectedPlayerOrderId,
            selectedScore,
            critical,
            reasonCode,
            ownedOptions);
    }

    private string BuildExplanation()
    {
        return $"{SelectedIntent} selected with score {SelectedScore} ({ReasonCode}).";
    }

    private static void Validate(
        long tick,
        string reasonCode,
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

        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }
    }
}

}