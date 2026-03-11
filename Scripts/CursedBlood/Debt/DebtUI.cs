using System;
using CursedBlood.Core;
using CursedBlood.UI;
using Godot;

namespace CursedBlood.Debt
{
    public partial class DebtUI : CanvasLayer
    {
        private static readonly Vector2 PanelDesignSize = new(932f, 1380f);

        private bool _built;
        private ColorRect _overlay;
        private Panel _panel;
        private Label _titleLabel;
        private ScrollContainer _summaryScroll;
        private Label _summaryLabel;
        private Label _optionHintLabel;
        private readonly Button[] _optionButtons = new Button[4];

        public event Action<DebtRepaymentChoice> RepaymentSelected;

        public void Initialize()
        {
            BuildUiIfNeeded();
            ApplyViewportLayout();
            HideScreen();
        }

        public void ShowScreen(DiveResultData result, DebtSettlementPreview preview)
        {
            BuildUiIfNeeded();
            ApplyViewportLayout();
            _titleLabel.Text = "借金精算";
            _summaryLabel.Text =
                $"潜行結果: {result.OutcomeLabel}\n" +
                $"持ち帰り額: {result.CarryValue:N0} / ロスト額: {result.LostValue:N0}\n" +
                $"回収費: {result.RescueCost:N0}\n" +
                (result.RescueCost > 0L ? $"内訳: {result.BuildRescueCostBreakdownText()}\n" : string.Empty) +
                $"精算前借金: {preview.DebtBeforeInterest:N0}\n" +
                $"今回利息: {preview.InterestAmount:N0}\n" +
                $"精算開始時借金: {preview.DebtAfterInterest:N0}\n" +
                $"現在所持金: {preview.MoneyBeforePayment:N0}";
            _optionHintLabel.Text = preview.SummaryText;
            CanvasLayoutHelper.UpdateScrollableLabelContent(_summaryLabel, _summaryScroll);

            for (var index = 0; index < _optionButtons.Length; index++)
            {
                var option = preview.Options[index];
                _optionButtons[index].Text = $"{option.Label}\n{option.Description}";
                _optionButtons[index].Disabled = !option.IsEnabled;
            }

            SetVisibleState(true);
        }

        public void HideScreen()
        {
            SetVisibleState(false);
        }

        public void ApplyViewportLayout()
        {
            if (!_built)
            {
                return;
            }

            CanvasLayoutHelper.StretchOverlay(this, _overlay);
            var panelRect = CanvasLayoutHelper.ResolveCenteredPanelRect(this, PanelDesignSize, 0.88f, 0.90f, 40f, 44f);
            _panel.Position = panelRect.Position;
            _panel.Size = panelRect.Size;

            var scale = CanvasLayoutHelper.GetScaleFactors(panelRect.Size, PanelDesignSize);
            CanvasLayoutHelper.ApplyScaledLayout(_titleLabel, new Vector2(70f, 42f), new Vector2(792f, 68f), scale);
            CanvasLayoutHelper.ApplyScaledLayout(_summaryScroll, new Vector2(104f, 142f), new Vector2(724f, 430f), scale);
            CanvasLayoutHelper.ApplyScaledLayout(_optionHintLabel, new Vector2(104f, 596f), new Vector2(724f, 104f), scale);

            for (var index = 0; index < _optionButtons.Length; index++)
            {
                CanvasLayoutHelper.ApplyScaledLayout(_optionButtons[index], new Vector2(104f, 736f + (index * 154f)), new Vector2(724f, 132f), scale);
                _optionButtons[index].AddThemeFontSizeOverride("font_size", CanvasLayoutHelper.ScaleFont(28, scale, 16, 40));
            }

            _titleLabel.AddThemeFontSizeOverride("font_size", CanvasLayoutHelper.ScaleFont(58, scale, 34, 84));
            _summaryLabel.AddThemeFontSizeOverride("font_size", CanvasLayoutHelper.ScaleFont(34, scale, 18, 48));
            _optionHintLabel.AddThemeFontSizeOverride("font_size", CanvasLayoutHelper.ScaleFont(28, scale, 16, 40));
            CanvasLayoutHelper.UpdateScrollableLabelContent(_summaryLabel, _summaryScroll);
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
                Position = Vector2.Zero,
                Size = PanelDesignSize
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

            _titleLabel = CreateLabel(new Vector2(70f, 42f), new Vector2(792f, 68f), 58, HorizontalAlignment.Center);
            _panel.AddChild(_titleLabel);

            _summaryScroll = new ScrollContainer
            {
                Position = new Vector2(104f, 142f),
                Size = new Vector2(724f, 430f)
            };
            _panel.AddChild(_summaryScroll);

            _summaryLabel = CreateLabel(Vector2.Zero, new Vector2(724f, 430f), 34, HorizontalAlignment.Left);
            _summaryLabel.VerticalAlignment = VerticalAlignment.Top;
            _summaryScroll.AddChild(_summaryLabel);

            _optionHintLabel = CreateLabel(new Vector2(104f, 596f), new Vector2(724f, 104f), 28, HorizontalAlignment.Left);
            _optionHintLabel.VerticalAlignment = VerticalAlignment.Top;
            _panel.AddChild(_optionHintLabel);

            for (var index = 0; index < _optionButtons.Length; index++)
            {
                var button = new Button
                {
                    Position = new Vector2(104f, 736f + index * 154f),
                    Size = new Vector2(724f, 132f),
                    ClipText = true,
                    Alignment = HorizontalAlignment.Left,
                    Text = string.Empty
                };
                button.AddThemeFontSizeOverride("font_size", 28);
                var capturedIndex = index;
                button.Pressed += () => RepaymentSelected?.Invoke((DebtRepaymentChoice)capturedIndex);
                _optionButtons[index] = button;
                _panel.AddChild(button);
            }

            _built = true;
            ApplyViewportLayout();
        }

        private void SetVisibleState(bool visible)
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