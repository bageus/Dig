using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Dig.Application.Jobs
{

public readonly struct ExcavationCadenceRatio
{
    public ExcavationCadenceRatio(int numerator, int denominator)
    {
        if (numerator <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(numerator));
        }

        if (denominator <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(denominator));
        }

        Numerator = numerator;
        Denominator = denominator;
    }

    public int Numerator { get; }
    public int Denominator { get; }
}

public sealed class ExcavationSkillCadenceBand
{
    public ExcavationSkillCadenceBand(
        int minimumSkill,
        int maximumSkill,
        ExcavationCadenceRatio intervalRatio)
    {
        if (minimumSkill < 0 || maximumSkill > 100 || minimumSkill > maximumSkill)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumSkill));
        }

        MinimumSkill = minimumSkill;
        MaximumSkill = maximumSkill;
        IntervalRatio = intervalRatio;
    }

    public int MinimumSkill { get; }
    public int MaximumSkill { get; }
    public ExcavationCadenceRatio IntervalRatio { get; }

    public bool Contains(int skill) => skill >= MinimumSkill && skill <= MaximumSkill;
}

public enum ExcavationFlavorCue
{
    None = 0,
    ClumsySwing = 1,
}

public sealed class ExcavationCadenceProfile
{
    private readonly IReadOnlyList<ExcavationSkillCadenceBand> _skillBands;
    private readonly IReadOnlyDictionary<TerrainWorkPosture, ExcavationCadenceRatio>
        _postureRatios;

    public ExcavationCadenceProfile(
        int referenceHardness,
        IEnumerable<ExcavationSkillCadenceBand> skillBands,
        IReadOnlyDictionary<TerrainWorkPosture, ExcavationCadenceRatio> postureRatios,
        int lowSkillFlavorIntervalTicks = 0)
    {
        if (referenceHardness <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(referenceHardness));
        }

        if (skillBands == null)
        {
            throw new ArgumentNullException(nameof(skillBands));
        }

        if (postureRatios == null)
        {
            throw new ArgumentNullException(nameof(postureRatios));
        }

        if (lowSkillFlavorIntervalTicks < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(lowSkillFlavorIntervalTicks));
        }

        ExcavationSkillCadenceBand[] ordered = skillBands
            .OrderBy(value => value.MinimumSkill)
            .ToArray();
        ValidateSkillBands(ordered);
        Dictionary<TerrainWorkPosture, ExcavationCadenceRatio> postures =
            new Dictionary<TerrainWorkPosture, ExcavationCadenceRatio>();
        foreach (TerrainWorkPosture posture in Enum.GetValues(typeof(TerrainWorkPosture)))
        {
            if (!postureRatios.TryGetValue(posture, out ExcavationCadenceRatio ratio))
            {
                throw new ArgumentException(
                    $"Missing excavation cadence posture '{posture}'.",
                    nameof(postureRatios));
            }

            postures.Add(posture, ratio);
        }

        ReferenceHardness = referenceHardness;
        _skillBands = new ReadOnlyCollection<ExcavationSkillCadenceBand>(ordered);
        _postureRatios = new ReadOnlyDictionary<TerrainWorkPosture, ExcavationCadenceRatio>(
            postures);
        LowSkillFlavorIntervalTicks = lowSkillFlavorIntervalTicks;
    }

    public int ReferenceHardness { get; }
    public IReadOnlyList<ExcavationSkillCadenceBand> SkillBands => _skillBands;
    public int LowSkillFlavorIntervalTicks { get; }

    public ExcavationSkillCadenceBand ResolveSkillBand(int skill)
    {
        if (skill < 0 || skill > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(skill));
        }

        return _skillBands.Single(value => value.Contains(skill));
    }

    public ExcavationCadenceRatio ResolvePosture(TerrainWorkPosture posture)
    {
        if (!Enum.IsDefined(typeof(TerrainWorkPosture), posture))
        {
            throw new ArgumentOutOfRangeException(nameof(posture));
        }

        return _postureRatios[posture];
    }

    public static ExcavationCadenceProfile CreateLegacyDeterministic()
    {
        ExcavationCadenceRatio neutral = new ExcavationCadenceRatio(1, 1);
        return new ExcavationCadenceProfile(
            referenceHardness: 120,
            skillBands: new[]
            {
                new ExcavationSkillCadenceBand(0, 10, new ExcavationCadenceRatio(3, 1)),
                new ExcavationSkillCadenceBand(11, 20, new ExcavationCadenceRatio(2, 1)),
                new ExcavationSkillCadenceBand(21, 50, neutral),
                new ExcavationSkillCadenceBand(51, 70, new ExcavationCadenceRatio(1, 2)),
                new ExcavationSkillCadenceBand(71, 100, new ExcavationCadenceRatio(1, 3)),
            },
            postureRatios: new Dictionary<TerrainWorkPosture, ExcavationCadenceRatio>
            {
                [TerrainWorkPosture.Standing] = neutral,
                [TerrainWorkPosture.DepthBraced] = neutral,
                [TerrainWorkPosture.Climbing] = neutral,
            });
    }

    private static void ValidateSkillBands(IReadOnlyList<ExcavationSkillCadenceBand> bands)
    {
        if (bands.Count == 0 || bands[0].MinimumSkill != 0
            || bands[bands.Count - 1].MaximumSkill != 100)
        {
            throw new ArgumentException("Excavation skill bands must cover 0..100.");
        }

        for (int index = 1; index < bands.Count; index++)
        {
            if (bands[index - 1].MaximumSkill + 1 != bands[index].MinimumSkill)
            {
                throw new ArgumentException(
                    "Excavation skill bands must be contiguous and non-overlapping.");
            }
        }
    }
}

