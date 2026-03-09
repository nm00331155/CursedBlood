using System;
using CursedBlood.Core;
using CursedBlood.Player;
using Godot;

namespace CursedBlood.UI
{
    public partial class HUDManager : CanvasLayer
    {
        private PlayerStats _stats;
        private ThemeSettings _themeSettings;
        private GameTheme _theme = ThemeSettings.CreateDefault().BuildTheme();
        private bool _uiBuilt;
        private Func<long> _debtGetter;
        private Func<int> _curseGetter;
        private Func<int> _achievementCountGetter;
        private float _notificationTimer;
        private string _notificationText = string.Empty;
        private ColorRect _topBackground;
        private ColorRect _bottomBackground;
        private ColorRect _lifespanBarBackground;
        private ColorRect _lifespanBarFill;
        private ColorRect _themePanel;
        private Label _nameLabel;
        private Label _ageLabel;
        private Label _phaseLabel;
        private Label _depthLabel;
        private Label _scoreLabel;
        private Label _goldLabel;
        private Label _comboLabel;
        private Label _debtLabel;
        private Label _hpLabel;
        private Label _skillLabel;
        private Label _curseLabel;
        private Label _notificationLabel;
        private Label _themeTitleLabel;
        private Label _themeStatusLabel;
        private Button _modeButton;
        private Button _backgroundButton;
        private Button _textButton;
        private Button _accentButton;
        private Button _equipmentButton;
        private Button _familyTreeButton;
        private Button _achievementButton;
        private Button _rankingButton;
        private Button _curseButton;
        private Button _pauseButton;

        public event Action<ThemeSettings> ThemeChanged;

        public event Action EquipmentRequested;

        public event Action FamilyTreeRequested;

        public event Action AchievementsRequested;

        public event Action RankingRequested;

        public event Action CurseRequested;

        public event Action PauseRequested;

        public void Initialize(PlayerStats stats, ThemeSettings themeSettings, Func<long> debtGetter = null, Func<int> curseGetter = null, Func<int> achievementCountGetter = null)
        {
            _stats = stats;
            _themeSettings = themeSettings;
            _debtGetter = debtGetter;
            _curseGetter = curseGetter;
            _achievementCountGetter = achievementCountGetter;
            BuildUiIfNeeded();
            ApplyTheme(_themeSettings.BuildTheme());
            UpdateThemeStatusText();
        }

        public void UpdateSources(Func<long> debtGetter, Func<int> curseGetter, Func<int> achievementCountGetter)
        {
            _debtGetter = debtGetter;
            _curseGetter = curseGetter;
            _achievementCountGetter = achievementCountGetter;
        }

        public void ShowNotification(string text)
        {
            _notificationText = text;
            _notificationTimer = 2f;
        }

        public void ApplyTheme(GameTheme theme)
        {
            _theme = theme;

            if (!_uiBuilt)
            {
                return;
            }

            _topBackground.Color = new Color(theme.PanelColor.R, theme.PanelColor.G, theme.PanelColor.B, 0.92f);
            _bottomBackground.Color = new Color(theme.PanelColor.R, theme.PanelColor.G, theme.PanelColor.B, 0.92f);
            _lifespanBarBackground.Color = theme.BackgroundColor.Lerp(theme.TextColor, 0.18f);
            _themePanel.Color = new Color(theme.PanelColor.R, theme.PanelColor.G, theme.PanelColor.B, 0.98f);

            ApplyLabelTheme(_nameLabel);
            ApplyLabelTheme(_ageLabel);
            ApplyLabelTheme(_phaseLabel);
            ApplyLabelTheme(_depthLabel);
            ApplyLabelTheme(_scoreLabel);
            ApplyLabelTheme(_goldLabel);
            ApplyLabelTheme(_comboLabel);
            ApplyLabelTheme(_debtLabel);
            ApplyLabelTheme(_hpLabel);
            ApplyLabelTheme(_skillLabel);
            ApplyLabelTheme(_curseLabel);
            ApplyLabelTheme(_notificationLabel);
            ApplyLabelTheme(_themeTitleLabel);
            ApplyLabelTheme(_themeStatusLabel);

            ApplyButtonTheme(_modeButton);
            ApplyButtonTheme(_backgroundButton);
            ApplyButtonTheme(_textButton);
            ApplyButtonTheme(_accentButton);
            ApplyButtonTheme(_equipmentButton);
            ApplyButtonTheme(_familyTreeButton);
            ApplyButtonTheme(_achievementButton);
            ApplyButtonTheme(_rankingButton);
            ApplyButtonTheme(_curseButton);
            ApplyButtonTheme(_pauseButton);
        }

