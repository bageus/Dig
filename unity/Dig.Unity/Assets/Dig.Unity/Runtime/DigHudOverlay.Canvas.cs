using System;

namespace Dig.Unity
{

public sealed partial class DigHudOverlay
{
    private DigGameHudCanvas? _gameHudCanvas;

    internal bool HasGameHudCanvas => _gameHudCanvas != null;

    internal void AttachGameHudCanvas(DigGameHudCanvas canvas)
    {
        _gameHudCanvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
        _gameHudCanvas.SetStatus(_status);
    }
}

}
