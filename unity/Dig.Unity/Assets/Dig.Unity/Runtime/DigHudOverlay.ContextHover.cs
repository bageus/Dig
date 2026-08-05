namespace Dig.Unity
{

public sealed partial class DigHudOverlay
{
    internal void SetWorldTargetHoverInfo(string title)
    {
        _gameHudCanvas?.SetWorldTargetHoverInfo(title);
    }

    internal void ClearWorldTargetHoverInfo()
    {
        _gameHudCanvas?.ClearWorldTargetHoverInfo();
    }
}

}
