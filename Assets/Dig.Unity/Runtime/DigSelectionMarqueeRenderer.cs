using UnityEngine;

namespace Dig.Unity
{
    [DisallowMultipleComponent]
    internal sealed class DigSelectionMarqueeRenderer : MonoBehaviour
    {
        private bool _visible;
        private Vector2 _start;
        private Vector2 _current;

        internal void Show(Vector2 start, Vector2 current)
        {
            _start = start;
            _current = current;
            _visible = true;
        }

        internal void Clear()
        {
            _visible = false;
        }

        private void OnGUI()
        {
            if (!_visible)
            {
                return;
            }

            Rect rectangle = ToGuiRect(_start, _current);
            Color previous = GUI.color;
            GUI.color = new Color(0.25f, 0.75f, 1f, 0.18f);
            GUI.DrawTexture(rectangle, Texture2D.whiteTexture);
            GUI.color = new Color(0.35f, 0.85f, 1f, 0.95f);
            DrawBorder(rectangle, 2f);
            GUI.color = previous;
        }

        private static Rect ToGuiRect(Vector2 first, Vector2 second)
        {
            float left = Mathf.Min(first.x, second.x);
            float right = Mathf.Max(first.x, second.x);
            float bottom = Mathf.Min(first.y, second.y);
            float top = Mathf.Max(first.y, second.y);
            return Rect.MinMaxRect(left, Screen.height - top, right, Screen.height - bottom);
        }

        private static void DrawBorder(Rect rectangle, float width)
        {
            GUI.DrawTexture(
                new Rect(rectangle.xMin, rectangle.yMin, rectangle.width, width),
                Texture2D.whiteTexture);
            GUI.DrawTexture(
                new Rect(rectangle.xMin, rectangle.yMax - width, rectangle.width, width),
                Texture2D.whiteTexture);
            GUI.DrawTexture(
                new Rect(rectangle.xMin, rectangle.yMin, width, rectangle.height),
                Texture2D.whiteTexture);
            GUI.DrawTexture(
                new Rect(rectangle.xMax - width, rectangle.yMin, width, rectangle.height),
                Texture2D.whiteTexture);
        }
    }
}
