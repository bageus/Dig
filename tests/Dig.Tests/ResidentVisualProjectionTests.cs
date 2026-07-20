using Dig.Presentation.Agents;
using Xunit;

namespace Dig.Tests
{
public sealed class ResidentVisualProjectionTests
{
    [Fact]
    public void Same_identity_restores_same_variant()
    {
        ResidentVisualPresenter presenter = new ResidentVisualPresenter();
        ResidentAppearanceViewModel first = presenter.PresentAppearance(
            "resident.ada", ResidentBodyVariant.Feminine,
            ResidentAgeVisualBand.Old, ResidentHeadwearRole.Miner);
        ResidentAppearanceViewModel second = presenter.PresentAppearance(
            "resident.ada", ResidentBodyVariant.Feminine,
            ResidentAgeVisualBand.Old, ResidentHeadwearRole.Miner);
        Assert.Equal(first.Version, second.Version);
        Assert.Equal(first.ClothingPaletteIndex, second.ClothingPaletteIndex);
        Assert.Equal(first.HairVariant, second.HairVariant);
    }
}
}
