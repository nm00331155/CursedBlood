using CursedBlood.Core;
using CursedBlood.Player;
using Godot;

namespace CursedBlood.UI
{
    public partial class HUDManager : CanvasLayer
    {
        private const float OxygenBarWidth = 476f;
        private const float HpBarWidth = 280f;

        private PlayerStats _stats;
        private bool _built;
        private ColorRect _topBackground;
        private ColorRect _oxygenBarBackground;
        private ColorRect _oxygenBarFill;
        private ColorRect _hpBarBackground;
        private ColorRect _hpBarFill;
        private ColorRect _sonarBackground;
        private ColorRect _debugBackground;
        private Label _diveLabel;
        private Label _timerLabel;
        private Label _phaseLabel;
        private Label _depthLabel;
        private Label _economyLabel;
        private Label _hpLabel;
        private Label _salvageLabel;
        private Label _sonarLabel;
        private Label _helpLabel;
        private Label _debugLabel;
        private bool _showControlHints;
        private bool _debugEnabled;
        private bool _sonarVisible = true;
        private string _debugText = string.Empty;
        private SonarReading _sonarReading = new(SonarSignalStrength.None, CellType.Empty, Vector2I.Zero, 0);

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

            var oxygenRatio = _stats.OxygenRatio;
            var hpRatio = Mathf.Clamp(_stats.CurrentHp / (float)_stats.MaxHp, 0f, 1f);
            _oxygenBarFill.Size = new Vector2(OxygenBarWidth * oxygenRatio, 30f);
            _oxygenBarFill.Color = oxygenRatio switch
            {
                < 0.18f => new Color(0.96f, 0.28f, 0.22f),
                < 0.45f => new Color(0.98f, 0.69f, 0.18f),
                _ => new Color(0.24f, 0.86f, 0.74f)
            };
            _hpBarFill.Size = new Vector2(HpBarWidth * hpRatio, 20f);
            _hpBarFill.Color = hpRatio switch
            {
                < 0.2f => new Color(0.95f, 0.28f, 0.28f),
                < 0.5f => new Color(0.96f, 0.64f, 0.24f),
                _ => new Color(0.94f, 0.48f, 0.52f)
            };

            _diveLabel.Text = $"潜行 {_stats.Generation:00}";
            _timerLabel.Text = $"酸素 {_stats.RemainingDiveSeconds}s";
            _phaseLabel.Text = $"{_stats.PhaseLabel}  x{_stats.PhaseMultiplier:0.00}";
            _phaseLabel.AddThemeColorOverride("font_color", _stats.Phase switch
            {
                DivePhase.Stable => new Color(0.74f, 0.97f, 0.90f),
                DivePhase.Worn => new Color(1.00f, 0.88f, 0.46f),
                _ => new Color(1.00f, 0.62f, 0.62f)
            });
            _depthLabel.Text = $"{_stats.CurrentDepthMeters}m";
            _economyLabel.Text = $"借金 {_stats.CurrentDebt:N0} / 所持 {_stats.CurrentMoney:N0}";
            _hpLabel.Text = $"HP {_stats.CurrentHp}/{_stats.MaxHp}   掘削 {_stats.BlocksDug}";
            _salvageLabel.Text = $"回収 {_stats.SalvageValue:N0}   鉱石 {_stats.OresCollected}";
            _sonarBackground.Visible = _sonarVisible;
            _sonarLabel.Visible = _sonarVisible;
            _sonarLabel.Text = _sonarVisible ? _sonarReading.GetDisplayText() : string.Empty;
            _sonarLabel.AddThemeColorOverride("font_color", _sonarReading.Strength switch
            {
                SonarSignalStrength.Near => new Color(0.62f, 1.00f, 0.96f),
                SonarSignalStrength.Medium => new Color(0.86f, 0.96f, 1.00f),
                SonarSignalStrength.Far => new Color(0.94f, 0.92f, 0.78f),
                _ => new Color(0.94f, 0.96f, 0.98f)
            });
            _helpLabel.Visible = _showControlHints;
            _debugBackground.Visible = _debugEnabled && !string.IsNullOrWhiteSpace(_debugText);
            _debugLabel.Visible = _debugBackground.Visible;
            _debugLabel.Text = _debugText;
        }

        public void SetSonarReading(SonarReading reading, bool visible)
        {
            _sonarReading = reading;
            _sonarVisible = visible;
        }

