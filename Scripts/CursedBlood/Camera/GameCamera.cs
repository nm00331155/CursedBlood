using Godot;

namespace CursedBlood.Camera
{
    public partial class GameCamera : Camera2D
    {
        private float _shakeTimer;

        private float _shakeIntensity;

        public override void _Ready()
        {
            Position = new Vector2(540f, 960f);
            Enabled = true;
            MakeCurrent();
        }

        public override void _Process(double delta)
        {
            if (_shakeTimer <= 0f)
            {
                Offset = Vector2.Zero;
                return;
            }

            _shakeTimer = Mathf.Max(0f, _shakeTimer - (float)delta);
            Offset = new Vector2(
                (float)GD.RandRange(-_shakeIntensity, _shakeIntensity),
                (float)GD.RandRange(-_shakeIntensity, _shakeIntensity));
        }

        public void Shake(float intensity = 5f, float duration = 0.2f)
        {
            _shakeIntensity = intensity;
            _shakeTimer = duration;
        }
    }
}