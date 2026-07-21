using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dig.Domain.Agents;
using Dig.Domain.Jobs;
using Dig.Domain.World;

namespace Dig.Presentation.Agents
{

public enum ResidentNeedBand
{
    Critical = 0,
    Warning = 1,
    Healthy = 2,
}

public enum ResidentMoodFace
{
    Sad = 0,
    Neutral = 1,
    Joy = 2,
}

public enum ResidentSexIndicator
{
    Unknown = 0,
    Female = 1,
    Male = 2,
}

public enum ResidentActivityKind
{
    FreeTime = 0,
    Move = 1,
    Attack = 2,
    Cook = 3,
    UnpackBuilding = 4,
    PackBuilding = 5,
    Dig = 6,
    Craft = 7,
    Pickup = 8,
    Service = 9,
    Train = 10,
    Study = 11,
    Logistics = 12,
    Eat = 13,
    Sleep = 14,
    Rest = 15,
    Flee = 16,
    Idle = 17,
    Work = 18,
    Blocked = 19,
}

public readonly struct ResidentLocalizationArgument
{
    public ResidentLocalizationArgument(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Localization argument key is required.", nameof(key));
        }

        Key = key.Trim();
        Value = value ?? string.Empty;
    }

    public string Key { get; }
    public string Value { get; }
}

public sealed class ResidentActivityDescriptor
{
    public ResidentActivityDescriptor(
        ResidentActivityKind kind,
        string actorId,
        string localizationKey,
        string? subjectId = null,
        CellId? destination = null,
        string? sourceJobId = null,
        AgentIntentKind? sourceIntent = null,
        string? sourceOrderId = null,
        JobStageKind? sourceJobStage = null,
        int progressCurrent = 0,
        int progressMaximum = 0,
        string? blockReasonCode = null,
        IEnumerable<ResidentLocalizationArgument>? localizationArguments = null)
    {
        if (!Enum.IsDefined(typeof(ResidentActivityKind), kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        if (string.IsNullOrWhiteSpace(actorId))
        {
            throw new ArgumentException("Activity actor id is required.", nameof(actorId));
        }

        if (string.IsNullOrWhiteSpace(localizationKey))
        {
            throw new ArgumentException("Activity localization key is required.", nameof(localizationKey));
        }

        if (progressCurrent < 0 || progressMaximum < 0 || progressCurrent > progressMaximum)
        {
            throw new ArgumentOutOfRangeException(nameof(progressCurrent));
        }

        Kind = kind;
        ActorId = actorId.Trim();
        SubjectId = Normalize(subjectId);
        Destination = destination;
        SourceJobId = Normalize(sourceJobId);
        SourceIntent = sourceIntent;
        SourceOrderId = Normalize(sourceOrderId);
        SourceJobStage = sourceJobStage;
        ProgressCurrent = progressCurrent;
        ProgressMaximum = progressMaximum;
        BlockReasonCode = Normalize(blockReasonCode);
        LocalizationKey = localizationKey.Trim();
        LocalizationArguments = new ReadOnlyCollection<ResidentLocalizationArgument>(
            (localizationArguments ?? Array.Empty<ResidentLocalizationArgument>()).ToArray());
    }

    public ResidentActivityKind Kind { get; }
    public string ActorId { get; }
    public string? SubjectId { get; }
    public CellId? Destination { get; }
    public string? SourceJobId { get; }
    public AgentIntentKind? SourceIntent { get; }
    public string? SourceOrderId { get; }
    public JobStageKind? SourceJobStage { get; }
    public int ProgressCurrent { get; }
    public int ProgressMaximum { get; }
    public string? BlockReasonCode { get; }
    public string LocalizationKey { get; }
    public IReadOnlyList<ResidentLocalizationArgument> LocalizationArguments { get; }

    public double Progress => ProgressMaximum == 0
        ? 0d
        : Math.Min(1d, (double)ProgressCurrent / ProgressMaximum);

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

public readonly struct ResidentNeedViewModel
{
    public ResidentNeedViewModel(int value, ResidentNeedBand band, string accessibilityKey)
    {
        if (value < NeedValue.Minimum || value > NeedValue.Maximum)
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        if (string.IsNullOrWhiteSpace(accessibilityKey))
        {
            throw new ArgumentException("Need accessibility key is required.", nameof(accessibilityKey));
        }

        Value = value;
        Band = band;
        AccessibilityKey = accessibilityKey.Trim();
    }

    public int Value { get; }
    public ResidentNeedBand Band { get; }
    public string AccessibilityKey { get; }
    public int Percent => Value / 100;
}

public readonly struct ResidentSkillViewModel
{
    public ResidentSkillViewModel(string skillId, int level)
    {
        if (string.IsNullOrWhiteSpace(skillId))
        {
            throw new ArgumentException("Skill id is required.", nameof(skillId));
        }

        if (level < 0 || level > 10_000)
        {
            throw new ArgumentOutOfRangeException(nameof(level));
        }

        SkillId = skillId.Trim();
        Level = level;
    }

    public string SkillId { get; }
    public int Level { get; }
}

public sealed partial class ResidentSkillSetViewModel
{
    public ResidentSkillSetViewModel(IEnumerable<ResidentSkillViewModel> skills)
        : this(
            skills,
            AgentSkillCatalog.BaseCapacityUnits,
            AgentSkillCatalog.PrecisionVersion,
            lastReport: null)
    {
    }

    public IReadOnlyList<ResidentSkillViewModel> All { get; }
    public IReadOnlyList<ResidentSkillViewModel> TopFive { get; }
}

public sealed class ResidentRosterRowViewModel
{
    public ResidentRosterRowViewModel(
        string id,
        string name,
        long version,
        bool isAlive,
        bool isExpanded,
        ResidentSexIndicator sex,
        string sexAccessibilityKey,
        ScheduleActivity scheduledActivity,
        ResidentMoodFace moodFace,
        ResidentNeedViewModel health,
        ResidentNeedViewModel nutrition,
        ResidentNeedViewModel alertness,
        ResidentNeedViewModel mood,
        ResidentActivityDescriptor activity,
        bool isIdleAtWork,
        ResidentSkillSetViewModel skills)
    {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Resident id and name are required.");
        }

        if (version < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version));
        }

        if (string.IsNullOrWhiteSpace(sexAccessibilityKey))
        {
            throw new ArgumentException("Sex accessibility key is required.", nameof(sexAccessibilityKey));
        }

        Id = id.Trim();
        Name = name.Trim();
        Version = version;
        IsAlive = isAlive;
        IsExpanded = isExpanded;
        Sex = sex;
        SexAccessibilityKey = sexAccessibilityKey.Trim();
        ScheduledActivity = scheduledActivity;
        MoodFace = moodFace;
        Health = health;
        Nutrition = nutrition;
        Alertness = alertness;
        Mood = mood;
        Activity = activity ?? throw new ArgumentNullException(nameof(activity));
        IsIdleAtWork = isIdleAtWork;
        Skills = skills ?? throw new ArgumentNullException(nameof(skills));
    }

