using System;
using CursedBlood.Player;
using Godot;

namespace CursedBlood.UI
{
    public partial class DeathScreen : CanvasLayer
    {
        private bool _built;
        private ColorRect _overlay;
        private Panel _panel;
        private Label _titleLabel;
        private Label _causeLabel;
        private Label _summaryLabel;
        private Label _hintLabel;
        private Button _restartButton;

        public bool IsVisibleScreen => _overlay != null && _overlay.Visible;

        public event Action RestartRequested;

        public void Initialize()
        {
            BuildUiIfNeeded();
            SetScreenVisible(false);
        }

        public void Show(PlayerStats stats, string cause)
        {
            BuildUiIfNeeded();
            _causeLabel.Text = cause;
            _summaryLabel.Text =
                $"世代: 第{stats.Generation}世代\n" +
                $"享年: {Mathf.RoundToInt(stats.HumanAge)}歳\n" +
                $"最大深度: {stats.MaxDepthMeters}m\n" +
                $"スコア: {stats.CalculateScore():N0}";
            SetScreenVisible(true);
        }

        public void HideScreen()
        {
            SetScreenVisible(false);
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
                Color = new Color(0f, 0f, 0f, 0.78f)
            };
            AddChild(_overlay);

            _panel = new Panel
            {
                Position = new Vector2(120f, 360f),
                Size = new Vector2(840f, 920f)
            };
            var panelStyle = new StyleBoxFlat
            {
                BgColor = new Color(0.10f, 0.12f, 0.16f, 0.98f),
                BorderColor = new Color(0.86f, 0.90f, 0.96f),
                BorderWidthTop = 3,
                BorderWidthBottom = 3,
                BorderWidthLeft = 3,
                BorderWidthRight = 3,
                CornerRadiusTopLeft = 24,
                CornerRadiusTopRight = 24,
                CornerRadiusBottomLeft = 24,
                CornerRadiusBottomRight = 24
            };
            _panel.AddThemeStyleboxOverride("panel", panelStyle);
            AddChild(_panel);

            _titleLabel = CreateLabel(new Vector2(70f, 60f), new Vector2(700f, 56f), 48, HorizontalAlignment.Center);
            _titleLabel.Text = "血脈はここで尽きた";
            _panel.AddChild(_titleLabel);

            _causeLabel = CreateLabel(new Vector2(70f, 134f), new Vector2(700f, 36f), 26, HorizontalAlignment.Center);
            _panel.AddChild(_causeLabel);

            _summaryLabel = CreateLabel(new Vector2(120f, 250f), new Vector2(600f, 260f), 34, HorizontalAlignment.Left);
            _panel.AddChild(_summaryLabel);

            _hintLabel = CreateLabel(new Vector2(70f, 650f), new Vector2(700f, 72f), 24, HorizontalAlignment.Center);
            _hintLabel.Text = "タップ / クリック / キー入力で次世代を開始";
            _panel.AddChild(_hintLabel);

            _restartButton = new Button
            {
                Position = new Vector2(240f, 760f),
                Size = new Vector2(360f, 72f),
                Text = "次世代へ"
            };
            _restartButton.AddThemeFontSizeOverride("font_size", 28);
            _restartButton.Pressed += () => RestartRequested?.Invoke();
            _panel.AddChild(_restartButton);

            _built = true;
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