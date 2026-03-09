using Godot;

namespace CursedBlood.Effects
{
    public partial class ScreenEffects : CanvasLayer
    {
        private ColorRect _flashOverlay;
        private float _flashTimer;
        private float _flashDuration = 0.15f;
        private Color _flashColor = Colors.White;
        private float _curseOverlayStrength;

        public override void _Ready()
        {
            _flashOverlay = new ColorRect
            {
                Position = Vector2.Zero,
                Size = new Vector2(1080f, 1920f),
                Color = new Color(0f, 0f, 0f, 0f)
            };
            AddChild(_flashOverlay);
        }

        public override void _Process(double delta)
        {
            if (_flashTimer > 0f)
            {
                _flashTimer = Mathf.Max(0f, _flashTimer - (float)delta);
                var alpha = _flashTimer / Mathf.Max(0.01f, _flashDuration);
                _flashOverlay.Color = new Color(_flashColor.R, _flashColor.G, _flashColor.B, alpha * 0.5f + _curseOverlayStrength);
            }
            else
            {
                _flashOverlay.Color = new Color(0f, 0f, 0f, _curseOverlayStrength);
            }
        }

        public void Flash(Color color, float duration = 0.15f)
        {
            _flashColor = color;
            _flashDuration = duration;
            _flashTimer = duration;
        }

        public void TriggerDeathFlash()
        {
            Flash(new Color(0.9f, 0.1f, 0.1f), 0.4f);
        }

        public void SetCurseOverlay(float strength)
        {
            _curseOverlayStrength = Mathf.Clamp(strength, 0f, 0.35f);
        }
    }
}