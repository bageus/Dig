using Dig.Domain.Navigation;
using Dig.Presentation.Agents;

namespace Dig.Unity
{
public sealed partial class DigAgentVisual
{
    private void ResolveSurfaceCoordinates(
        AgentViewModel model,
        out double x,
        out double y,
        out double z)
    {
        double u = (model.SurfaceU - SurfacePose.CellCentre)
            / (double)SurfacePose.UnitsPerCell;
        double v = (model.SurfaceV - SurfacePose.CellCentre)
            / (double)SurfacePose.UnitsPerCell;
        x = model.CellX;
        y = model.CellY;
        z = model.CellZ;

        switch (model.SurfaceFace)
        {
            case SurfaceFace.Floor:
                x += u;
                z += v;
                break;
            case SurfaceFace.NegativeX:
                x -= 0.5d;
                z += u;
                y += v;
                break;
            case SurfaceFace.PositiveX:
                x += 0.5d;
                z += u;
                y += v;
                break;
            case SurfaceFace.NegativeZ:
                z -= 0.5d;
                x += u;
                y += v;
                break;
            case SurfaceFace.PositiveZ:
                z += 0.5d;
                x += u;
                y += v;
                break;
        }

        if (model.SurfaceFace == SurfaceFace.Floor
            && _freeformDestinationCell.HasValue)
        {
            x = ResolveVisualX(model.CellX, model.CellY, model.CellZ);
            z = model.CellZ + _freeformDestinationOffsetZ;
        }
    }
}
}
