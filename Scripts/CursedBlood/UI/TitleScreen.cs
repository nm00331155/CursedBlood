using System;
using CursedBlood.Save;
using Godot;

namespace CursedBlood.UI
{
    public partial class TitleScreen : CanvasLayer
    {
        private bool _built;
        private ColorRect _background;
        private Panel _panel;
        private Label _titleLabel;
        private Label _subtitleLabel;
        private Label _summaryLabel;
        private Button _primaryButton;
        private Button _profileButton;

        public event Action PrimaryRequested;

        public event Action ProfileRequested;

        public void Initialize()
        {
            BuildUiIfNeeded();
            HideScreen();
        }

        public void ShowScreen(PlayerProfileData profile, DebtData debt, RankingData ranking)
        {
            BuildUiIfNeeded();
            var hasProfile = profile.IsProfileConfigured;
            _summaryLabel.Text = hasProfile
                ? $"主人公: {profile.Name} / {profile.Gender}\n潜行回数: {profile.TotalDiveCount}\n借金残高: {debt.CurrentDebt:N0}\n最高深度: {ranking.BestDepth}m"
                : "新規開始では主人公の性別と名前を仮設定します。\n立ち絵は未実装のため、次画面では仮カードで表示します。";
            _primaryButton.Text = hasProfile ? "続きから" : "新規開始";
            _profileButton.Visible = hasProfile;
            SetVisibleState(true);
            _primaryButton.GrabFocus();
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
                Color = new Color(0.07f, 0.08f, 0.11f, 1f)
            };
            AddChild(_background);

            _panel = new Panel
            {
                Position = new Vector2(100f, 220f),
                Size = new Vector2(880f, 1240f)
            };
            var panelStyle = new StyleBoxFlat
            {
                BgColor = new Color(0.10f, 0.12f, 0.18f, 0.96f),
                BorderColor = new Color(0.82f, 0.88f, 0.94f),
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

            _titleLabel = CreateLabel(new Vector2(80f, 80f), new Vector2(720f, 90f), 68, HorizontalAlignment.Center);
            _titleLabel.Text = "CursedBlood";
            _panel.AddChild(_titleLabel);

            _subtitleLabel = CreateLabel(new Vector2(120f, 188f), new Vector2(640f, 64f), 28, HorizontalAlignment.Center);
            _subtitleLabel.Text = "60秒だけ潜って、借金を返す。";
            _panel.AddChild(_subtitleLabel);

            _summaryLabel = CreateLabel(new Vector2(150f, 380f), new Vector2(580f, 220f), 28, HorizontalAlignment.Left);
            _panel.AddChild(_summaryLabel);

            _primaryButton = new Button
            {
                Position = new Vector2(250f, 840f),
                Size = new Vector2(380f, 92f),
                Text = "続きから"
            };
            _primaryButton.AddThemeFontSizeOverride("font_size", 32);
            _primaryButton.Pressed += () => PrimaryRequested?.Invoke();
            _panel.AddChild(_primaryButton);

            _profileButton = new Button
            {
                Position = new Vector2(250f, 960f),
                Size = new Vector2(380f, 72f),
                Text = "主人公設定を確認"
            };
            _profileButton.AddThemeFontSizeOverride("font_size", 24);
            _profileButton.Pressed += () => ProfileRequested?.Invoke();
            _panel.AddChild(_profileButton);

            var noteLabel = CreateLabel(new Vector2(140f, 1120f), new Vector2(600f, 130f), 22, HorizontalAlignment.Left);
            noteLabel.Text = "現在の実装範囲: タイトル → 主人公選択 → 拠点 → 潜行 → 結果 → 借金精算 → 拠点\n装備・研究・実績・ランキングは拠点内で仮配置です。";
            _panel.AddChild(noteLabel);

            _built = true;
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