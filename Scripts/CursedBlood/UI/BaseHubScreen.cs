using System;
using System.Text;
using CursedBlood.Save;
using Godot;

namespace CursedBlood.UI
{
    public partial class BaseHubScreen : CanvasLayer
    {
        private static readonly Vector2 PanelDesignSize = new(972f, 1740f);

        private bool _built;
        private ColorRect _background;
        private Panel _panel;
        private Label _titleLabel;
        private Label _profileLabel;
        private Label _moneyLabel;
        private Label _debtLabel;
        private Label _placeholderLabel;
        private Label _recordsTitleLabel;
        private Label _recordsLabel;
        private Label _messageLabel;
        private Button _startButton;
        private Button _profileButton;
        private Button _titleButton;
        private readonly Button[] _placeholderButtons = new Button[5];

        public event Action StartDiveRequested;

        public event Action ProfileEditRequested;

        public event Action ReturnToTitleRequested;

        public void Initialize()
        {
            BuildUiIfNeeded();
            ApplyViewportLayout();
            HideScreen();
        }

        public void ShowScreen(SaveData data, string message)
        {
            BuildUiIfNeeded();
            ApplyViewportLayout();
            _profileLabel.Text = $"主人公: {data.PlayerProfile.Name} / {data.PlayerProfile.Gender}\n総潜行回数: {data.PlayerProfile.TotalDiveCount}";
            _moneyLabel.Text = $"所持金 {data.PlayerProfile.CurrentMoney:N0}";
            _debtLabel.Text = data.Debt.DebtCleared
                ? "借金完済済み / 深層ライセンス要素は準備中"
                : $"借金残高 {data.Debt.CurrentDebt:N0}\n総返済額 {data.Debt.TotalRepaid:N0} / 総救助費 {data.Debt.TotalRescueCost:N0}";
            _recordsLabel.Text = BuildRecordSummary(data);
            _messageLabel.Text = string.IsNullOrWhiteSpace(message)
                ? "拠点では次の潜行準備ができます。装備・研究・実績・ランキングは仮配置です。"
                : message;
            SetVisibleState(true);
            _startButton.GrabFocus();
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

            CanvasLayoutHelper.StretchOverlay(this, _background);
            var panelRect = CanvasLayoutHelper.ResolveCenteredPanelRect(this, PanelDesignSize, 0.92f, 0.94f, 36f, 40f);
            _panel.Position = panelRect.Position;
            _panel.Size = panelRect.Size;

            var scale = CanvasLayoutHelper.GetScaleFactors(panelRect.Size, PanelDesignSize);
            CanvasLayoutHelper.ApplyScaledLayout(_titleLabel, new Vector2(62f, 32f), new Vector2(848f, 66f), scale);
            CanvasLayoutHelper.ApplyScaledLayout(_profileLabel, new Vector2(70f, 132f), new Vector2(420f, 120f), scale);
            CanvasLayoutHelper.ApplyScaledLayout(_moneyLabel, new Vector2(552f, 132f), new Vector2(320f, 48f), scale);
            CanvasLayoutHelper.ApplyScaledLayout(_debtLabel, new Vector2(522f, 188f), new Vector2(350f, 108f), scale);
            CanvasLayoutHelper.ApplyScaledLayout(_startButton, new Vector2(250f, 346f), new Vector2(460f, 106f), scale);
            CanvasLayoutHelper.ApplyScaledLayout(_profileButton, new Vector2(250f, 470f), new Vector2(460f, 74f), scale);
            CanvasLayoutHelper.ApplyScaledLayout(_placeholderLabel, new Vector2(110f, 602f), new Vector2(720f, 40f), scale);
            CanvasLayoutHelper.ApplyScaledLayout(_placeholderButtons[0], new Vector2(110f, 662f), new Vector2(320f, 76f), scale);
            CanvasLayoutHelper.ApplyScaledLayout(_placeholderButtons[1], new Vector2(486f, 662f), new Vector2(320f, 76f), scale);
            CanvasLayoutHelper.ApplyScaledLayout(_placeholderButtons[2], new Vector2(110f, 758f), new Vector2(320f, 76f), scale);
            CanvasLayoutHelper.ApplyScaledLayout(_placeholderButtons[3], new Vector2(486f, 758f), new Vector2(320f, 76f), scale);
            CanvasLayoutHelper.ApplyScaledLayout(_placeholderButtons[4], new Vector2(110f, 854f), new Vector2(320f, 76f), scale);
            CanvasLayoutHelper.ApplyScaledLayout(_recordsTitleLabel, new Vector2(110f, 980f), new Vector2(720f, 40f), scale);
            CanvasLayoutHelper.ApplyScaledLayout(_recordsLabel, new Vector2(110f, 1038f), new Vector2(760f, 390f), scale);
            CanvasLayoutHelper.ApplyScaledLayout(_messageLabel, new Vector2(110f, 1450f), new Vector2(760f, 150f), scale);
            CanvasLayoutHelper.ApplyScaledLayout(_titleButton, new Vector2(250f, 1632f), new Vector2(460f, 78f), scale);

            _titleLabel.AddThemeFontSizeOverride("font_size", CanvasLayoutHelper.ScaleFont(56, scale, 30, 80));
            _profileLabel.AddThemeFontSizeOverride("font_size", CanvasLayoutHelper.ScaleFont(34, scale, 18, 46));
            _moneyLabel.AddThemeFontSizeOverride("font_size", CanvasLayoutHelper.ScaleFont(36, scale, 20, 50));
            _debtLabel.AddThemeFontSizeOverride("font_size", CanvasLayoutHelper.ScaleFont(28, scale, 16, 40));
            _startButton.AddThemeFontSizeOverride("font_size", CanvasLayoutHelper.ScaleFont(40, scale, 24, 54));
            _profileButton.AddThemeFontSizeOverride("font_size", CanvasLayoutHelper.ScaleFont(26, scale, 16, 38));
            _placeholderLabel.AddThemeFontSizeOverride("font_size", CanvasLayoutHelper.ScaleFont(30, scale, 18, 40));
            _recordsTitleLabel.AddThemeFontSizeOverride("font_size", CanvasLayoutHelper.ScaleFont(30, scale, 18, 40));
            _recordsLabel.AddThemeFontSizeOverride("font_size", CanvasLayoutHelper.ScaleFont(28, scale, 16, 38));
            _messageLabel.AddThemeFontSizeOverride("font_size", CanvasLayoutHelper.ScaleFont(28, scale, 16, 38));
            _titleButton.AddThemeFontSizeOverride("font_size", CanvasLayoutHelper.ScaleFont(26, scale, 16, 38));
            for (var index = 0; index < _placeholderButtons.Length; index++)
            {
                _placeholderButtons[index].AddThemeFontSizeOverride("font_size", CanvasLayoutHelper.ScaleFont(24, scale, 14, 34));
            }
        }

