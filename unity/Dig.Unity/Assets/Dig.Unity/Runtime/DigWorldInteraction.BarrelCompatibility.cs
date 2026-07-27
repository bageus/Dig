using Dig.Domain.Ecology;
using UnityEngine;

namespace Dig.Unity
{

internal sealed class DigMushromVisual
{
    internal DigMushromVisual(DigMushroomVisual visual)
    {
        Visual = visual;
    }

    internal DigMushroomVisual Visual { get; }
    internal MushroomSiteSnapshot Model => Visual.Model;
}

internal static class DigMushroomRendererCompatibilityExtensions
{
    internal static bool TryGetMushroom(
        this DigMushroomRenderer renderer,
        RaycastHit hit,
        out DigMushromVisual visual)
    {
        if (renderer.TryGetMushroom(hit, out DigMushroomVisual actual))
        {
            visual = new DigMushromVisual(actual);
            return true;
        }

        visual = null!;
        return false;
    }
}

public sealed partial class DigWorldInteraction
{
    private void CancelResidentMarqueee()
    {
        CancelResidentMarquee();
    }
}

}