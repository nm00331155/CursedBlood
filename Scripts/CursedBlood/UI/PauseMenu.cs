using System;
using Godot;

namespace CursedBlood.UI
{
    public partial class PauseMenu : CanvasLayer
    {
        private bool _uiBuilt;
        private ColorRect _overlay;
        private Panel _panel;

        public bool IsOpen => _panel?.Visible == true;

        public event Action ResumeRequested;

        public event Action SettingsRequested;

        public event Action TitleRequested;

        public void Toggle()
        {
            BuildUiIfNeeded();
            SetVisibleState(!IsOpen);
        }

        public void HideMenu()
        {
            SetVisibleState(false);
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
                Color = new Color(0f, 0f, 0f, 0.5f)
            };
            AddChild(_overlay);

            _panel = new Panel
            {
                Position = new Vector2(280f, 420f),
                Size = new Vector2(520f, 460f)
            };
            AddChild(_panel);

            AddButton("再開", 80f, () => ResumeRequested?.Invoke());
            AddButton("設定", 160f, () => SettingsRequested?.Invoke());
            AddButton("タイトルへ", 240f, () => TitleRequested?.Invoke());

            _uiBuilt = true;
            SetVisibleState(false);
        }

        private void AddButton(string text, float y, Action action)
        {
            var button = new Button
            {
                Position = new Vector2(100f, y),
                Size = new Vector2(320f, 54f),
                Text = text
            };
            button.Pressed += () =>
            {
                action();
                SetVisibleState(false);
            };
            _panel.AddChild(button);
        }

        private void SetVisibleState(bool visible)
        {
            if (!_uiBuilt)
            {
                return;
            }

            _overlay.Visible = visible;
            _panel.Visible = visible;
        }
    }
}