        private void BuildUiIfNeeded()
        {
            if (_built)
            {
                return;
            }

            _background = new ColorRect
            {
                Position = Vector2.Zero,
                Size = new Vector2(1080f, 1920f),
                Color = new Color(0.08f, 0.09f, 0.12f, 1f)
            };
            AddChild(_background);

            _panel = new Panel
            {
                Position = Vector2.Zero,
                Size = PanelDesignSize
            };
            var panelStyle = new StyleBoxFlat
            {
                BgColor = new Color(0.10f, 0.12f, 0.16f, 0.96f),
                BorderColor = new Color(0.84f, 0.90f, 0.96f),
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

            _titleLabel = CreateLabel(new Vector2(62f, 32f), new Vector2(848f, 66f), 56, HorizontalAlignment.Center);
            _titleLabel.Text = "拠点 / 準備";
            _panel.AddChild(_titleLabel);

            _profileLabel = CreateLabel(new Vector2(70f, 132f), new Vector2(420f, 120f), 34, HorizontalAlignment.Left);
            _profileLabel.VerticalAlignment = VerticalAlignment.Top;
            _panel.AddChild(_profileLabel);

            _moneyLabel = CreateLabel(new Vector2(552f, 132f), new Vector2(320f, 48f), 36, HorizontalAlignment.Right);
            _panel.AddChild(_moneyLabel);

            _debtLabel = CreateLabel(new Vector2(522f, 188f), new Vector2(350f, 108f), 28, HorizontalAlignment.Right);
            _debtLabel.VerticalAlignment = VerticalAlignment.Top;
            _panel.AddChild(_debtLabel);

            _startButton = new Button
            {
                Position = new Vector2(250f, 346f),
                Size = new Vector2(460f, 106f),
                Text = "次の潜行へ出発"
            };
            _startButton.AddThemeFontSizeOverride("font_size", 40);
            _startButton.Pressed += () => StartDiveRequested?.Invoke();
            _panel.AddChild(_startButton);

            _profileButton = new Button
            {
                Position = new Vector2(250f, 470f),
                Size = new Vector2(460f, 74f),
                Text = "主人公設定を開く"
            };
            _profileButton.AddThemeFontSizeOverride("font_size", 26);
            _profileButton.Pressed += () => ProfileEditRequested?.Invoke();
            _panel.AddChild(_profileButton);

            _placeholderLabel = CreateLabel(new Vector2(110f, 602f), new Vector2(720f, 40f), 30, HorizontalAlignment.Left);
            _placeholderLabel.Text = "仮配置メニュー";
            _panel.AddChild(_placeholderLabel);

            AddPlaceholderButton(0, new Vector2(110f, 662f), "装備変更", "装備画面は次段階で接続予定です。");
            AddPlaceholderButton(1, new Vector2(486f, 662f), "研究", "研究画面は次段階で接続予定です。");
            AddPlaceholderButton(2, new Vector2(110f, 758f), "実績", "実績画面は次段階で接続予定です。");
            AddPlaceholderButton(3, new Vector2(486f, 758f), "ランキング", "ランキング画面は次段階で接続予定です。");
            AddPlaceholderButton(4, new Vector2(110f, 854f), "設定", "設定画面は次段階で接続予定です。");

            _recordsTitleLabel = CreateLabel(new Vector2(110f, 980f), new Vector2(720f, 40f), 30, HorizontalAlignment.Left);
            _recordsTitleLabel.Text = "最近の潜行記録";
            _panel.AddChild(_recordsTitleLabel);

            _recordsLabel = CreateLabel(new Vector2(110f, 1038f), new Vector2(760f, 390f), 28, HorizontalAlignment.Left);
            _recordsLabel.VerticalAlignment = VerticalAlignment.Top;
            _panel.AddChild(_recordsLabel);

            _messageLabel = CreateLabel(new Vector2(110f, 1450f), new Vector2(760f, 150f), 28, HorizontalAlignment.Left);
            _messageLabel.VerticalAlignment = VerticalAlignment.Top;
            _panel.AddChild(_messageLabel);

            _titleButton = new Button
            {
                Position = new Vector2(250f, 1632f),
                Size = new Vector2(460f, 78f),
                Text = "タイトルへ戻る"
            };
            _titleButton.AddThemeFontSizeOverride("font_size", 26);
            _titleButton.Pressed += () => ReturnToTitleRequested?.Invoke();
            _panel.AddChild(_titleButton);

            _built = true;
            ApplyViewportLayout();
        }

        private void AddPlaceholderButton(int index, Vector2 position, string text, string message)
        {
            var button = new Button
            {
                Position = position,
                Size = new Vector2(320f, 76f),
                Text = text
            };
            button.AddThemeFontSizeOverride("font_size", 24);
            button.Pressed += () => _messageLabel.Text = message;
            _placeholderButtons[index] = button;
            _panel.AddChild(button);
        }

        private static string BuildRecordSummary(SaveData data)
        {
            if (data.Records.DiveRecords.Count == 0)
            {
                return "まだ潜行記録はありません。最初の潜行で地下の様子を確認してください。";
            }

            var builder = new StringBuilder();
            var startIndex = Math.Max(0, data.Records.DiveRecords.Count - 4);
            for (var index = data.Records.DiveRecords.Count - 1; index >= startIndex; index--)
            {
                var record = data.Records.DiveRecords[index];
                var outcome = record.ReturnedSafely ? "帰還成功" : "救助";
                builder.Append($"#{record.DiveCount} {outcome} / 深度 {record.MaxDepthMeters}m / 持帰 {record.CarryValue:N0} / 借金差 {record.DebtChange:N0}");
                if (index > startIndex)
                {
                    builder.Append('\n');
                }
            }

            return builder.ToString();
        }

        private void SetVisibleState(bool visible)
        {
            if (!_built)
            {
                return;
            }

            _background.Visible = visible;
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