    public string Id { get; }
    public string Name { get; }
    public long Version { get; }
    public bool IsAlive { get; }
    public bool IsExpanded { get; }
    public ResidentSexIndicator Sex { get; }
    public string SexAccessibilityKey { get; }
    public ScheduleActivity ScheduledActivity { get; }
    public ResidentMoodFace MoodFace { get; }
    public ResidentNeedViewModel Health { get; }
    public ResidentNeedViewModel Nutrition { get; }
    public ResidentNeedViewModel Alertness { get; }
    public ResidentNeedViewModel Mood { get; }
    public ResidentActivityDescriptor Activity { get; }
    public bool IsIdleAtWork { get; }
    public ResidentSkillSetViewModel Skills { get; }
}

public sealed class ResidentRosterViewModel
{
    public ResidentRosterViewModel(
        IEnumerable<ResidentRosterRowViewModel> rows,
        string? selectedResidentId)
    {
        if (rows is null)
        {
            throw new ArgumentNullException(nameof(rows));
        }

        Rows = new ReadOnlyCollection<ResidentRosterRowViewModel>(rows.ToArray());
        SelectedResidentId = string.IsNullOrWhiteSpace(selectedResidentId)
            ? null
            : selectedResidentId.Trim();
    }

    public IReadOnlyList<ResidentRosterRowViewModel> Rows { get; }
    public string? SelectedResidentId { get; }

    public IReadOnlyList<ResidentRosterRowViewModel> GetWindow(int offset, int count)
    {
        if (offset < 0 || count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }

        if (offset >= Rows.Count || count == 0)
        {
            return Array.Empty<ResidentRosterRowViewModel>();
        }

        int actualCount = Math.Min(count, Rows.Count - offset);
        ResidentRosterRowViewModel[] window = new ResidentRosterRowViewModel[actualCount];
        for (int index = 0; index < actualCount; index++)
        {
            window[index] = Rows[offset + index];
        }

        return new ReadOnlyCollection<ResidentRosterRowViewModel>(window);
    }
}
}
