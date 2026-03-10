using Godot;

namespace CursedBlood.Core
{
    public readonly record struct GameplayLayoutMetrics(
        Rect2 VisibleRect,
        Rect2 PlayfieldRect,
        float ReservedLeft,
        float ReservedTop,
        float ReservedRight,
        float ReservedBottom,
        Vector2 ReferenceAvailableSize,
        Vector2 CameraFocusOffsetPixels)
    {
        public Vector2 ScreenSize => VisibleRect.Size;

        public Vector2 AvailableSize => PlayfieldRect.Size;
    }

    public readonly record struct GameplayProjectionMetrics(
        Vector2 LogicalWorldSize,
        float RenderScale,
        Vector2 RenderSize,
        float CameraZoomScale);

    public static class GameplayLayoutCalculator
    {
        public static readonly Vector2 ReferenceScreenSize = new(1080f, 1920f);

        private const float InfoPanelTop = 18f;
        private const float InfoPanelHeight = 188f;
        private const float InfoPanelBottomPadding = 12f;
        private const float MapPanelRightMargin = 18f;
        private const float MapPanelFramePadding = 24f;
        private const float SonarPanelTop = 1586f;
        private const float ReturnPanelHeight = 172f;
        private const float ReturnPanelBottomMargin = 76f;

        public static GameplayLayoutMetrics Calculate(Rect2 visibleRect, Vector2 minimapSize)
        {
            var reservedTop = InfoPanelTop + InfoPanelHeight + InfoPanelBottomPadding;
            var reservedRight = MapPanelRightMargin + Mathf.Max(180f, minimapSize.X) + MapPanelFramePadding;
            var reservedBottom = Mathf.Max(
                ReferenceScreenSize.Y - SonarPanelTop,
                ReturnPanelBottomMargin + ReturnPanelHeight);
            var reservedLeft = 0f;

            var availableSize = new Vector2(
                Mathf.Max(1f, visibleRect.Size.X - reservedLeft - reservedRight),
                Mathf.Max(1f, visibleRect.Size.Y - reservedTop - reservedBottom));
            var playfieldRect = new Rect2(
                visibleRect.Position + new Vector2(reservedLeft, reservedTop),
                availableSize);
            var referenceAvailableSize = new Vector2(
                Mathf.Max(1f, ReferenceScreenSize.X - reservedLeft - reservedRight),
                Mathf.Max(1f, ReferenceScreenSize.Y - reservedTop - reservedBottom));
            var cameraFocusOffsetPixels = GetCenter(playfieldRect) - GetCenter(visibleRect);

            return new GameplayLayoutMetrics(
                visibleRect,
                playfieldRect,
                reservedLeft,
                reservedTop,
                reservedRight,
                reservedBottom,
                referenceAvailableSize,
                cameraFocusOffsetPixels);
        }

        public static GameplayProjectionMetrics ResolveProjection(GameplayLayoutMetrics layout, float baseZoomScale)
        {
            var clampedBaseZoom = Mathf.Clamp(baseZoomScale, 0.18f, 1.0f);
            var logicalWorldSize = layout.ReferenceAvailableSize * clampedBaseZoom;
            var renderScale = Mathf.Min(
                layout.AvailableSize.X / Mathf.Max(1f, logicalWorldSize.X),
                layout.AvailableSize.Y / Mathf.Max(1f, logicalWorldSize.Y));
            renderScale = Mathf.Max(0.01f, renderScale);

            var renderSize = logicalWorldSize * renderScale;
            var cameraZoomScale = Mathf.Clamp(1f / renderScale, 0.18f, 1.0f);

            return new GameplayProjectionMetrics(logicalWorldSize, renderScale, renderSize, cameraZoomScale);
        }

        public static Vector2 ResolveCameraWorldOffset(GameplayLayoutMetrics layout, float cameraZoomScale)
        {
            return -layout.CameraFocusOffsetPixels * cameraZoomScale;
        }

        public static Vector2 ResolveVirtualPadOrigin(GameplayLayoutMetrics layout, Vector2 designOrigin)
        {
            var bottomMargin = ReferenceScreenSize.Y - designOrigin.Y;
            return new Vector2(
                layout.VisibleRect.Position.X + designOrigin.X,
                layout.VisibleRect.Position.Y + layout.ScreenSize.Y - bottomMargin);
        }

        public static Rect2 AlignTopRight(GameplayLayoutMetrics layout, Vector2 size, float rightMargin, float topMargin)
        {
            return new Rect2(
                layout.VisibleRect.Position + new Vector2(layout.ScreenSize.X - rightMargin - size.X, topMargin),
                size);
        }

        public static Rect2 AlignBottomRight(GameplayLayoutMetrics layout, Vector2 size, float rightMargin, float bottomMargin)
        {
            return new Rect2(
                layout.VisibleRect.Position + new Vector2(layout.ScreenSize.X - rightMargin - size.X, layout.ScreenSize.Y - bottomMargin - size.Y),
                size);
        }

        public static Rect2 AlignBottomCenter(GameplayLayoutMetrics layout, Vector2 size, float bottomMargin)
        {
            return new Rect2(
                layout.VisibleRect.Position + new Vector2((layout.ScreenSize.X - size.X) * 0.5f, layout.ScreenSize.Y - bottomMargin - size.Y),
                size);
        }

        public static Rect2 AlignCentered(GameplayLayoutMetrics layout, Vector2 size)
        {
            return new Rect2(
                layout.VisibleRect.Position + ((layout.ScreenSize - size) * 0.5f),
                size);
        }

        private static Vector2 GetCenter(Rect2 rect)
        {
            return rect.Position + (rect.Size * 0.5f);
        }
    }
}