using System;

namespace Dig.Unity
{
    public sealed partial class DigWorldInteraction
    {
        internal void ActivateExcavationDrawingMode(DigExcavationDrawingMode mode)
        {
            if (mode != DigExcavationDrawingMode.None
                && _agentRenderer != null
                && _agentRenderer.SelectedCount > 0)
            {
                CancelResidentMarquee();
                _agentRenderer.RestoreSelection(Array.Empty<string>(), primaryAgentId: null);
            }

            SetExcavationDrawingMode(mode);
        }
    }
}
