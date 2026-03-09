using CursedBlood.Player;
using Godot;

namespace CursedBlood.UI
{
    public partial class HUDManager : CanvasLayer
    {
        private PlayerStats _stats;
        private bool _built;
        private ColorRect _topBackground;
        private ColorRect _bottomBackground;
        private ColorRect _lifeBarBackground;
        private ColorRect _lifeBarFill;
        private Label _generationLabel;
        private Label _ageLabel;
        private Label _phaseLabel;
        private Label _depthLabel;
        private Label _scoreLabel;
        private Label _hpLabel;
        private Label _hintLabel;

        public override void _Ready()
        {
            SetProcess(true);
        }

        public void Initialize(PlayerStats stats)
        {
            _stats = stats;
            BuildUiIfNeeded();
        }

        public override void _Process(double delta)
        {
            if (_stats == null || !_built)
            {
                return;
            }

            var lifeRatio = Mathf.Clamp(1f - (_stats.CurrentAge / _stats.MaxLifespan), 0f, 1f);
            _lifeBarFill.Size = new Vector2(420f * lifeRatio, 28f);
            _lifeBarFill.Color = lifeRatio switch
            {
                < 0.25f => new Color(0.95f, 0.30f, 0.22f),
                < 0.5f => new Color(0.95f, 0.67f, 0.18f),
                _ => new Color(0.28f, 0.82f, 0.52f)
            };

            _generationLabel.Text = $"第{_stats.Generation}世代";
            _ageLabel.Text = $"{Mathf.RoundToInt(_stats.HumanAge)}歳/35歳";
            _phaseLabel.Text = _stats.Phase switch
            {
                LifePhase.Youth => "少年期 3x3 / 速度 x0.6",
                LifePhase.Prime => "青年期 5x5 / 速度 x1.0",
                LifePhase.Twilight => "晩年期 5x5 / 速度 x0.7",
                _ => string.Empty
            };
            _depthLabel.Text = $"深度 {_stats.GridPosition.Y}m";
            _scoreLabel.Text = $"Score {_stats.CalculateScore():N0}";
            _hpLabel.Text = $"HP {_stats.CurrentHp}/{_stats.MaxHp}   掘削 {_stats.BlocksDug}";
        }

        private void BuildUiIfNeeded()
        {
            if (_built)
            {
                return;
            }

            _topBackground = new ColorRect
            {
                Position = Vector2.Zero,
                Size = new Vector2(1080f, 200f),
                Color = new Color(0.05f, 0.07f, 0.10f, 0.92f)
            };
            AddChild(_topBackground);

            _bottomBackground = new ColorRect
            {
                Position = new Vector2(0f, 1600f),
                Size = new Vector2(1080f, 320f),
                Color = new Color(0.05f, 0.07f, 0.10f, 0.92f)
            };
            AddChild(_bottomBackground);

            _lifeBarBackground = new ColorRect
            {
                Position = new Vector2(32f, 78f),
                Size = new Vector2(420f, 28f),
                Color = new Color(0.19f, 0.23f, 0.28f)
            };
            AddChild(_lifeBarBackground);

            _lifeBarFill = new ColorRect
            {
                Position = new Vector2(32f, 78f),
                Size = new Vector2(420f, 28f),
                Color = new Color(0.28f, 0.82f, 0.52f)
            };
            AddChild(_lifeBarFill);

            _generationLabel = CreateLabel(new Vector2(32f, 22f), 30, HorizontalAlignment.Left, new Vector2(220f, 36f));
            AddChild(_generationLabel);

            _ageLabel = CreateLabel(new Vector2(32f, 112f), 26, HorizontalAlignment.Left, new Vector2(240f, 32f));
            AddChild(_ageLabel);

            _phaseLabel = CreateLabel(new Vector2(32f, 146f), 24, HorizontalAlignment.Left, new Vector2(420f, 32f));
            AddChild(_phaseLabel);

            _depthLabel = CreateLabel(new Vector2(360f, 26f), 38, HorizontalAlignment.Center, new Vector2(360f, 44f));
            AddChild(_depthLabel);

            _scoreLabel = CreateLabel(new Vector2(720f, 28f), 28, HorizontalAlignment.Right, new Vector2(328f, 36f));
            AddChild(_scoreLabel);

            _hpLabel = CreateLabel(new Vector2(32f, 1640f), 32, HorizontalAlignment.Left, new Vector2(460f, 42f));
            AddChild(_hpLabel);

            _hintLabel = CreateLabel(new Vector2(32f, 1694f), 22, HorizontalAlignment.Left, new Vector2(1000f, 28f));
            _hintLabel.Text = "矢印キー同時押しで斜め / スワイプで8方向 / Space長押しでGuard";
            AddChild(_hintLabel);

            _built = true;
        }

        private static Label CreateLabel(Vector2 position, int fontSize, HorizontalAlignment alignment, Vector2 size)
        {
            var label = new Label
            {
                Position = position,
                Size = size,
                HorizontalAlignment = alignment,
                VerticalAlignment = VerticalAlignment.Center
            };
            label.AddThemeFontSizeOverride("font_size", fontSize);
            label.AddThemeColorOverride("font_color", new Color(0.95f, 0.96f, 0.98f));
            return label;
        }
    }
}