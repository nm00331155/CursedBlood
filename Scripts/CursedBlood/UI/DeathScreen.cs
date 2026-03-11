using System;
using CursedBlood.Player;
using Godot;

namespace CursedBlood.UI
{
    public partial class DeathScreen : CanvasLayer
    {
        private static readonly Vector2 PanelDesignSize = new(860f, 1110f);

        private bool _built;
        private ColorRect _overlay;
        private Panel _panel;
        private Label _titleLabel;
        private Label _causeLabel;
        private Label _summaryLabel;
        private Label _hintLabel;
        private Button _restartButton;

        public event Action RestartRequested;

        public bool IsVisibleScreen => _overlay != null && _overlay.Visible;

        public void Initialize()
        {
            BuildUiIfNeeded();
            ApplyViewportLayout();
            HideScreen();
        }

        public void Show(PlayerStats stats, string cause)
        {
            BuildUiIfNeeded();
            ApplyViewportLayout();
            _titleLabel.Text = "潜行終了";
            _causeLabel.Text = cause;
            _summaryLabel.Text = string.Join("\n", new[]
            {
                $"到達深度 {_statsDepth(stats)}m",
                $"掘削 {stats.BlocksDug}",
                $"鉱石 {stats.OresCollected}",
                $"回収額 {stats.SalvageValue:N0}",
                $"Score {stats.CalculateScore():N0}"
            });
            SetScreenVisible(true);
        }

        public void HideScreen()
        {
            SetScreenVisible(false);
        }

        public void ApplyViewportLayout()
        {
            if (!_built)
            {
                return;
            }

            CanvasLayoutHelper.StretchOverlay(this, _overlay);
            var panelRect = CanvasLayoutHelper.ResolveCenteredPanelRect(this, PanelDesignSize, 0.86f, 0.72f, 44f, 48f);
            _panel.Position = panelRect.Position;
            _panel.Size = panelRect.Size;

            var scale = CanvasLayoutHelper.GetScaleFactors(panelRect.Size, PanelDesignSize);
            CanvasLayoutHelper.ApplyScaledLayout(_titleLabel, new Vector2(80f, 54f), new Vector2(700f, 64f), scale);
            CanvasLayoutHelper.ApplyScaledLayout(_causeLabel, new Vector2(80f, 136f), new Vector2(700f, 44f), scale);
            CanvasLayoutHelper.ApplyScaledLayout(_summaryLabel, new Vector2(120f, 260f), new Vector2(620f, 460f), scale);
            CanvasLayoutHelper.ApplyScaledLayout(_hintLabel, new Vector2(80f, 860f), new Vector2(700f, 68f), scale);
            CanvasLayoutHelper.ApplyScaledLayout(_restartButton, new Vector2(250f, 954f), new Vector2(360f, 88f), scale);

            _titleLabel.AddThemeFontSizeOverride("font_size", CanvasLayoutHelper.ScaleFont(54, scale, 28, 78));
            _causeLabel.AddThemeFontSizeOverride("font_size", CanvasLayoutHelper.ScaleFont(30, scale, 16, 44));
            _summaryLabel.AddThemeFontSizeOverride("font_size", CanvasLayoutHelper.ScaleFont(36, scale, 18, 48));
            _hintLabel.AddThemeFontSizeOverride("font_size", CanvasLayoutHelper.ScaleFont(24, scale, 14, 34));
            _restartButton.AddThemeFontSizeOverride("font_size", CanvasLayoutHelper.ScaleFont(32, scale, 18, 44));
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (!IsVisibleScreen)
            {
                return;
            }

            if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
            {
                RestartRequested?.Invoke();
                GetViewport().SetInputAsHandled();
                return;
            }

            if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed)
            {
                RestartRequested?.Invoke();
                GetViewport().SetInputAsHandled();
                return;
            }

            if (@event is InputEventScreenTouch screenTouch && screenTouch.Pressed)
            {
                RestartRequested?.Invoke();
                GetViewport().SetInputAsHandled();
            }
        }

        private void BuildUiIfNeeded()
        {
            if (_built)
            {
                return;
            }

            _overlay = new ColorRect
            {
                Position = Vector2.Zero,
                Size = new Vector2(1080f, 1920f),
                Color = new Color(0f, 0f, 0f, 0.84f)
            };
            AddChild(_overlay);

            _panel = new Panel
            {
                Position = Vector2.Zero,
                Size = PanelDesignSize
            };
            var panelStyle = new StyleBoxFlat
            {
                BgColor = new Color(0.08f, 0.10f, 0.15f, 0.98f),
                BorderColor = new Color(0.84f, 0.90f, 0.98f),
                BorderWidthTop = 3,
                BorderWidthBottom = 3,
                BorderWidthLeft = 3,
                BorderWidthRight = 3,
                CornerRadiusTopLeft = 28,
                CornerRadiusTopRight = 28,
                CornerRadiusBottomLeft = 28,
                CornerRadiusBottomRight = 28
            };
            _panel.AddThemeStyleboxOverride("panel", panelStyle);
            AddChild(_panel);

            _titleLabel = CreateLabel(new Vector2(80f, 54f), new Vector2(700f, 64f), 54, HorizontalAlignment.Center);
            _panel.AddChild(_titleLabel);

            _causeLabel = CreateLabel(new Vector2(80f, 136f), new Vector2(700f, 44f), 30, HorizontalAlignment.Center);
            _panel.AddChild(_causeLabel);

            _summaryLabel = CreateLabel(new Vector2(120f, 260f), new Vector2(620f, 460f), 36, HorizontalAlignment.Left);
            _summaryLabel.VerticalAlignment = VerticalAlignment.Top;
            _panel.AddChild(_summaryLabel);

            _hintLabel = CreateLabel(new Vector2(80f, 860f), new Vector2(700f, 68f), 24, HorizontalAlignment.Center);
            _hintLabel.Text = "タップ / クリック / キー入力で次の潜行へ";
            _panel.AddChild(_hintLabel);

            _restartButton = new Button
            {
                Position = new Vector2(250f, 954f),
                Size = new Vector2(360f, 88f),
                Text = "次の潜行へ"
            };
            _restartButton.AddThemeFontSizeOverride("font_size", 32);
            _restartButton.Pressed += () => RestartRequested?.Invoke();
            _panel.AddChild(_restartButton);

            _built = true;
            ApplyViewportLayout();
        }

        private void SetScreenVisible(bool visible)
        {
            if (!_built)
            {
                return;
            }

            _overlay.Visible = visible;
            _panel.Visible = visible;
        }

        private static int _statsDepth(PlayerStats stats)
        {
            return stats?.MaxDepthMeters ?? 0;
        }

        private static Label CreateLabel(Vector2 position, Vector2 size, int fontSize, HorizontalAlignment alignment)
        {
            var label = new Label
            {
                Position = position,
                Size = size,
                HorizontalAlignment = alignment,
                VerticalAlignment = VerticalAlignment.Center,
                AutowrapMode = TextServer.AutowrapMode.WordSmart
            };
            label.AddThemeFontSizeOverride("font_size", fontSize);
            label.AddThemeColorOverride("font_color", new Color(0.96f, 0.97f, 0.99f));
            return label;
        }
    }
}