using System;
using CursedBlood.Core;
using Godot;

namespace CursedBlood.UI
{
    public partial class ResultScreen : CanvasLayer
    {
        private static readonly Vector2 PanelDesignPosition = new(80f, 190f);

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
            ApplyViewportLayout();
            SetScreenVisible(false);
        }

        public void Show(DiveResultData result)
        {
            BuildUiIfNeeded();
            ApplyViewportLayout();
            _resultLabel.Text = result.OutcomeLabel;
            _summaryLabel.Text = result.BuildSummaryText();
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
                Position = PanelDesignPosition,
                Size = new Vector2(920f, 1260f)
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

            _titleLabel = CreateLabel(new Vector2(70f, 48f), new Vector2(780f, 72f), 60, HorizontalAlignment.Center);
            _titleLabel.Text = "潜行結果";
            _panel.AddChild(_titleLabel);

            _resultLabel = CreateLabel(new Vector2(70f, 132f), new Vector2(780f, 46f), 36, HorizontalAlignment.Center);
            _panel.AddChild(_resultLabel);

            _summaryLabel = CreateLabel(new Vector2(100f, 242f), new Vector2(720f, 660f), 40, HorizontalAlignment.Left);
            _panel.AddChild(_summaryLabel);

            _hintLabel = CreateLabel(new Vector2(70f, 1010f), new Vector2(780f, 76f), 28, HorizontalAlignment.Center);
            _hintLabel.Text = "タップ / クリック / キー入力で返済画面へ";
            _panel.AddChild(_hintLabel);

            _continueButton = new Button
            {
                Position = new Vector2(258f, 1114f),
                Size = new Vector2(404f, 96f),
                Text = "返済画面へ"
            };
            _continueButton.AddThemeFontSizeOverride("font_size", 36);
            _continueButton.Pressed += () => ContinueRequested?.Invoke();
            _panel.AddChild(_continueButton);

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