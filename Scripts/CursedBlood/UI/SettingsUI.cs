using CursedBlood.Audio;
using Godot;

namespace CursedBlood.UI
{
    public partial class SettingsUI : CanvasLayer
    {
        private bool _uiBuilt;
        private ColorRect _overlay;
        private Panel _panel;
        private Label _contentLabel;
        private AudioManager _audioManager;

        public bool IsOpen => _panel?.Visible == true;

        public void Initialize(AudioManager audioManager)
        {
            _audioManager = audioManager;
            BuildUiIfNeeded();
            Refresh();
            SetVisibleState(false);
        }

        public void Toggle()
        {
            BuildUiIfNeeded();
            Refresh();
            SetVisibleState(!_panel.Visible);
        }

        public void HidePanel()
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
                Color = new Color(0f, 0f, 0f, 0.45f)
            };
            AddChild(_overlay);

            _panel = new Panel
            {
                Position = new Vector2(220f, 460f),
                Size = new Vector2(640f, 520f)
            };
            AddChild(_panel);

            var title = new Label
            {
                Position = new Vector2(30f, 20f),
                Size = new Vector2(320f, 36f),
                Text = "設定"
            };
            title.AddThemeFontSizeOverride("font_size", 28);
            _panel.AddChild(title);

            _contentLabel = new Label
            {
                Position = new Vector2(30f, 76f),
                Size = new Vector2(580f, 140f)
            };
            _contentLabel.AddThemeFontSizeOverride("font_size", 24);
            _panel.AddChild(_contentLabel);

            AddButton("BGM +", 240f, () => { _audioManager.AdjustBgm(0.1f); Refresh(); });
            AddButton("BGM -", 300f, () => { _audioManager.AdjustBgm(-0.1f); Refresh(); });
            AddButton("SE +", 360f, () => { _audioManager.AdjustSe(0.1f); Refresh(); });
            AddButton("SE -", 420f, () => { _audioManager.AdjustSe(-0.1f); Refresh(); });
            AddButton("振動切替", 480f, () => { _audioManager.ToggleVibration(); Refresh(); });

            var close = new Button
            {
                Position = new Vector2(460f, 20f),
                Size = new Vector2(120f, 40f),
                Text = "閉じる"
            };
            close.Pressed += HidePanel;
            _panel.AddChild(close);

            _uiBuilt = true;
        }

        private void Refresh()
        {
            if (_audioManager == null || _contentLabel == null)
            {
                return;
            }

            _contentLabel.Text =
                $"BGM: {_audioManager.Settings.BgmVolume:P0}\n" +
                $"SE: {_audioManager.Settings.SeVolume:P0}\n" +
                $"振動: {(_audioManager.Settings.VibrationEnabled ? "ON" : "OFF")}";
        }

        private void AddButton(string text, float y, System.Action action)
        {
            var button = new Button
            {
                Position = new Vector2(180f, y),
                Size = new Vector2(280f, 44f),
                Text = text
            };
            button.Pressed += () => action();
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