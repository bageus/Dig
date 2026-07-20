using System.Collections.Generic;
using Dig.Presentation.Overlays;
using Xunit;

namespace Dig.Tests
{

public sealed class OverlayVisibilityResolverTests
{
    private readonly OverlayVisibilityResolver _resolver =
        OverlayVisibilityResolver.CreateDefault();

    [Fact]
    public void Default_priorities_keep_selection_and_preview_above_diagnostics()
    {
        Assert.True(
            _resolver.ResolveLayer(OverlayLayerKind.Selection).Priority
            > _resolver.ResolveLayer(OverlayLayerKind.Preview).Priority);
        Assert.True(
            _resolver.ResolveLayer(OverlayLayerKind.Preview).Priority
            > _resolver.ResolveLayer(OverlayLayerKind.Jobs).Priority);
        Assert.True(
            _resolver.ResolveLayer(OverlayLayerKind.Jobs).Priority
            > _resolver.ResolveLayer(OverlayLayerKind.Routes).Priority);
        Assert.True(
            _resolver.ResolveLayer(OverlayLayerKind.Routes).Priority
            > _resolver.ResolveLayer(OverlayLayerKind.Diagnostics).Priority);
    }

    [Fact]
    public void Release_profile_cannot_reveal_debug_only_layers()
    {
        Dictionary<OverlayLayerKind, bool> overrides = new()
        {
            [OverlayLayerKind.Routes] = true,
            [OverlayLayerKind.Diagnostics] = true,
        };

        OverlayVisibilitySnapshot snapshot = _resolver.CreateSnapshot(
            OverlayVisibilityProfile.Release,
            overrides);

        Assert.False(snapshot.IsVisible(OverlayLayerKind.Routes));
        Assert.False(snapshot.IsVisible(OverlayLayerKind.Diagnostics));
        Assert.True(snapshot.IsVisible(OverlayLayerKind.Jobs));
        Assert.True(snapshot.IsVisible(OverlayLayerKind.Preview));
    }

    [Fact]
    public void Debug_profile_preserves_default_route_and_job_visibility()
    {
        OverlayVisibilitySnapshot snapshot = _resolver.CreateSnapshot(
            OverlayVisibilityProfile.Debug);

        Assert.True(snapshot.IsVisible(OverlayLayerKind.Jobs));
        Assert.True(snapshot.IsVisible(OverlayLayerKind.Routes));
        Assert.False(snapshot.IsVisible(OverlayLayerKind.Diagnostics));
    }

    [Fact]
    public void All_profile_can_still_hide_one_user_toggled_layer()
    {
        Dictionary<OverlayLayerKind, bool> overrides = new()
        {
            [OverlayLayerKind.Jobs] = false,
        };

        OverlayVisibilitySnapshot snapshot = _resolver.CreateSnapshot(
            OverlayVisibilityProfile.All,
            overrides);

        Assert.False(snapshot.IsVisible(OverlayLayerKind.Jobs));
        Assert.True(snapshot.IsVisible(OverlayLayerKind.Routes));
        Assert.True(snapshot.IsVisible(OverlayLayerKind.Diagnostics));
    }

    [Fact]
    public void Identical_visibility_inputs_have_stable_versions()
    {
        OverlayVisibilitySnapshot first = _resolver.CreateSnapshot(
            OverlayVisibilityProfile.Debug);
        OverlayVisibilitySnapshot second = _resolver.CreateSnapshot(
            OverlayVisibilityProfile.Debug);
        OverlayVisibilitySnapshot release = _resolver.CreateSnapshot(
            OverlayVisibilityProfile.Release);

        Assert.Equal(first.Version, second.Version);
        Assert.Equal(first.VisibleLayers, second.VisibleLayers);
        Assert.NotEqual(first.Version, release.Version);
    }

    [Fact]
    public void Accessibility_styles_do_not_depend_on_colour_alone()
    {
        OverlayStyleDefinition invalid = Find(OverlaySemanticKind.PreviewInvalid);
        OverlayStyleDefinition selected = Find(OverlaySemanticKind.Selection);
        OverlayStyleDefinition blocked = Find(OverlaySemanticKind.JobBlocked);

        Assert.Equal(OverlayShapeKind.Cross, invalid.Shape);
        Assert.Equal(OverlayPatternKind.CrossHatch, invalid.Pattern);
        Assert.Equal(OverlayShapeKind.Diamond, selected.Shape);
        Assert.Equal(OverlayPatternKind.Double, selected.Pattern);
        Assert.Equal(OverlayShapeKind.Cross, blocked.Shape);
        Assert.NotEqual(100, blocked.ScalePercent);
    }

    private static OverlayStyleDefinition Find(OverlaySemanticKind semantic)
    {
        for (int index = 0; index < DefaultOverlayStyles.Values.Count; index++)
        {
            OverlayStyleDefinition value = DefaultOverlayStyles.Values[index];
            if (value.Semantic == semantic)
            {
                return value;
            }
        }

        throw new Xunit.Sdk.XunitException($"Missing overlay style {semantic}.");
    }
}
}
