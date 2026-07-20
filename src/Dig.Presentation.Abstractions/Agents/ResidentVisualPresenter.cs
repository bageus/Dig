using System;

namespace Dig.Presentation.Agents
{

public sealed class ResidentVisualPresenter
{
    private const ulong OffsetBasis = 14695981039346656037UL;
    private const ulong Prime = 1099511628211UL;

    public ResidentAppearanceViewModel PresentAppearance(
        string residentId,
        ResidentBodyVariant bodyVariant = ResidentBodyVariant.Neutral,
        ResidentAgeVisualBand ageBand = ResidentAgeVisualBand.Adult,
        ResidentHeadwearRole headwearRole = ResidentHeadwearRole.None)
    {
        if (string.IsNullOrWhiteSpace(residentId))
        {
            throw new ArgumentException("Resident id is required.", nameof(residentId));
        }

        string id = residentId.Trim();
        ulong hash = Hash(id, (int)bodyVariant, (int)ageBand, (int)headwearRole);
        int clothing = (int)(hash % 8UL);
        int hairPalette = (int)((hash / 8UL) % 6UL);
        int face = (int)((hash / 48UL) % 4UL);
        ResidentHairVisualVariant hair = ageBand == ResidentAgeVisualBand.Old
            ? (hash % 3UL == 0UL
                ? ResidentHairVisualVariant.Bald
                : ResidentHairVisualVariant.OldSparse)
            : (ResidentHairVisualVariant)(1 + ((hash / 192UL) % 3UL));
        long version = ToVersion(Hash(id, (int)bodyVariant, (int)ageBand,
            (int)headwearRole, clothing, hairPalette, face, (int)hair));
        return new ResidentAppearanceViewModel(id, bodyVariant, ageBand, hair,
            headwearRole, clothing, hairPalette, face, version);
    }

    public ResidentActionVisualViewModel PresentAction(
        AgentViewModel model,
        bool isMoving,
        bool isCarrying,
        bool showImpact = false)
    {
        if (model is null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        ResidentActionVisualState state = ResolveAction(model, isMoving, isCarrying, showImpact);
        double progress = state == ResidentActionVisualState.Death ? 1d : model.ActionProgress;
        bool looping = state == ResidentActionVisualState.Idle
            || state == ResidentActionVisualState.Walk
            || state == ResidentActionVisualState.Dig
            || state == ResidentActionVisualState.Carry
            || state == ResidentActionVisualState.Build;
        long version = ToVersion(Hash(model.Id, (int)state,
            (int)Math.Round(progress * 1000d), looping ? 1 : 0, model.Version));
        return new ResidentActionVisualViewModel(model.Id, state, progress, looping, version);
    }

    private static ResidentActionVisualState ResolveAction(
        AgentViewModel model,
        bool isMoving,
        bool isCarrying,
        bool showImpact)
    {
        if (!model.IsAlive) return ResidentActionVisualState.Death;
        if (showImpact) return ResidentActionVisualState.Hit;
        if (isMoving) return ResidentActionVisualState.Walk;
        string intent = (model.ActiveIntent ?? string.Empty).Trim().ToLowerInvariant();
        if (ContainsAny(intent, "dig", "excavat", "mine"))
            return ResidentActionVisualState.Dig;
        if (ContainsAny(intent, "build", "construct", "assemble"))
            return ResidentActionVisualState.Build;
        if (ContainsAny(intent, "pickup", "pick up", "collect"))
            return ResidentActionVisualState.Pickup;
        if (ContainsAny(intent, "drop", "store", "deliver"))
            return ResidentActionVisualState.Drop;
        return isCarrying ? ResidentActionVisualState.Carry : ResidentActionVisualState.Idle;
    }

    private static bool ContainsAny(string value, params string[] fragments)
    {
        for (int index = 0; index < fragments.Length; index++)
        {
            if (value.Contains(fragments[index], StringComparison.Ordinal)) return true;
        }
        return false;
    }

    private static ulong Hash(string value, params long[] numbers)
    {
        ulong hash = OffsetBasis;
        for (int index = 0; index < value.Length; index++)
        {
            hash ^= value[index];
            hash *= Prime;
        }
        for (int index = 0; index < numbers.Length; index++)
        {
            ulong number = unchecked((ulong)numbers[index]);
            for (int shift = 0; shift < 64; shift += 8)
            {
                hash ^= (byte)(number >> shift);
                hash *= Prime;
            }
        }
        return hash;
    }

    private static long ToVersion(ulong value)
    {
        return (long)(value & (ulong)long.MaxValue);
    }
}
}
