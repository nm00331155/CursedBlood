using System;
using CursedBlood.Save;
using Godot;

namespace CursedBlood.UI
{
    public partial class TitleScreen : CanvasLayer
    {
        private static readonly Vector2 PanelDesignSize = new(924f, 1320f);

        private bool _built;
        private ColorRect _background;
        private Panel _panel;
        private Label _titleLabel;
        private Label _subtitleLabel;
        private Label _summaryLabel;
        private Button _primaryButton;
        private Button _profileButton;
        private Label _noteLabel;

        public event Action PrimaryRequested;

        public event Action ProfileRequested;

        public void Initialize()
        {
            BuildUiIfNeeded();
            ApplyViewportLayout();
            HideScreen();
        }

        public void ShowScreen(PlayerProfileData profile, DebtData debt, RankingData ranking)
        {
            BuildUiIfNeeded();
            ApplyViewportLayout();
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

        public void ApplyViewportLayout()
        {
            if (!_built)
            {
                return;
            }

            CanvasLayoutHelper.StretchOverlay(this, _background);
            var panelRect = CanvasLayoutHelper.ResolveCenteredPanelRect(this, PanelDesignSize, 0.88f, 0.84f, 40f, 48f);
            _panel.Position = panelRect.Position;
            _panel.Size = panelRect.Size;

            var scale = CanvasLayoutHelper.GetScaleFactors(panelRect.Size, PanelDesignSize);
            CanvasLayoutHelper.ApplyScaledLayout(_titleLabel, new Vector2(72f, 72f), new Vector2(780f, 96f), scale);
            CanvasLayoutHelper.ApplyScaledLayout(_subtitleLabel, new Vector2(92f, 188f), new Vector2(740f, 72f), scale);
            CanvasLayoutHelper.ApplyScaledLayout(_summaryLabel, new Vector2(118f, 380f), new Vector2(690f, 270f), scale);
            CanvasLayoutHelper.ApplyScaledLayout(_primaryButton, new Vector2(242f, 920f), new Vector2(440f, 108f), scale);
            CanvasLayoutHelper.ApplyScaledLayout(_profileButton, new Vector2(242f, 1056f), new Vector2(440f, 82f), scale);
            CanvasLayoutHelper.ApplyScaledLayout(_noteLabel, new Vector2(112f, 1128f), new Vector2(700f, 150f), scale);

            _titleLabel.AddThemeFontSizeOverride("font_size", CanvasLayoutHelper.ScaleFont(84, scale, 42, 108));
            _subtitleLabel.AddThemeFontSizeOverride("font_size", CanvasLayoutHelper.ScaleFont(36, scale, 20, 52));
            _summaryLabel.AddThemeFontSizeOverride("font_size", CanvasLayoutHelper.ScaleFont(34, scale, 18, 46));
            _primaryButton.AddThemeFontSizeOverride("font_size", CanvasLayoutHelper.ScaleFont(40, scale, 24, 56));
            _profileButton.AddThemeFontSizeOverride("font_size", CanvasLayoutHelper.ScaleFont(30, scale, 18, 42));
            _noteLabel.AddThemeFontSizeOverride("font_size", CanvasLayoutHelper.ScaleFont(28, scale, 16, 38));
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
                Position = Vector2.Zero,
                Size = PanelDesignSize
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

            _titleLabel = CreateLabel(new Vector2(72f, 72f), new Vector2(780f, 96f), 84, HorizontalAlignment.Center);
            _titleLabel.Text = "CursedBlood";
            _panel.AddChild(_titleLabel);

            _subtitleLabel = CreateLabel(new Vector2(92f, 188f), new Vector2(740f, 72f), 36, HorizontalAlignment.Center);
            _subtitleLabel.Text = "60秒だけ潜って、借金を返す。";
            _panel.AddChild(_subtitleLabel);

            _summaryLabel = CreateLabel(new Vector2(118f, 380f), new Vector2(690f, 270f), 34, HorizontalAlignment.Left);
            _summaryLabel.VerticalAlignment = VerticalAlignment.Top;
            _panel.AddChild(_summaryLabel);

            _primaryButton = new Button
            {
                Position = new Vector2(242f, 920f),
                Size = new Vector2(440f, 108f),
                Text = "続きから"
            };
            _primaryButton.AddThemeFontSizeOverride("font_size", 40);
            _primaryButton.Pressed += () => PrimaryRequested?.Invoke();
            _panel.AddChild(_primaryButton);

            _profileButton = new Button
            {
                Position = new Vector2(242f, 1056f),
                Size = new Vector2(440f, 82f),
                Text = "主人公設定を確認"
            };
            _profileButton.AddThemeFontSizeOverride("font_size", 30);
            _profileButton.Pressed += () => ProfileRequested?.Invoke();
            _panel.AddChild(_profileButton);

            _noteLabel = CreateLabel(new Vector2(112f, 1128f), new Vector2(700f, 150f), 28, HorizontalAlignment.Left);
            _noteLabel.VerticalAlignment = VerticalAlignment.Top;
            _noteLabel.Text = "現在の実装範囲: タイトル → 主人公選択 → 拠点 → 潜行 → 結果 → 借金精算 → 拠点\n装備・研究・実績・ランキングは拠点内で仮配置です。";
            _panel.AddChild(_noteLabel);

            _built = true;
            ApplyViewportLayout();
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