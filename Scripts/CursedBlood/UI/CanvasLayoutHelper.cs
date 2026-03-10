using Godot;

namespace CursedBlood.UI
{
    internal static class CanvasLayoutHelper
    {
        private static readonly Vector2 ReferenceScreenSize = new(1080f, 1920f);

        public static void StretchOverlay(CanvasLayer layer, Control overlay)
        {
            if (layer == null || overlay == null)
            {
                return;
            }

            var visibleRect = layer.GetViewport()?.GetVisibleRect() ?? new Rect2(Vector2.Zero, ReferenceScreenSize);
            overlay.Position = visibleRect.Position;
            overlay.Size = visibleRect.Size;
        }

        public static void CenterFromReference(CanvasLayer layer, Control control, Vector2 designPosition)
        {
            if (layer == null || control == null)
            {
                return;
            }

            var visibleRect = layer.GetViewport()?.GetVisibleRect() ?? new Rect2(Vector2.Zero, ReferenceScreenSize);
            var shift = (visibleRect.Size - ReferenceScreenSize) * 0.5f;
            control.Position = visibleRect.Position + designPosition + shift;
        }
    }
}