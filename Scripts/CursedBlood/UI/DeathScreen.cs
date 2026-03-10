using System;
using CursedBlood.Player;
using Godot;

namespace CursedBlood.UI
{
    public partial class DeathScreen : CanvasLayer
    {
        private static readonly Vector2 PanelDesignPosition = new(110f, 260f);

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
            CanvasLayoutHelper.CenterFromReference(this, _panel, PanelDesignPosition);
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
                Position = PanelDesignPosition,
                Size = new Vector2(860f, 1110f)
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

            _summaryLabel = CreateLabel(new Vector2(120f, 260f), new Vector2(620f, 420f), 36, HorizontalAlignment.Left);
            _panel.AddChild(_summaryLabel);

            _hintLabel = CreateLabel(new Vector2(80f, 870f), new Vector2(700f, 66f), 24, HorizontalAlignment.Center);
            _hintLabel.Text = "タップ / クリック / キー入力で次の潜行へ";
            _panel.AddChild(_hintLabel);

            _restartButton = new Button
            {
                Position = new Vector2(250f, 960f),
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