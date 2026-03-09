using System.Collections.Generic;
using Godot;

namespace CursedBlood.Achievement
{
    public partial class AchievementPopup : CanvasLayer
    {
        private readonly Queue<AchievementEntry> _pendingEntries = new();
        private Panel _panel;
        private Label _contentLabel;
        private Tween _activeTween;
        private bool _uiBuilt;
        private bool _isShowing;

        public override void _UnhandledInput(InputEvent @event)
        {
            if (!_isShowing)
            {
                return;
            }

            if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed)
            {
                DismissCurrent();
                GetViewport().SetInputAsHandled();
                return;
            }

            if (@event is InputEventScreenTouch screenTouch && screenTouch.Pressed)
            {
                DismissCurrent();
                GetViewport().SetInputAsHandled();
                return;
            }

            if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
            {
                DismissCurrent();
                GetViewport().SetInputAsHandled();
            }
        }

        public void QueueUnlock(AchievementEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            BuildUiIfNeeded();
            _pendingEntries.Enqueue(entry);
            if (!_isShowing)
            {
                ShowNext();
            }
        }

        private void BuildUiIfNeeded()
        {
            if (_uiBuilt)
            {
                return;
            }

            Layer = 70;
            _panel = new Panel
            {
                Position = new Vector2(140f, -120f),
                Size = new Vector2(800f, 100f),
                Visible = false
            };
            AddChild(_panel);

            _contentLabel = new Label
            {
                Position = new Vector2(24f, 18f),
                Size = new Vector2(752f, 64f),
                AutowrapMode = TextServer.AutowrapMode.WordSmart
            };
            _contentLabel.AddThemeFontSizeOverride("font_size", 24);
            _panel.AddChild(_contentLabel);

            _uiBuilt = true;
        }

        private void ShowNext()
        {
            if (_pendingEntries.Count == 0)
            {
                _isShowing = false;
                return;
            }

            var entry = _pendingEntries.Dequeue();
            _isShowing = true;
            _panel.Visible = true;
            _panel.Position = new Vector2(140f, -120f);
            _contentLabel.Text = $"実績解除! {entry.Title}\n{entry.PassiveDescription}";

            _activeTween?.Kill();
            _activeTween = CreateTween();
            _activeTween.TweenProperty(_panel, "position:y", 30f, 0.25f).From(-120f);
            _activeTween.TweenInterval(3.0f);
            _activeTween.TweenProperty(_panel, "position:y", -120f, 0.25f);
            _activeTween.TweenCallback(Callable.From(OnPopupFinished));
        }

        private void DismissCurrent()
        {
            if (!_isShowing)
            {
                return;
            }

            _activeTween?.Kill();
            OnPopupFinished();
        }

        private void OnPopupFinished()
        {
            _panel.Visible = false;
            _isShowing = false;
            ShowNext();
        }
    }
}