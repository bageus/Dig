namespace Dig.Presentation.Creatures
{

public enum CreatureVisualFamily
{
    Plant = 0,
    Vuker = 1,
    Arachnid = 2,
    Biped = 3,
    LargeDemon = 4,
    SmallCreature = 5,
}

public enum CreatureLifecycleVisualStage
{
    Seed = 0,
    Egg = 1,
    Larva = 2,
    Child = 3,
    Adult = 4,
}

public enum CreatureDisposition
{
    Neutral = 0,
    Tamed = 1,
    Hostile = 2,
}

public enum CreatureActionVisualState
{
    Idle = 0,
    Move = 1,
    Attack = 2,
    Hit = 3,
    Death = 4,
    Growth = 5,
    Special = 6,
}

public enum CreatureMarkerShape
{
    Ring = 0,
    Shield = 1,
    Spikes = 2,
}

public enum CreatureLodTier
{
    Near = 0,
    Mid = 1,
    Far = 2,
    Hidden = 3,
}

public enum CreatureAnimationUpdatePolicy
{
    EveryFrame = 0,
    Reduced = 1,
    Frozen = 2,
}

}