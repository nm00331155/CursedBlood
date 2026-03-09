using System;
using CursedBlood.Core;
using Godot;

namespace CursedBlood.Debt
{
    public partial class DebtUI : CanvasLayer
    {
        private bool _built;
        private ColorRect _overlay;
        private Panel _panel;
        private Label _titleLabel;
        private Label _summaryLabel;
        private Label _optionHintLabel;
        private readonly Button[] _optionButtons = new Button[4];

        public event Action<DebtRepaymentChoice> RepaymentSelected;

        public void Initialize()
        {
            BuildUiIfNeeded();
            HideScreen();
        }

        public void ShowScreen(DiveResultData result, DebtSettlementPreview preview)
        {
            BuildUiIfNeeded();
            _titleLabel.Text = "借金精算";
            _summaryLabel.Text =
                $"潜行結果: {result.OutcomeLabel}\n" +
                $"持ち帰り額: {result.CarryValue:N0} / ロスト額: {result.LostValue:N0}\n" +
                $"回収費: {result.RescueCost:N0}\n" +
                $"精算前借金: {preview.DebtBeforeInterest:N0}\n" +
                $"今回利息: {preview.InterestAmount:N0}\n" +
                $"精算開始時借金: {preview.DebtAfterInterest:N0}\n" +
                $"現在所持金: {preview.MoneyBeforePayment:N0}";
            _optionHintLabel.Text = preview.SummaryText;

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
                Position = new Vector2(100f, 180f),
                Size = new Vector2(880f, 1280f)
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

            _titleLabel = CreateLabel(new Vector2(80f, 44f), new Vector2(720f, 56f), 48, HorizontalAlignment.Center);
            _panel.AddChild(_titleLabel);

            _summaryLabel = CreateLabel(new Vector2(120f, 140f), new Vector2(640f, 340f), 30, HorizontalAlignment.Left);
            _panel.AddChild(_summaryLabel);

            _optionHintLabel = CreateLabel(new Vector2(120f, 500f), new Vector2(640f, 80f), 22, HorizontalAlignment.Left);
            _panel.AddChild(_optionHintLabel);

            for (var index = 0; index < _optionButtons.Length; index++)
            {
                var button = new Button
                {
                    Position = new Vector2(120f, 620f + index * 138f),
                    Size = new Vector2(640f, 112f),
                    ClipText = true,
                    Alignment = HorizontalAlignment.Left,
                    Text = string.Empty
                };
                button.AddThemeFontSizeOverride("font_size", 22);
                var capturedIndex = index;
                button.Pressed += () => RepaymentSelected?.Invoke((DebtRepaymentChoice)capturedIndex);
                _optionButtons[index] = button;
                _panel.AddChild(button);
            }

            _built = true;
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