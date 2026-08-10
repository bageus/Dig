namespace Dig.Unity
{
    public sealed partial class DigWorldInteraction
    {
        private static bool UsesTunnelCellInteraction(DigExcavationDrawingMode mode)
        {
            return mode == DigExcavationDrawingMode.Tunnel
                || mode == DigExcavationDrawingMode.Delete
                || mode == DigExcavationDrawingMode.Depth;
        }
    }
}