        public override void _Process(double delta)
        {
            if (_stats == null || !_uiBuilt)
            {
                return;
            }

            var lifeRatio = Mathf.Clamp(1f - _stats.CurrentAge / _stats.MaxLifespan, 0f, 1f);
            _lifespanBarFill.Size = new Vector2(400f * lifeRatio, 40f);

            if (lifeRatio < 0.25f)
            {
                var pulse = (Mathf.Sin((float)Time.GetTicksMsec() / 200f) + 1f) / 2f;
                _lifespanBarFill.Color = _theme.WarningColor.Lerp(_theme.TextColor, pulse * 0.15f);
            }
            else if (lifeRatio < 0.5f)
            {
                _lifespanBarFill.Color = _theme.WarningColor.Lerp(_theme.AccentColor, 0.35f);
            }
            else
            {
                _lifespanBarFill.Color = _theme.AccentColor.Lerp(_theme.PlayerYouthColor, 0.45f);
            }

            _nameLabel.Text = $"第{_stats.Generation}世代 {_stats.CharacterName}";
            _ageLabel.Text = $"{(int)_stats.HumanAge}歳 / 35歳";
            _phaseLabel.Text = _stats.Phase switch
            {
                LifePhase.Youth => "【少年期】 掘削の感覚を掴み始める",
                LifePhase.Prime => "【青年期】 全盛期の速度で掘り進む",
                LifePhase.Twilight => "【晩年期】 呪いが血脈を蝕む",
                _ => string.Empty
            };
            _depthLabel.Text = $"深度 {_stats.CurrentDepth}";
            _scoreLabel.Text = $"Score {_stats.CalculateScore():N0}";
            _goldLabel.Text = $"Gold {_stats.Gold:N0}G";
            _comboLabel.Text = _stats.CurrentCombo > 0 ? $"{_stats.CurrentCombo} Combo!" : string.Empty;
            _debtLabel.Text = $"借金 {_debtGetter?.Invoke() ?? 0:N0}G";
            _hpLabel.Text = $"HP: {_stats.CurrentHp} / {_stats.MaxHp}";
            _skillLabel.Text = $"Skill {_stats.SkillGauge:0}%  |  実績 {_achievementCountGetter?.Invoke() ?? 0}";
            _curseLabel.Text = $"呪い研究 {_curseGetter?.Invoke() ?? 0}";

            if (_notificationTimer > 0f)
            {
                _notificationTimer = Mathf.Max(0f, _notificationTimer - (float)delta);
                _notificationLabel.Text = _notificationText;
                _notificationLabel.Modulate = new Color(1f, 1f, 1f, Mathf.Clamp(_notificationTimer / 2f, 0.2f, 1f));
            }
            else
            {
                _notificationLabel.Text = string.Empty;
            }
        }

