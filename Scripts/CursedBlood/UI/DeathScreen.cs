using System;
using System.Collections.Generic;
using System.Linq;
using CursedBlood.Core;
using CursedBlood.Debt;
using CursedBlood.Equipment;
using CursedBlood.Player;
using Godot;

namespace CursedBlood.UI
{
    public partial class DeathScreen : CanvasLayer
    {
        private bool _uiBuilt;
        private bool _isVisible;
        private GameTheme _theme = ThemeSettings.CreateDefault().BuildTheme();
        private ColorRect _overlay;
        private Panel _panel;
        private Label _titleLabel;
        private Label _causeLabel;
        private Label _statsLabel;
        private Label _heirloomLabel;
        private Button _continueButton;
        private DebtUI _debtUi;
        private readonly List<Button> _heirloomButtons = new();
        private DebtPaymentKind _selectedDebtKind = DebtPaymentKind.None;
        private EquipmentData _selectedHeirloom;
        private IReadOnlyList<DebtPaymentOption> _currentDebtOptions = Array.Empty<DebtPaymentOption>();

        public bool IsVisibleScreen => _isVisible;

        public event Action<DebtPaymentKind, EquipmentData> ContinueRequested;

        public void Initialize()
        {
            BuildUiIfNeeded();
            SetScreenVisible(false);
        }

        public void ApplyTheme(GameTheme theme)
        {
            _theme = theme;
            if (!_uiBuilt)
            {
                return;
            }

            _overlay.Color = theme.OverlayColor;
            var panelStyle = new StyleBoxFlat
            {
                BgColor = new Color(theme.PanelColor.R, theme.PanelColor.G, theme.PanelColor.B, 0.98f),
                BorderColor = theme.BorderColor,
                BorderWidthBottom = 3,
                BorderWidthLeft = 3,
                BorderWidthRight = 3,
                BorderWidthTop = 3,
                CornerRadiusBottomLeft = 20,
                CornerRadiusBottomRight = 20,
                CornerRadiusTopLeft = 20,
                CornerRadiusTopRight = 20
            };
            _panel.AddThemeStyleboxOverride("panel", panelStyle);

            ApplyLabelTheme(_titleLabel);
            ApplyLabelTheme(_causeLabel);
            ApplyLabelTheme(_statsLabel);
            ApplyLabelTheme(_heirloomLabel);
            _continueButton.AddThemeColorOverride("font_color", theme.TextColor);
        }

        public void Show(PlayerStats stats, string deathCause, IReadOnlyList<DebtPaymentOption> debtOptions)
        {
            BuildUiIfNeeded();
            SetScreenVisible(true);

            _currentDebtOptions = debtOptions ?? Array.Empty<DebtPaymentOption>();
            _selectedDebtKind = DebtPaymentKind.None;
            _selectedHeirloom = null;

            _causeLabel.Text = deathCause;
            _statsLabel.Text =
                $"世代: 第{stats.Generation}世代\n" +
                $"享年: {(int)stats.HumanAge}歳\n" +
                $"最大深度: {stats.MaxDepth}\n" +
                $"スコア: {stats.CalculateScore():N0}\n" +
                $"撃破数: {stats.EnemiesKilled}\n" +
                $"最大コンボ: {stats.MaxCombo}\n" +
                $"所持ゴールド: {stats.Gold:N0}G";

            _debtUi.SetOptions(_currentDebtOptions);
            BuildHeirloomButtons(stats);
            _heirloomLabel.Text = "遺品: なし";
            UpdateContinueButton();
        }

        public void HideScreen()
        {
            SetScreenVisible(false);
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
                Size = new Vector2(1080f, 1920f)
            };
            AddChild(_overlay);

            _panel = new Panel
            {
                Position = new Vector2(70f, 120f),
                Size = new Vector2(940f, 1580f)
            };
            AddChild(_panel);