        public void SetControlHintsVisible(bool visible)
        {
            _showControlHints = visible;
        }

        public void SetDebugState(bool enabled, string debugText)
        {
            _debugEnabled = enabled;
            _debugText = debugText ?? string.Empty;
        }

        private void BuildUiIfNeeded()
        {
            if (_built)
            {
                return;
            }

            _topBackground = new ColorRect
            {
                Position = new Vector2(12f, 12f),
                Size = new Vector2(1056f, 194f),
                Color = new Color(0.04f, 0.06f, 0.09f, 0.88f)
            };
            AddChild(_topBackground);

            _oxygenBarBackground = new ColorRect
            {
                Position = new Vector2(32f, 80f),
                Size = new Vector2(OxygenBarWidth, 30f),
                Color = new Color(0.17f, 0.23f, 0.29f, 0.9f)
            };
            AddChild(_oxygenBarBackground);

            _oxygenBarFill = new ColorRect
            {
                Position = new Vector2(32f, 80f),
                Size = new Vector2(OxygenBarWidth, 30f),
                Color = new Color(0.24f, 0.86f, 0.74f)
            };
            AddChild(_oxygenBarFill);

            _hpBarBackground = new ColorRect
            {
                Position = new Vector2(748f, 124f),
                Size = new Vector2(HpBarWidth, 20f),
                Color = new Color(0.23f, 0.20f, 0.25f, 0.92f)
            };
            AddChild(_hpBarBackground);

            _hpBarFill = new ColorRect
            {
                Position = new Vector2(748f, 124f),
                Size = new Vector2(HpBarWidth, 20f),
                Color = new Color(0.94f, 0.48f, 0.52f)
            };
            AddChild(_hpBarFill);

            _diveLabel = CreateLabel(new Vector2(32f, 22f), 26, HorizontalAlignment.Left, new Vector2(210f, 34f));
            AddChild(_diveLabel);

            _timerLabel = CreateLabel(new Vector2(32f, 120f), 28, HorizontalAlignment.Left, new Vector2(220f, 40f));
            AddChild(_timerLabel);

            _phaseLabel = CreateLabel(new Vector2(740f, 22f), 28, HorizontalAlignment.Right, new Vector2(288f, 38f));
            AddChild(_phaseLabel);

            _depthLabel = CreateLabel(new Vector2(406f, 20f), 56, HorizontalAlignment.Center, new Vector2(268f, 58f));
            AddChild(_depthLabel);

            _economyLabel = CreateLabel(new Vector2(520f, 78f), 28, HorizontalAlignment.Right, new Vector2(508f, 36f));
            AddChild(_economyLabel);

            _hpLabel = CreateLabel(new Vector2(520f, 144f), 28, HorizontalAlignment.Right, new Vector2(508f, 34f));
            AddChild(_hpLabel);

            _salvageLabel = CreateLabel(new Vector2(32f, 156f), 24, HorizontalAlignment.Left, new Vector2(420f, 30f));
            AddChild(_salvageLabel);

            _helpLabel = CreateLabel(new Vector2(148f, 214f), 20, HorizontalAlignment.Center, new Vector2(784f, 28f));
            _helpLabel.Text = "F3 Debug  F4 Preview  F5 Hint  F6 Sonar  F7 Zoom";
            _helpLabel.Visible = false;
            AddChild(_helpLabel);

            _sonarBackground = new ColorRect
            {
                Position = new Vector2(140f, 1798f),
                Size = new Vector2(800f, 74f),
                Color = new Color(0.05f, 0.08f, 0.11f, 0.78f)
            };
            AddChild(_sonarBackground);

            _sonarLabel = CreateLabel(new Vector2(164f, 1810f), 26, HorizontalAlignment.Center, new Vector2(752f, 40f));
            AddChild(_sonarLabel);

            _debugBackground = new ColorRect
            {
                Position = new Vector2(56f, 248f),
                Size = new Vector2(968f, 126f),
                Color = new Color(0.03f, 0.05f, 0.07f, 0.82f),
                Visible = false
            };
            AddChild(_debugBackground);

            _debugLabel = CreateLabel(new Vector2(76f, 264f), 22, HorizontalAlignment.Left, new Vector2(928f, 96f));
            _debugLabel.Visible = false;
            _debugLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            AddChild(_debugLabel);

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