        private void BuildUiIfNeeded()
        {
            if (_uiBuilt)
            {
                return;
            }

            _topBackground = new ColorRect
            {
                Position = Vector2.Zero,
                Size = new Vector2(1080f, 200f)
            };
            AddChild(_topBackground);

            _bottomBackground = new ColorRect
            {
                Position = new Vector2(0f, 1600f),
                Size = new Vector2(1080f, 320f)
            };
            AddChild(_bottomBackground);

            _lifespanBarBackground = new ColorRect
            {
                Position = new Vector2(30f, 58f),
                Size = new Vector2(400f, 40f)
            };
            AddChild(_lifespanBarBackground);

            _lifespanBarFill = new ColorRect
            {
                Position = new Vector2(30f, 58f),
                Size = new Vector2(400f, 40f)
            };
            AddChild(_lifespanBarFill);

            _nameLabel = CreateLabel(new Vector2(30f, 16f), 28);
            _nameLabel.Size = new Vector2(500f, 34f);
            AddChild(_nameLabel);

            _ageLabel = CreateLabel(new Vector2(30f, 104f), 26);
            AddChild(_ageLabel);

            _phaseLabel = CreateLabel(new Vector2(30f, 136f), 22);
            _phaseLabel.Size = new Vector2(560f, 28f);
            AddChild(_phaseLabel);

            _depthLabel = CreateLabel(new Vector2(340f, 20f), 36, HorizontalAlignment.Center);
            _depthLabel.Size = new Vector2(380f, 52f);
            AddChild(_depthLabel);

            _scoreLabel = CreateLabel(new Vector2(640f, 20f), 26, HorizontalAlignment.Right);
            _scoreLabel.Size = new Vector2(390f, 34f);
            AddChild(_scoreLabel);

            _goldLabel = CreateLabel(new Vector2(640f, 56f), 24, HorizontalAlignment.Right);
            _goldLabel.Size = new Vector2(390f, 30f);
            AddChild(_goldLabel);

            _comboLabel = CreateLabel(new Vector2(640f, 88f), 24, HorizontalAlignment.Right);
            _comboLabel.Size = new Vector2(390f, 30f);
            AddChild(_comboLabel);

            _debtLabel = CreateLabel(new Vector2(30f, 168f), 22);
            AddChild(_debtLabel);

            _themePanel = new ColorRect
            {
                Position = new Vector2(700f, 128f),
                Size = new Vector2(350f, 60f)
            };
            AddChild(_themePanel);

            _themeTitleLabel = CreateLabel(new Vector2(10f, 6f), 14);
            _themeTitleLabel.Text = "Color Settings";
            _themePanel.AddChild(_themeTitleLabel);

            _modeButton = CreateThemeButton("Mode", new Vector2(10f, 26f), new Vector2(72f, 28f));
            _modeButton.Pressed += OnModePressed;
            _themePanel.AddChild(_modeButton);

            _backgroundButton = CreateThemeButton("BG", new Vector2(90f, 26f), new Vector2(56f, 28f));
            _backgroundButton.Pressed += OnBackgroundPressed;
            _themePanel.AddChild(_backgroundButton);

            _textButton = CreateThemeButton("Text", new Vector2(152f, 26f), new Vector2(56f, 28f));
            _textButton.Pressed += OnTextPressed;
            _themePanel.AddChild(_textButton);

            _accentButton = CreateThemeButton("Accent", new Vector2(214f, 26f), new Vector2(64f, 28f));
            _accentButton.Pressed += OnAccentPressed;
            _themePanel.AddChild(_accentButton);

            _themeStatusLabel = CreateLabel(new Vector2(284f, 28f), 12);
            _themeStatusLabel.Size = new Vector2(62f, 24f);
            _themePanel.AddChild(_themeStatusLabel);

            _hpLabel = CreateLabel(new Vector2(30f, 1640f), 28);
            AddChild(_hpLabel);

            _skillLabel = CreateLabel(new Vector2(30f, 1680f), 24);
            _skillLabel.Size = new Vector2(600f, 30f);
            AddChild(_skillLabel);

            _curseLabel = CreateLabel(new Vector2(30f, 1712f), 22);
            _curseLabel.Size = new Vector2(600f, 28f);
            AddChild(_curseLabel);

            _notificationLabel = CreateLabel(new Vector2(120f, 1760f), 24, HorizontalAlignment.Center);
            _notificationLabel.Size = new Vector2(840f, 36f);
            AddChild(_notificationLabel);

            _equipmentButton = CreateThemeButton("装備", new Vector2(700f, 1640f), new Vector2(80f, 44f));
            _equipmentButton.Pressed += () => EquipmentRequested?.Invoke();
            AddChild(_equipmentButton);

            _familyTreeButton = CreateThemeButton("家系", new Vector2(790f, 1640f), new Vector2(80f, 44f));
            _familyTreeButton.Pressed += () => FamilyTreeRequested?.Invoke();
            AddChild(_familyTreeButton);

            _achievementButton = CreateThemeButton("実績", new Vector2(880f, 1640f), new Vector2(80f, 44f));
            _achievementButton.Pressed += () => AchievementsRequested?.Invoke();
            AddChild(_achievementButton);

            _rankingButton = CreateThemeButton("順位", new Vector2(970f, 1640f), new Vector2(80f, 44f));
            _rankingButton.Pressed += () => RankingRequested?.Invoke();
            AddChild(_rankingButton);

            _curseButton = CreateThemeButton("呪研", new Vector2(700f, 1692f), new Vector2(80f, 44f));
            _curseButton.Pressed += () => CurseRequested?.Invoke();
            AddChild(_curseButton);

            _pauseButton = CreateThemeButton("Pause", new Vector2(790f, 1692f), new Vector2(170f, 44f));
            _pauseButton.Pressed += () => PauseRequested?.Invoke();
            AddChild(_pauseButton);

            _uiBuilt = true;
        }