            _titleLabel = CreateLabel(new Vector2(60f, 42f), 52, HorizontalAlignment.Center);
            _titleLabel.Size = new Vector2(820f, 68f);
            _titleLabel.Text = "命尽きる";
            _panel.AddChild(_titleLabel);

            _causeLabel = CreateLabel(new Vector2(60f, 118f), 28, HorizontalAlignment.Center);
            _causeLabel.Size = new Vector2(820f, 56f);
            _panel.AddChild(_causeLabel);

            _statsLabel = CreateLabel(new Vector2(70f, 210f), 28);
            _statsLabel.Size = new Vector2(360f, 360f);
            _panel.AddChild(_statsLabel);

            _debtUi = new DebtUI
            {
                Position = new Vector2(460f, 210f),
                Size = new Vector2(620f, 280f)
            };
            _debtUi.OptionSelected += OnDebtOptionSelected;
            _panel.AddChild(_debtUi);

            _heirloomLabel = CreateLabel(new Vector2(70f, 610f), 28);
            _heirloomLabel.Size = new Vector2(820f, 36f);
            _heirloomLabel.Text = "遺品を選択";
            _panel.AddChild(_heirloomLabel);

            _continueButton = new Button
            {
                Position = new Vector2(300f, 1460f),
                Size = new Vector2(340f, 62f),
                Text = "次世代へ"
            };
            _continueButton.Pressed += OnContinuePressed;
            _panel.AddChild(_continueButton);

            _uiBuilt = true;
            ApplyTheme(_theme);
        }

        private void BuildHeirloomButtons(PlayerStats stats)
        {
            foreach (var button in _heirloomButtons)
            {
                button.QueueFree();
            }

            _heirloomButtons.Clear();
            var heirloomCandidates = stats.Inventory.EnumerateAllItems().ToList();
            heirloomCandidates.Insert(0, null);

            for (var index = 0; index < heirloomCandidates.Count && index < 16; index++)
            {
                var candidate = heirloomCandidates[index];
                var button = new Button
                {
                    Position = new Vector2(70f + (index % 2) * 390f, 660f + (index / 2) * 64f),
                    Size = new Vector2(360f, 52f),
                    Text = candidate == null ? "何も残さない" : candidate.GetSummary(),
                    ClipText = true
                };
                var localCandidate = candidate;
                button.Pressed += () => OnHeirloomSelected(localCandidate);
                _heirloomButtons.Add(button);
                _panel.AddChild(button);
            }
        }

        private void OnDebtOptionSelected(DebtPaymentKind kind)
        {
            _selectedDebtKind = kind;
            UpdateContinueButton();
        }

        private void OnHeirloomSelected(EquipmentData equipmentData)
        {
            _selectedHeirloom = equipmentData;
            _heirloomLabel.Text = equipmentData == null ? "遺品: なし" : $"遺品: {equipmentData.Name}";
        }

        private void OnContinuePressed()
        {
            ContinueRequested?.Invoke(_selectedDebtKind, _selectedHeirloom);
        }

        private void UpdateContinueButton()
        {
            _continueButton.Disabled = _currentDebtOptions.Count > 0 && !_currentDebtOptions.Any(option => option.Kind == _selectedDebtKind);
        }

        private Label CreateLabel(Vector2 position, int fontSize, HorizontalAlignment horizontalAlignment = HorizontalAlignment.Left)
        {
            var label = new Label
            {
                Position = position,
                HorizontalAlignment = horizontalAlignment,
                VerticalAlignment = VerticalAlignment.Center
            };
            label.AddThemeFontSizeOverride("font_size", fontSize);
            return label;
        }

        private void ApplyLabelTheme(Label label)
        {
            label.AddThemeColorOverride("font_color", _theme.TextColor);
        }

        private void SetScreenVisible(bool visible)
        {
            _isVisible = visible;
            if (!_uiBuilt)
            {
                return;
            }

            _overlay.Visible = visible;
            _panel.Visible = visible;
        }
    }
}