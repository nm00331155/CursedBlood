using Godot;

namespace CursedBlood.Generation
{
    public partial class FamilyTreeUI : CanvasLayer
    {
        private bool _uiBuilt;
        private ColorRect _overlay;
        private Panel _panel;
        private Label _contentLabel;

        public bool IsOpen => _panel?.Visible == true;

        public void SetFamilyTree(FamilyTree familyTree)
        {
            BuildUiIfNeeded();
            _contentLabel.Text = string.Empty;
            foreach (var record in familyTree.Records)
            {
                var gender = record.IsMale ? "男" : "女";
                _contentLabel.Text += $"第{record.Generation}世代 {record.Name} ({gender}) 深度:{record.MaxDepth} スコア:{record.Score:N0} 享年:{record.HumanAge}\n";
            }
        }

        public void Toggle()
        {
            BuildUiIfNeeded();
            SetVisibleState(!IsOpen);
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
                Color = new Color(0f, 0f, 0f, 0.6f)
            };
            AddChild(_overlay);

            _panel = new Panel
            {
                Position = new Vector2(120f, 220f),
                Size = new Vector2(840f, 1180f)
            };
            AddChild(_panel);

            var title = new Label
            {
                Position = new Vector2(32f, 22f),
                Size = new Vector2(500f, 40f),
                Text = "家系図"
            };
            title.AddThemeFontSizeOverride("font_size", 30);
            _panel.AddChild(title);

            var close = new Button
            {
                Position = new Vector2(660f, 22f),
                Size = new Vector2(120f, 40f),
                Text = "閉じる"
            };
            close.Pressed += Toggle;
            _panel.AddChild(close);

            _contentLabel = new Label
            {
                Position = new Vector2(32f, 90f),
                Size = new Vector2(760f, 1040f),
                AutowrapMode = TextServer.AutowrapMode.WordSmart
            };
            _contentLabel.AddThemeFontSizeOverride("font_size", 20);
            _panel.AddChild(_contentLabel);

            _uiBuilt = true;
            SetVisibleState(false);
        }

        private void SetVisibleState(bool visible)
        {
            _overlay.Visible = visible;
            _panel.Visible = visible;
        }
    }
}