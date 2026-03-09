using Godot;

namespace CursedBlood.UI
{
    public sealed class VirtualPadSettings
    {
        public float BaseOpacity { get; set; } = 0.28f;

        public float KnobOpacity { get; set; } = 0.52f;

        public float DeadZoneRadius { get; set; } = 24f;

        public float MaxRadius { get; set; } = 72f;

        public float BaseRadius { get; set; } = 84f;

        public float KnobRadius { get; set; } = 34f;

        public static VirtualPadSettings CreateDefault()
        {
            return new VirtualPadSettings();
        }
    }

    public partial class VirtualPad : Control
    {
        private Vector2 _origin;
        private Vector2 _currentPosition;
        private bool _active;

        public VirtualPadSettings Settings { get; private set; } = VirtualPadSettings.CreateDefault();

        public bool IsActive => _active;

        public Vector2 Origin => _origin;

        public Vector2 CurrentPosition => _currentPosition;

        public override void _Ready()
        {
            SetAnchorsPreset(LayoutPreset.FullRect);
            MouseFilter = MouseFilterEnum.Ignore;
            ZIndex = 100;
            Visible = false;
        }

        public void ApplySettings(VirtualPadSettings settings)
        {
            Settings = settings ?? VirtualPadSettings.CreateDefault();
            QueueRedraw();
        }

        public void Begin(Vector2 origin)
        {
            _active = true;
            _origin = origin;
            _currentPosition = origin;
            Visible = true;
            QueueRedraw();
        }

        public void UpdatePointer(Vector2 currentPosition)
        {
            if (!_active)
            {
                return;
            }

            _currentPosition = currentPosition;
            QueueRedraw();
        }

        public void End()
        {
            _active = false;
            _currentPosition = _origin;
            Visible = false;
            QueueRedraw();
        }

        public Vector2 GetDelta()
        {
            return _currentPosition - _origin;
        }

        public Vector2 GetClampedKnobOffset()
        {
            var delta = GetDelta();
            return delta == Vector2.Zero ? Vector2.Zero : delta.LimitLength(Settings.MaxRadius);
        }

        public Vector2I GetSnappedDirection()
        {
            var delta = GetDelta();
            if (delta.Length() < Settings.DeadZoneRadius)
            {
                return Vector2I.Zero;
            }

            var octant = Mathf.PosMod(Mathf.RoundToInt(delta.Angle() / (Mathf.Pi / 4f)), 8);
            return octant switch
            {
                0 => Vector2I.Right,
                1 => new Vector2I(1, 1),
                2 => Vector2I.Down,
                3 => new Vector2I(-1, 1),
                4 => Vector2I.Left,
                5 => new Vector2I(-1, -1),
                6 => Vector2I.Up,
                7 => new Vector2I(1, -1),
                _ => Vector2I.Zero
            };
        }

        public override void _Draw()
        {
            if (!_active)
            {
                return;
            }

            var baseAlpha = Mathf.Clamp(Settings.BaseOpacity, 0f, 1f);
            var knobAlpha = Mathf.Clamp(Settings.KnobOpacity, 0f, 1f);
            var baseColor = new Color(0.94f, 0.97f, 1f, baseAlpha);
            var rimColor = new Color(0.75f, 0.86f, 1f, Mathf.Clamp(baseAlpha + 0.12f, 0f, 1f));
            var knobColor = new Color(0.34f, 0.62f, 0.94f, knobAlpha);
            var knobOutline = new Color(0.95f, 0.98f, 1f, Mathf.Clamp(knobAlpha + 0.18f, 0f, 1f));
            var knobCenter = _origin + GetClampedKnobOffset();

            DrawCircle(_origin, Settings.BaseRadius, baseColor);
            DrawArc(_origin, Settings.BaseRadius, 0f, Mathf.Tau, 48, rimColor, 3f);
            DrawCircle(knobCenter, Settings.KnobRadius, knobColor);
            DrawArc(knobCenter, Settings.KnobRadius, 0f, Mathf.Tau, 32, knobOutline, 3f);
        }
    }
}