public readonly struct ExcavationCadenceDecision
{
    public ExcavationCadenceDecision(
        int intervalTicks,
        int materialHardness,
        int miningSkill,
        int equipmentIntervalTicks,
        TerrainWorkPosture posture,
        ExcavationCadenceRatio skillRatio,
        ExcavationCadenceRatio postureRatio,
        ExcavationFlavorCue flavorCue)
    {
        IntervalTicks = intervalTicks;
        MaterialHardness = materialHardness;
        MiningSkill = miningSkill;
        EquipmentIntervalTicks = equipmentIntervalTicks;
        Posture = posture;
        SkillRatio = skillRatio;
        PostureRatio = postureRatio;
        FlavorCue = flavorCue;
    }

    public int IntervalTicks { get; }
    public int MaterialHardness { get; }
    public int MiningSkill { get; }
    public int EquipmentIntervalTicks { get; }
    public TerrainWorkPosture Posture { get; }
    public ExcavationCadenceRatio SkillRatio { get; }
    public ExcavationCadenceRatio PostureRatio { get; }
    public ExcavationFlavorCue FlavorCue { get; }
}

public sealed class ExcavationCadenceResolver
{
    private readonly ExcavationCadenceProfile _profile;

    public ExcavationCadenceResolver(ExcavationCadenceProfile profile)
    {
        _profile = profile ?? throw new ArgumentNullException(nameof(profile));
    }

    public ExcavationCadenceDecision Resolve(
        int materialHardness,
        int miningSkill,
        int equipmentIntervalTicks,
        TerrainWorkPosture posture,
        long tick)
    {
        if (materialHardness <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(materialHardness));
        }

        if (miningSkill < 0 || miningSkill > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(miningSkill));
        }

        if (equipmentIntervalTicks <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(equipmentIntervalTicks));
        }

        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        ExcavationCadenceRatio skill = _profile.ResolveSkillBand(miningSkill).IntervalRatio;
        ExcavationCadenceRatio postureRatio = _profile.ResolvePosture(posture);
        long numerator = checked((long)equipmentIntervalTicks
            * materialHardness
            * skill.Numerator
            * postureRatio.Numerator);
        long denominator = checked((long)_profile.ReferenceHardness
            * skill.Denominator
            * postureRatio.Denominator);
        long interval = checked((numerator + denominator - 1L) / denominator);
        int intervalTicks = interval > int.MaxValue ? int.MaxValue : Math.Max(1, (int)interval);
        ExcavationFlavorCue cue = miningSkill <= 10
            && _profile.LowSkillFlavorIntervalTicks > 0
            && tick % _profile.LowSkillFlavorIntervalTicks == 0
                ? ExcavationFlavorCue.ClumsySwing
                : ExcavationFlavorCue.None;
        return new ExcavationCadenceDecision(
            intervalTicks,
            materialHardness,
            miningSkill,
            equipmentIntervalTicks,
            posture,
            skill,
            postureRatio,
            cue);
    }

    public static bool IsDue(long tick, ExcavationCadenceDecision decision)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        return tick % decision.IntervalTicks == 0;
    }
}

}
