using System;
using CursedBlood.Core;
using Godot;

namespace CursedBlood.UI
{
    public partial class ResultScreen : CanvasLayer
    {
        private static readonly Vector2 PanelDesignSize = new(920f, 1260f);

        private bool _built;
        private ColorRect _overlay;
        private Panel _panel;
        private Label _titleLabel;
        private Label _resultLabel;
        private ScrollContainer _summaryScroll;
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
            CanvasLayoutHelper.UpdateScrollableLabelContent(_summaryLabel, _summaryScroll);
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
            var panelRect = CanvasLayoutHelper.ResolveCenteredPanelRect(this, PanelDesignSize, 0.88f, 0.84f, 40f, 48f);
            _panel.Position = panelRect.Position;
            _panel.Size = panelRect.Size;

            var scale = CanvasLayoutHelper.GetScaleFactors(panelRect.Size, PanelDesignSize);
            CanvasLayoutHelper.ApplyScaledLayout(_titleLabel, new Vector2(70f, 48f), new Vector2(780f, 72f), scale);
            CanvasLayoutHelper.ApplyScaledLayout(_resultLabel, new Vector2(70f, 132f), new Vector2(780f, 46f), scale);
            CanvasLayoutHelper.ApplyScaledLayout(_summaryScroll, new Vector2(100f, 242f), new Vector2(720f, 694f), scale);
            CanvasLayoutHelper.ApplyScaledLayout(_hintLabel, new Vector2(70f, 972f), new Vector2(780f, 80f), scale);
            CanvasLayoutHelper.ApplyScaledLayout(_continueButton, new Vector2(258f, 1108f), new Vector2(404f, 96f), scale);

            _titleLabel.AddThemeFontSizeOverride("font_size", CanvasLayoutHelper.ScaleFont(60, scale, 34, 86));
            _resultLabel.AddThemeFontSizeOverride("font_size", CanvasLayoutHelper.ScaleFont(36, scale, 20, 54));
            _summaryLabel.AddThemeFontSizeOverride("font_size", CanvasLayoutHelper.ScaleFont(36, scale, 20, 52));
            _hintLabel.AddThemeFontSizeOverride("font_size", CanvasLayoutHelper.ScaleFont(28, scale, 16, 40));
            _continueButton.AddThemeFontSizeOverride("font_size", CanvasLayoutHelper.ScaleFont(36, scale, 22, 52));
            CanvasLayoutHelper.UpdateScrollableLabelContent(_summaryLabel, _summaryScroll);
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

            _summaryScroll = new ScrollContainer
            {
                Position = new Vector2(100f, 242f),
                Size = new Vector2(720f, 694f)
            };
            _panel.AddChild(_summaryScroll);

            _summaryLabel = CreateLabel(Vector2.Zero, new Vector2(720f, 694f), 36, HorizontalAlignment.Left);
            _summaryLabel.VerticalAlignment = VerticalAlignment.Top;
            _summaryScroll.AddChild(_summaryLabel);

            _hintLabel = CreateLabel(new Vector2(70f, 972f), new Vector2(780f, 80f), 28, HorizontalAlignment.Center);
            _hintLabel.Text = "タップ / クリック / キー入力で返済画面へ";
            _panel.AddChild(_hintLabel);

            _continueButton = new Button
            {
                Position = new Vector2(258f, 1108f),
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