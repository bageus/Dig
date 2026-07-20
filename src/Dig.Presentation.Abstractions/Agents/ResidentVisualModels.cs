using System;

namespace Dig.Presentation.Agents
{

public enum ResidentBodyVariant
{
    Neutral = 0,
    Masculine = 1,
    Feminine = 2,
}

public enum ResidentAgeVisualBand
{
    Young = 0,
    Adult = 1,
    Old = 2,
}

public enum ResidentHairVisualVariant
{
    Bald = 0,
    Short = 1,
    Braided = 2,
    Long = 3,
    OldSparse = 4,
}

public enum ResidentHeadwearRole
{
    None = 0,
    Worker = 1,
    Miner = 2,
    Builder = 3,
    Hauler = 4,
}

public enum ResidentActionVisualState
{
    Idle = 0,
    Walk = 1,
    Dig = 2,
    Carry = 3,
    Build = 4,
    Pickup = 5,
    Drop = 6,
    Hit = 7,
    Death = 8,
}

public sealed class ResidentAppearanceViewModel
{
    public ResidentAppearanceViewModel(string residentId, ResidentBodyVariant bodyVariant,
        ResidentAgeVisualBand ageBand, ResidentHairVisualVariant hairVariant,
        ResidentHeadwearRole headwearRole, int clothingPaletteIndex,
        int hairPaletteIndex, int faceVariant, long version)
    {
        if (string.IsNullOrWhiteSpace(residentId))
        {
            throw new ArgumentException("Resident id is required.", nameof(residentId));
        }

        if (!Enum.IsDefined(typeof(ResidentBodyVariant), bodyVariant)
            || !Enum.IsDefined(typeof(ResidentAgeVisualBand), ageBand)
            || !Enum.IsDefined(typeof(ResidentHairVisualVariant), hairVariant)
            || !Enum.IsDefined(typeof(ResidentHeadwearRole), headwearRole)
            || clothingPaletteIndex < 0 || clothingPaletteIndex > 7
            || hairPaletteIndex < 0 || hairPaletteIndex > 5
            || faceVariant < 0 || faceVariant > 3 || version < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bodyVariant));
        }

        ResidentId = residentId.Trim();
        BodyVariant = bodyVariant;
        AgeBand = ageBand;
        HairVariant = hairVariant;
        HeadwearRole = headwearRole;
        ClothingPaletteIndex = clothingPaletteIndex;
        HairPaletteIndex = hairPaletteIndex;
        FaceVariant = faceVariant;
        Version = version;
    }

    public string ResidentId { get; }
    public ResidentBodyVariant BodyVariant { get; }
    public ResidentAgeVisualBand AgeBand { get; }
    public ResidentHairVisualVariant HairVariant { get; }
    public ResidentHeadwearRole HeadwearRole { get; }
    public int ClothingPaletteIndex { get; }
    public int HairPaletteIndex { get; }
    public int FaceVariant { get; }
    public long Version { get; }
}

public sealed class ResidentActionVisualViewModel
{
    public ResidentActionVisualViewModel(string residentId,
        ResidentActionVisualState state, double normalizedProgress,
        bool isLooping, long version)
    {
        if (string.IsNullOrWhiteSpace(residentId))
        {
            throw new ArgumentException("Resident id is required.", nameof(residentId));
        }

        if (!Enum.IsDefined(typeof(ResidentActionVisualState), state)
            || normalizedProgress < 0d || normalizedProgress > 1d || version < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(state));
        }

        ResidentId = residentId.Trim();
        State = state;
        NormalizedProgress = normalizedProgress;
        IsLooping = isLooping;
        Version = version;
    }

    public string ResidentId { get; }
    public ResidentActionVisualState State { get; }
    public double NormalizedProgress { get; }
    public bool IsLooping { get; }
    public long Version { get; }
}
}
