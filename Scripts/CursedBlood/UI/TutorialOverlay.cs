using CursedBlood.Core;
using Godot;

namespace CursedBlood.UI
{
    public sealed class TutorialState
    {
        public bool HasSeenTutorial { get; set; }
    }

    public partial class TutorialOverlay : CanvasLayer
    {
        private const string SavePath = "user://settings/tutorial_state.json";

        private bool _uiBuilt;
        private ColorRect _overlay;
        private Panel _panel;

        public bool ShouldShow()
        {
            return !JsonStorage.Load(SavePath, () => new TutorialState()).HasSeenTutorial;
        }

        public void ShowIfNeeded()
        {
            if (!ShouldShow())
            {
                return;
            }

            BuildUiIfNeeded();
            _overlay.Visible = true;
            _panel.Visible = true;
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
                Color = new Color(0f, 0f, 0f, 0.6f),
                Visible = false
            };
            AddChild(_overlay);

            _panel = new Panel
            {
                Position = new Vector2(120f, 320f),
                Size = new Vector2(840f, 760f),
                Visible = false
            };
            AddChild(_panel);

            var label = new Label
            {
                Position = new Vector2(40f, 40f),
                Size = new Vector2(760f, 560f),
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                Text = "矢印キーまたはスワイプで移動\nSpace長押しでガード\nEnterまたはダブルタップでスキル\n\n敵を倒して装備を集め、世代を重ねて深く潜ってください。"
            };
            label.AddThemeFontSizeOverride("font_size", 28);
            _panel.AddChild(label);

            var button = new Button
            {
                Position = new Vector2(260f, 640f),
                Size = new Vector2(320f, 56f),
                Text = "始める"
            };
            button.Pressed += Dismiss;
            _panel.AddChild(button);

            _uiBuilt = true;
        }

        private void Dismiss()
        {
            JsonStorage.Save(SavePath, new TutorialState { HasSeenTutorial = true });
            _overlay.Visible = false;
            _panel.Visible = false;
        }
    }
}