        private void ApplyLabelTheme(Label label)
        {
            if (label != null)
            {
                label.AddThemeColorOverride("font_color", _theme.TextColor);
            }
        }

        private void ApplyButtonTheme(Button button)
        {
            if (button == null)
            {
                return;
            }

            var normalStyle = new StyleBoxFlat
            {
                BgColor = _theme.PanelColor.Lerp(_theme.AccentColor, 0.16f),
                BorderColor = _theme.BorderColor,
                BorderWidthBottom = 2,
                BorderWidthLeft = 2,
                BorderWidthRight = 2,
                BorderWidthTop = 2,
                CornerRadiusBottomLeft = 8,
                CornerRadiusBottomRight = 8,
                CornerRadiusTopLeft = 8,
                CornerRadiusTopRight = 8
            };

            var hoverStyle = new StyleBoxFlat
            {
                BgColor = _theme.PanelColor.Lerp(_theme.AccentColor, 0.28f),
                BorderColor = _theme.TextColor,
                BorderWidthBottom = 2,
                BorderWidthLeft = 2,
                BorderWidthRight = 2,
                BorderWidthTop = 2,
                CornerRadiusBottomLeft = 8,
                CornerRadiusBottomRight = 8,
                CornerRadiusTopLeft = 8,
                CornerRadiusTopRight = 8
            };

            button.AddThemeStyleboxOverride("normal", normalStyle);
            button.AddThemeStyleboxOverride("hover", hoverStyle);
            button.AddThemeStyleboxOverride("pressed", hoverStyle);
            button.AddThemeStyleboxOverride("focus", hoverStyle);
            button.AddThemeColorOverride("font_color", _theme.TextColor);
            button.AddThemeColorOverride("font_hover_color", _theme.TextColor);
            button.AddThemeColorOverride("font_pressed_color", _theme.TextColor);
        }

        private Label CreateLabel(Vector2 position, int fontSize, HorizontalAlignment horizontalAlignment = HorizontalAlignment.Left)
        {
            var label = new Label
            {
                Position = position,
                HorizontalAlignment = horizontalAlignment
            };
            label.AddThemeFontSizeOverride("font_size", fontSize);
            return label;
        }

        private Button CreateThemeButton(string text, Vector2 position, Vector2 size)
        {
            var button = new Button
            {
                Text = text,
                Position = position,
                Size = size,
                FocusMode = Control.FocusModeEnum.All
            };
            button.AddThemeFontSizeOverride("font_size", 16);
            return button;
        }

        private void UpdateThemeStatusText()
        {
            if (_themeSettings == null || !_uiBuilt)
            {
                return;
            }

            _themeStatusLabel.Text = _themeSettings.Mode == ThemeMode.Dark ? "Dark" : "Light";
        }

        private void NotifyThemeChanged()
        {
            if (_themeSettings == null)
            {
                return;
            }

            ApplyTheme(_themeSettings.BuildTheme());
            UpdateThemeStatusText();
            ThemeChanged?.Invoke(_themeSettings);
        }

        private void OnModePressed()
        {
            _themeSettings.ToggleMode();
            NotifyThemeChanged();
        }

        private void OnBackgroundPressed()
        {
            _themeSettings.CycleBackground();
            NotifyThemeChanged();
        }

        private void OnTextPressed()
        {
            _themeSettings.CycleText();
            NotifyThemeChanged();
        }

        private void OnAccentPressed()
        {
            _themeSettings.CycleAccent();
            NotifyThemeChanged();
        }
    }
}