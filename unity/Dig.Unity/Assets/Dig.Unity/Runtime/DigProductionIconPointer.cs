using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Dig.Unity
{

internal sealed class DigProductionIconPointer : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    internal Action<bool>? HoverChanged { get; set; }

    internal Action? RightClicked { get; set; }

    public void OnPointerEnter(PointerEventData eventData)
    {
        HoverChanged?.Invoke(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HoverChanged?.Invoke(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null)
        {
            throw new ArgumentNullException(nameof(eventData));
        }

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            RightClicked?.Invoke();
        }
    }

    private void OnDisable()
    {
        HoverChanged?.Invoke(false);
    }
}

internal static class DigProductionIconGlyph
{
    internal static string Resolve(string itemId)
    {
        if (itemId.IndexOf("tent", StringComparison.Ordinal) >= 0)
        {
            return "⌂";
        }

        if (itemId.IndexOf("stone_mason", StringComparison.Ordinal) >= 0)
        {
            return "▦";
        }

        if (itemId.IndexOf("wood_workshop", StringComparison.Ordinal) >= 0)
        {
            return "▥";
        }

        if (itemId.IndexOf("campfire", StringComparison.Ordinal) >= 0)
        {
            return "♨";
        }

        if (itemId.IndexOf("mushroom_cap", StringComparison.Ordinal) >= 0
            || itemId.IndexOf("grilled_mushroom", StringComparison.Ordinal) >= 0)
        {
            return "●";
        }

        if (itemId.IndexOf("mushroom_leg", StringComparison.Ordinal) >= 0)
        {
            return "│";
        }

        if (itemId.IndexOf("stone", StringComparison.Ordinal) >= 0)
        {
            return "◆";
        }

        if (itemId.IndexOf("hamster", StringComparison.Ordinal) >= 0)
        {
            return "◉";
        }

        if (itemId.StartsWith("building_box.", StringComparison.Ordinal))
        {
            return "□";
        }

        return itemId.StartsWith("food.", StringComparison.Ordinal) ? "○" : "•";
    }
}

}
