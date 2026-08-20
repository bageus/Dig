namespace Dig.Domain.Farming
{
    public enum FarmOperationMode : byte
    {
        Idle = 0,
        MushroomCultivation = 1,
        HamsterBreeding = 2,
        GrubBreeding = 3
    }

    public enum FarmCycleStage : byte
    {
        AwaitingFeed = 0,
        InBreeding = 1,
        ReadyToHarvest = 2
    }

    public readonly record struct FarmBreedingRecipe(
        FarmOperationMode Mode,
        string RequiredFeedItemId,
        int RequiredFeedQuantity,
        int CycleDurationTicks,
        string OutputEntityId,
        int OutputQuantity);

    public static class FarmDefaultRecipes
    {
        public static readonly FarmBreedingRecipe MushroomRecipe = new(
            FarmOperationMode.MushroomCultivation,
            RequiredFeedItemId: "material.organic_compost",
            RequiredFeedQuantity: 2,
            CycleDurationTicks: 150,
            OutputEntityId: "material.mushroom_cap",
            OutputQuantity: 2);

        public static readonly FarmBreedingRecipe HamsterRecipe = new(
            FarmOperationMode.HamsterBreeding,
            RequiredFeedItemId: "food.grilled_mushroom",
            RequiredFeedQuantity: 1,
            CycleDurationTicks: 300,
            OutputEntityId: "creature.hamster",
            OutputQuantity: 1);

        public static readonly FarmBreedingRecipe GrubRecipe = new(
            FarmOperationMode.GrubBreeding,
            RequiredFeedItemId: "material.mushroom_leg",
            RequiredFeedQuantity: 1,
            CycleDurationTicks: 200,
            OutputEntityId: "creature.grub",
            OutputQuantity: 1);
    }
}
