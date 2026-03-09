using Godot;

namespace CursedBlood.Curse
{
    public partial class CurseResearchUI : CanvasLayer
    {
        private bool _uiBuilt;
        private ColorRect _overlay;
        private Panel _panel;
        private Label _contentLabel;

        public bool IsOpen => _panel?.Visible == true;

        public void SetData(CurseResearchManager manager)
        {
            BuildUiIfNeeded();
            var nextThreshold = ((manager.TotalPoints / 100) + 1) * 100;
            _contentLabel.Text =
                $"研究度: {manager.TotalPoints}\n" +
                $"寿命ボーナス: +{manager.BonusYears}歳 / +{manager.BonusSeconds:F1}秒\n" +
                $"次の閾値: {nextThreshold}\n" +
                $"エンディング到達: {(manager.EndingCleared ? "済み" : "未到達")}";
        }

        public void Toggle()
        {
            BuildUiIfNeeded();
            var visible = !_panel.Visible;
            _overlay.Visible = visible;
            _panel.Visible = visible;
        }

        private void BuildUiIfNeeded()
        {
            if (_uiBuilt)
            {
                return;
            }

            _overlay = new ColorRect
            {
                Position = Vector2.Zero,
                Size = new Vector2(1080f, 1920f),
                Color = new Color(0f, 0f, 0f, 0.55f),
                Visible = false
            };
            AddChild(_overlay);

            _panel = new Panel
            {
                Position = new Vector2(180f, 520f),
                Size = new Vector2(720f, 360f),
                Visible = false
            };
            AddChild(_panel);

            var title = new Label
            {
                Position = new Vector2(28f, 18f),
                Size = new Vector2(500f, 36f),
                Text = "呪い研究"
            };
            title.AddThemeFontSizeOverride("font_size", 28);
            _panel.AddChild(title);

            var close = new Button
            {
                Position = new Vector2(560f, 18f),
                Size = new Vector2(110f, 38f),
                Text = "閉じる"
            };
            close.Pressed += Toggle;
            _panel.AddChild(close);

            _contentLabel = new Label
            {
                Position = new Vector2(28f, 80f),
                Size = new Vector2(660f, 240f),
                AutowrapMode = TextServer.AutowrapMode.WordSmart
            };
            _contentLabel.AddThemeFontSizeOverride("font_size", 22);
            _panel.AddChild(_contentLabel);

            _uiBuilt = true;
        }
    }
}