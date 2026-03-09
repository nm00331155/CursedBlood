using System;
using CursedBlood.Player;
using Godot;

namespace CursedBlood.UI
{
    public partial class HUDManager : CanvasLayer
    {
        private PlayerStats _stats;
        private PlayerController _controller;
        private bool _built;
        private bool _debugVisible;
        private bool _returnVisible;
        private string _returnButtonText = "帰還";
        private string _returnStatusText = string.Empty;
        private string _sonarText = "ソナー: --";
        private float _virtualPadOpacity = 0.28f;
        private ColorRect _topBackground;
        private ColorRect _bottomBackground;
        private ColorRect _oxygenBarBackground;
        private ColorRect _oxygenBarFill;
        private Label _timerLabel;
        private Label _phaseLabel;
        private Label _depthLabel;
        private Label _debtLabel;
        private Label _moneyLabel;
        private Label _salvageLabel;
        private Label _hpLabel;
        private Label _sonarLabel;
        private Label _hintLabel;
        private Label _returnStatusLabel;
        private Label _movementLabel;
        private Label _debugLabel;
        private Button _returnButton;

        public event Action ReturnRequested;

        public override void _Ready()
        {
            SetProcess(true);
        }

        public void Initialize(PlayerStats stats, PlayerController controller)
        {
            _stats = stats;
            _controller = controller;
            BuildUiIfNeeded();
            SetDebugVisible(_debugVisible);
        }

        public void SetDiveContext(string sonarText, bool canReturn, string returnButtonText, string returnStatusText, float virtualPadOpacity)
        {
            _sonarText = sonarText;
            _returnVisible = canReturn;
            _returnButtonText = returnButtonText;
            _returnStatusText = returnStatusText;
            _virtualPadOpacity = virtualPadOpacity;

            if (_returnButton != null)
            {
                _returnButton.Visible = canReturn;
                _returnButton.Text = returnButtonText;
            }
        }

        public void SetDebugVisible(bool visible)
        {
            _debugVisible = visible;
            if (_debugLabel != null)
            {
                _debugLabel.Visible = visible;
            }

            UpdateHintText();
        }

