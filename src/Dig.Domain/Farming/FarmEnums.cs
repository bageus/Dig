namespace Dig.Domain.Farming
{
    public enum FarmMode : byte
    {
        None = 0,
        Mushrooms = 1,
        Hamsters = 2,
        Grub = 3
    }

    public enum FarmDeliveryKind : byte
    {
        MushroomSeed = 1,
        MushroomFeed = 2,
        Hamster = 3,
        Grub = 4
    }
}
