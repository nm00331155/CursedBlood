using System.Collections.Generic;
using Godot;

namespace CursedBlood.Achievement
{
    public partial class AchievementUI : CanvasLayer
    {
        private AchievementCategory _currentCategory = AchievementCategory.Digging;
        private AchievementManager _achievementManager;
        private bool _uiBuilt;
        private ColorRect _overlay;
        private Panel _panel;
        private Label _contentLabel;
        private readonly Dictionary<AchievementCategory, Button> _tabButtons = new();

        public bool IsOpen => _panel?.Visible == true;

        public void SetData(AchievementManager achievementManager)
        {
            BuildUiIfNeeded();
            _achievementManager = achievementManager;
            RefreshContent();
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
                Position = new Vector2(120f, 200f),
                Size = new Vector2(840f, 1260f)
            };
            AddChild(_panel);

            var title = new Label
            {
                Position = new Vector2(32f, 22f),
                Size = new Vector2(500f, 40f),
                Text = "実績とランキング"
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

            AddTabButton("掘削", AchievementCategory.Digging, new Vector2(32f, 78f));
            AddTabButton("戦闘", AchievementCategory.Combat, new Vector2(212f, 78f));
            AddTabButton("収集", AchievementCategory.Collection, new Vector2(392f, 78f));
            AddTabButton("世代", AchievementCategory.Generation, new Vector2(572f, 78f));

            _contentLabel = new Label
            {
                Position = new Vector2(32f, 138f),
                Size = new Vector2(760f, 1072f),
                AutowrapMode = TextServer.AutowrapMode.WordSmart
            };
            _contentLabel.AddThemeFontSizeOverride("font_size", 20);
            _panel.AddChild(_contentLabel);

            _uiBuilt = true;
            SetVisibleState(false);
        }

        private void AddTabButton(string text, AchievementCategory category, Vector2 position)
        {
            var button = new Button
            {
                Position = position,
                Size = new Vector2(160f, 42f),
                Text = text
            };
            button.Pressed += () =>
            {
                _currentCategory = category;
                RefreshContent();
            };
            _tabButtons[category] = button;
            _panel.AddChild(button);
        }

        private void RefreshContent()
        {
            if (_achievementManager == null || _contentLabel == null)
            {
                return;
            }

            foreach (var pair in _tabButtons)
            {
                pair.Value.Disabled = pair.Key == _currentCategory;
            }

            var lines = new List<string>
            {
                $"解除数: {_achievementManager.GetUnlockedCount()} / {_achievementManager.Entries.Count}",
                string.Empty
            };

            foreach (var entry in _achievementManager.GetEntries(_currentCategory))
            {
                var stateText = entry.Unlocked ? "解除済み" : $"進捗 {(int)(entry.Progress * 100f)}%";
                lines.Add($"{entry.Title} [{stateText}]");
                lines.Add(entry.Description);
                lines.Add($"報酬: {entry.PassiveDescription}");
                lines.Add(string.Empty);
            }

            _contentLabel.Text = string.Join("\n", lines);
        }

        private void SetVisibleState(bool visible)
        {
            _overlay.Visible = visible;
            _panel.Visible = visible;
        }
    }
}