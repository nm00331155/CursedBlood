using Godot;

namespace CursedBlood.UI
{
    internal static class CanvasLayoutHelper
    {
        public static readonly Vector2 ReferenceScreenSize = new(1080f, 1920f);

        public static Rect2 GetVisibleRect(CanvasLayer layer)
        {
            return layer?.GetViewport()?.GetVisibleRect() ?? new Rect2(Vector2.Zero, ReferenceScreenSize);
        }

        public static void StretchOverlay(CanvasLayer layer, Control overlay)
        {
            if (layer == null || overlay == null)
            {
                return;
            }

            var visibleRect = GetVisibleRect(layer);
            overlay.Position = visibleRect.Position;
            overlay.Size = visibleRect.Size;
        }

        public static void CenterFromReference(CanvasLayer layer, Control control, Vector2 designPosition)
        {
            if (layer == null || control == null)
            {
                return;
            }

            var visibleRect = GetVisibleRect(layer);
            var shift = (visibleRect.Size - ReferenceScreenSize) * 0.5f;
            control.Position = visibleRect.Position + designPosition + shift;
        }

        public static Rect2 ResolveCenteredPanelRect(
            CanvasLayer layer,
            Vector2 designSize,
            float widthUsage,
            float heightUsage,
            float horizontalMargin = 48f,
            float verticalMargin = 48f)
        {
            var visibleRect = GetVisibleRect(layer);
            var maxSize = new Vector2(
                Mathf.Max(1f, visibleRect.Size.X - (horizontalMargin * 2f)),
                Mathf.Max(1f, visibleRect.Size.Y - (verticalMargin * 2f)));
            var panelSize = new Vector2(
                Mathf.Min(maxSize.X, Mathf.Max(designSize.X, visibleRect.Size.X * widthUsage)),
                Mathf.Min(maxSize.Y, Mathf.Max(designSize.Y, visibleRect.Size.Y * heightUsage)));

            return new Rect2(
                visibleRect.Position + ((visibleRect.Size - panelSize) * 0.5f),
                panelSize);
        }

        public static Vector2 GetScaleFactors(Vector2 currentSize, Vector2 designSize)
        {
            return new Vector2(
                Mathf.Max(0.1f, currentSize.X / Mathf.Max(1f, designSize.X)),
                Mathf.Max(0.1f, currentSize.Y / Mathf.Max(1f, designSize.Y)));
        }

        public static void ApplyScaledLayout(Control control, Vector2 designPosition, Vector2 designSize, Vector2 scale)
        {
            if (control == null)
            {
                return;
            }

            control.Position = new Vector2(designPosition.X * scale.X, designPosition.Y * scale.Y);
            control.Size = new Vector2(designSize.X * scale.X, designSize.Y * scale.Y);
        }

        public static int ScaleFont(int baseFontSize, Vector2 scale, int minFontSize = 12, int maxFontSize = 120)
        {
            var scaleFactor = Mathf.Sqrt(scale.X * scale.Y);
            return Mathf.Clamp(Mathf.RoundToInt(baseFontSize * scaleFactor), minFontSize, maxFontSize);
        }

        public static void UpdateScrollableLabelContent(Label label, ScrollContainer scrollContainer, float padding = 12f)
        {
            if (label == null || scrollContainer == null)
            {
                return;
            }

            var contentWidth = Mathf.Max(1f, scrollContainer.Size.X - padding);
            var font = label.GetThemeFont("font");
            var fontSize = label.GetThemeFontSize("font_size");
            var measuredSize = font != null
                ? font.GetMultilineStringSize(label.Text ?? string.Empty, label.HorizontalAlignment, contentWidth, fontSize)
                : label.Size;
            var contentHeight = Mathf.Max(scrollContainer.Size.Y, measuredSize.Y + padding);

            label.Position = Vector2.Zero;
            label.Size = new Vector2(contentWidth, contentHeight);
            label.CustomMinimumSize = label.Size;
        }
    }
}