        public override void _Process(double delta)
        {
            if (_stats == null || !_built)
            {
                return;
            }

            var oxygenRatio = _stats.OxygenRatio;
            _oxygenBarFill.Size = new Vector2(420f * oxygenRatio, 28f);
            _oxygenBarFill.Color = _stats.Phase switch
            {
                DivePhase.Critical => new Color(0.95f, 0.30f, 0.22f),
                DivePhase.Worn => new Color(0.95f, 0.67f, 0.18f),
                _ => new Color(0.28f, 0.82f, 0.52f)
            };

            _timerLabel.Text = $"酸素 {oxygenRatio * 100f:0}% / {_stats.CurrentDiveTime:00.0}s";
            _phaseLabel.Text = _stats.Phase switch
            {
                DivePhase.Stable => "Stable / フィルタ 100% / 移動 x1.00",
                DivePhase.Worn => "Worn / フィルタ 65% / 移動 x0.82",
                DivePhase.Critical => "Critical / フィルタ 28% / 移動 x0.62",
                _ => string.Empty
            };
            _depthLabel.Text = $"深度 {_stats.GridPosition.Y}m";
            _debtLabel.Text = $"借金 {_stats.CurrentDebt:N0}";
            _moneyLabel.Text = $"所持金 {_stats.CurrentMoney:N0}";
            _salvageLabel.Text = $"未換金 {_stats.SalvageValue:N0}";
            _hpLabel.Text = $"HP {_stats.CurrentHp}/{_stats.MaxHp}   鉱石 {_stats.OresCollected}   掘削 {_stats.BlocksDug}";
            _sonarLabel.Text = _sonarText;
            _returnStatusLabel.Text = _returnStatusText;
            _returnButton.Visible = _returnVisible;
            _returnButton.Text = _returnButtonText;

            var movementText = _controller?.MovementStatusText ?? string.Empty;
            _movementLabel.Text = movementText;
            _movementLabel.Visible = !string.IsNullOrEmpty(movementText);
            _movementLabel.AddThemeColorOverride(
                "font_color",
                movementText.StartsWith("Blocked") ? new Color(1f, 0.72f, 0.72f) : new Color(0.82f, 0.93f, 1f));

            _debugLabel.Text = _debugVisible && _controller != null ? _controller.GetDebugSummary() : string.Empty;
            _debugLabel.Visible = _debugVisible;
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

            _oxygenBarBackground = new ColorRect
            {
                Position = new Vector2(32f, 78f),
                Size = new Vector2(420f, 28f),
                Color = new Color(0.19f, 0.23f, 0.28f)
            };
            AddChild(_oxygenBarBackground);

            _oxygenBarFill = new ColorRect
            {
                Position = new Vector2(32f, 78f),
                Size = new Vector2(420f, 28f),
                Color = new Color(0.28f, 0.82f, 0.52f)
            };
            AddChild(_oxygenBarFill);

            _timerLabel = CreateLabel(new Vector2(32f, 22f), 30, HorizontalAlignment.Left, new Vector2(320f, 36f));
            AddChild(_timerLabel);

            _phaseLabel = CreateLabel(new Vector2(32f, 146f), 24, HorizontalAlignment.Left, new Vector2(420f, 32f));
            AddChild(_phaseLabel);

            _depthLabel = CreateLabel(new Vector2(360f, 26f), 38, HorizontalAlignment.Center, new Vector2(360f, 44f));
            AddChild(_depthLabel);

            _debtLabel = CreateLabel(new Vector2(720f, 22f), 28, HorizontalAlignment.Right, new Vector2(328f, 34f));
            AddChild(_debtLabel);

            _moneyLabel = CreateLabel(new Vector2(720f, 58f), 24, HorizontalAlignment.Right, new Vector2(328f, 30f));
            AddChild(_moneyLabel);

            _salvageLabel = CreateLabel(new Vector2(720f, 92f), 24, HorizontalAlignment.Right, new Vector2(328f, 30f));
            AddChild(_salvageLabel);

            _hpLabel = CreateLabel(new Vector2(32f, 1640f), 32, HorizontalAlignment.Left, new Vector2(460f, 42f));
            AddChild(_hpLabel);

            _sonarLabel = CreateLabel(new Vector2(32f, 1684f), 24, HorizontalAlignment.Left, new Vector2(520f, 34f));
            AddChild(_sonarLabel);

            _movementLabel = CreateLabel(new Vector2(568f, 1640f), 24, HorizontalAlignment.Right, new Vector2(480f, 34f));
            _movementLabel.Visible = false;
            AddChild(_movementLabel);

            _returnStatusLabel = CreateLabel(new Vector2(568f, 1680f), 22, HorizontalAlignment.Right, new Vector2(480f, 30f));
            AddChild(_returnStatusLabel);

            _returnButton = new Button
            {
                Position = new Vector2(748f, 1722f),
                Size = new Vector2(300f, 84f),
                Visible = false,
                Text = _returnButtonText
            };
            _returnButton.AddThemeFontSizeOverride("font_size", 28);
            _returnButton.Pressed += () => ReturnRequested?.Invoke();
            AddChild(_returnButton);

            _hintLabel = CreateLabel(new Vector2(32f, 1730f), 22, HorizontalAlignment.Left, new Vector2(680f, 54f));
            _hintLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            AddChild(_hintLabel);

            _debugLabel = CreateLabel(new Vector2(32f, 1800f), 18, HorizontalAlignment.Left, new Vector2(1016f, 96f));
            _debugLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            _debugLabel.Visible = false;
            AddChild(_debugLabel);

            _built = true;
            UpdateHintText();
        }

        private void UpdateHintText()
        {
            if (_hintLabel == null)
            {
                return;
            }

            _hintLabel.Text = _debugVisible
                ? $"矢印キー同時押しで斜め / 画面タッチで動的パッド / 長押しでGuard / [ ]で透過率 {Mathf.RoundToInt(_virtualPadOpacity * 100f)}% / F3 Debug ON"
                : $"矢印キー同時押しで斜め / 画面タッチで動的パッド / 長押しでGuard / [ ]で透過率 {Mathf.RoundToInt(_virtualPadOpacity * 100f)}% / F3でDebug";
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