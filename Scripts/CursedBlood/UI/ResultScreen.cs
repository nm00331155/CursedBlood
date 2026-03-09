using System;
using CursedBlood.Core;
using Godot;

namespace CursedBlood.UI
{
    public partial class ResultScreen : CanvasLayer
    {
        private bool _built;
        private ColorRect _overlay;
        private Panel _panel;
        private Label _titleLabel;
        private Label _resultLabel;
        private Label _summaryLabel;
        private Label _hintLabel;
        private Button _continueButton;

        public bool IsVisibleScreen => _overlay != null && _overlay.Visible;

        public event Action ContinueRequested;

        public void Initialize()
        {
            BuildUiIfNeeded();
            SetScreenVisible(false);
        }

        public void Show(DiveResultData result)
        {
            BuildUiIfNeeded();
            _resultLabel.Text = result.OutcomeLabel;
            _summaryLabel.Text = result.BuildSummaryText();
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
                ContinueRequested?.Invoke();
                GetViewport().SetInputAsHandled();
                return;
            }

            if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed)
            {
                ContinueRequested?.Invoke();
                GetViewport().SetInputAsHandled();
                return;
            }

            if (@event is InputEventScreenTouch screenTouch && screenTouch.Pressed)
            {
                ContinueRequested?.Invoke();
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
                Color = new Color(0f, 0f, 0f, 0.82f)
            };
            AddChild(_overlay);

            _panel = new Panel
            {
                Position = new Vector2(110f, 250f),
                Size = new Vector2(860f, 1120f)
            };
            var panelStyle = new StyleBoxFlat
            {
                BgColor = new Color(0.08f, 0.10f, 0.15f, 0.98f),
                BorderColor = new Color(0.84f, 0.90f, 0.98f),
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

            _titleLabel = CreateLabel(new Vector2(80f, 54f), new Vector2(700f, 56f), 48, HorizontalAlignment.Center);
            _titleLabel.Text = "潜行結果";
            _panel.AddChild(_titleLabel);

            _resultLabel = CreateLabel(new Vector2(80f, 130f), new Vector2(700f, 40f), 28, HorizontalAlignment.Center);
            _panel.AddChild(_resultLabel);

            _summaryLabel = CreateLabel(new Vector2(120f, 240f), new Vector2(620f, 560f), 34, HorizontalAlignment.Left);
            _panel.AddChild(_summaryLabel);

            _hintLabel = CreateLabel(new Vector2(80f, 890f), new Vector2(700f, 66f), 24, HorizontalAlignment.Center);
            _hintLabel.Text = "タップ / クリック / キー入力で返済画面へ";
            _panel.AddChild(_hintLabel);

            _continueButton = new Button
            {
                Position = new Vector2(250f, 980f),
                Size = new Vector2(360f, 84f),
                Text = "返済画面へ"
            };
            _continueButton.AddThemeFontSizeOverride("font_size", 30);
            _continueButton.Pressed += () => ContinueRequested?.Invoke();
            _panel.AddChild(_continueButton);

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