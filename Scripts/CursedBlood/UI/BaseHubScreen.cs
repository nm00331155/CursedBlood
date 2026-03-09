using System;
using System.Text;
using CursedBlood.Save;
using Godot;

namespace CursedBlood.UI
{
    public partial class BaseHubScreen : CanvasLayer
    {
        private bool _built;
        private ColorRect _background;
        private Panel _panel;
        private Label _profileLabel;
        private Label _moneyLabel;
        private Label _debtLabel;
        private Label _recordsLabel;
        private Label _messageLabel;
        private Button _startButton;

        public event Action StartDiveRequested;

        public event Action ProfileEditRequested;

        public event Action ReturnToTitleRequested;

        public void Initialize()
        {
            BuildUiIfNeeded();
            HideScreen();
        }

        public void ShowScreen(SaveData data, string message)
        {
            BuildUiIfNeeded();
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
                Position = new Vector2(70f, 90f),
                Size = new Vector2(940f, 1680f)
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

            var title = CreateLabel(new Vector2(70f, 40f), new Vector2(760f, 52f), 44, HorizontalAlignment.Center);
            title.Text = "拠点 / 準備";
            _panel.AddChild(title);

            _profileLabel = CreateLabel(new Vector2(70f, 130f), new Vector2(360f, 100f), 28, HorizontalAlignment.Left);
            _panel.AddChild(_profileLabel);

            _moneyLabel = CreateLabel(new Vector2(520f, 130f), new Vector2(300f, 42f), 30, HorizontalAlignment.Right);
            _panel.AddChild(_moneyLabel);

            _debtLabel = CreateLabel(new Vector2(520f, 180f), new Vector2(300f, 90f), 22, HorizontalAlignment.Right);
            _panel.AddChild(_debtLabel);

            _startButton = new Button
            {
                Position = new Vector2(250f, 320f),
                Size = new Vector2(440f, 96f),
                Text = "次の潜行へ出発"
            };
            _startButton.AddThemeFontSizeOverride("font_size", 34);
            _startButton.Pressed += () => StartDiveRequested?.Invoke();
            _panel.AddChild(_startButton);

            var profileButton = new Button
            {
                Position = new Vector2(250f, 434f),
                Size = new Vector2(440f, 66f),
                Text = "主人公設定を開く"
            };
            profileButton.Pressed += () => ProfileEditRequested?.Invoke();
            _panel.AddChild(profileButton);

            var placeholderLabel = CreateLabel(new Vector2(110f, 560f), new Vector2(720f, 40f), 24, HorizontalAlignment.Left);
            placeholderLabel.Text = "仮配置メニュー";
            _panel.AddChild(placeholderLabel);

            AddPlaceholderButton(new Vector2(110f, 620f), "装備変更", "装備画面は次段階で接続予定です。");
            AddPlaceholderButton(new Vector2(470f, 620f), "研究", "研究画面は次段階で接続予定です。");
            AddPlaceholderButton(new Vector2(110f, 710f), "実績", "実績画面は次段階で接続予定です。");
            AddPlaceholderButton(new Vector2(470f, 710f), "ランキング", "ランキング画面は次段階で接続予定です。");
            AddPlaceholderButton(new Vector2(110f, 800f), "設定", "設定画面は次段階で接続予定です。");

            var recordsTitle = CreateLabel(new Vector2(110f, 910f), new Vector2(720f, 40f), 24, HorizontalAlignment.Left);
            recordsTitle.Text = "最近の潜行記録";
            _panel.AddChild(recordsTitle);

            _recordsLabel = CreateLabel(new Vector2(110f, 960f), new Vector2(720f, 360f), 22, HorizontalAlignment.Left);
            _panel.AddChild(_recordsLabel);

            _messageLabel = CreateLabel(new Vector2(110f, 1340f), new Vector2(720f, 120f), 22, HorizontalAlignment.Left);
            _panel.AddChild(_messageLabel);

            var titleButton = new Button
            {
                Position = new Vector2(250f, 1500f),
                Size = new Vector2(440f, 68f),
                Text = "タイトルへ戻る"
            };
            titleButton.Pressed += () => ReturnToTitleRequested?.Invoke();
            _panel.AddChild(titleButton);

            _built = true;
        }

        private void AddPlaceholderButton(Vector2 position, string text, string message)
        {
            var button = new Button
            {
                Position = position,
                Size = new Vector2(300f, 70f),
                Text = text
            };
            button.Pressed += () => _messageLabel.Text = message;
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