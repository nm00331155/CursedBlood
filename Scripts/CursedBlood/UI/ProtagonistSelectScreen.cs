using System;
using Godot;

namespace CursedBlood.UI
{
    public partial class ProtagonistSelectScreen : CanvasLayer
    {
        private static readonly Vector2 PanelDesignPosition = new(60f, 92f);

        private static readonly string[] MaleNames = { "アキラ", "ハヤト", "ソウマ", "レン", "トウマ", "ユウト" };
        private static readonly string[] FemaleNames = { "ユイ", "ミオ", "リン", "サクラ", "ナギサ", "ヒナ" };

        private readonly RandomNumberGenerator _rng = new();
        private bool _built;
        private bool _returnToHub;
        private string _selectedGender = "男";
        private ColorRect _background;
        private Panel _panel;
        private Button _maleCard;
        private Button _femaleCard;
        private LineEdit _nameEdit;
        private Button _confirmButton;

        public event Action<string, string> Confirmed;

        public event Action<bool> CancelRequested;

        public void Initialize()
        {
            BuildUiIfNeeded();
            ApplyViewportLayout();
            HideScreen();
        }

        public void ShowScreen(CursedBlood.Save.PlayerProfileData profile, bool returnToHub)
        {
            BuildUiIfNeeded();
            ApplyViewportLayout();
            _returnToHub = returnToHub;
            _selectedGender = profile.IsProfileConfigured && profile.Gender == "女" ? "女" : "男";
            _nameEdit.Text = profile.IsProfileConfigured ? profile.Name : CreateRandomName(_selectedGender);
            RefreshSelection();
            SetVisibleState(true);
            CallDeferred(MethodName.FocusPrimaryAction);
        }

        public void HideScreen()
        {
            SetVisibleState(false);
        }

        public void ApplyViewportLayout()
        {
            if (!_built)
            {
                return;
            }

            CanvasLayoutHelper.StretchOverlay(this, _background);
            CanvasLayoutHelper.CenterFromReference(this, _panel, PanelDesignPosition);
        }

        private void BuildUiIfNeeded()
        {
            if (_built)
            {
                return;
            }

            _background = new ColorRect
            {
                Position = Vector2.Zero,
                Size = new Vector2(1080f, 1920f),
                Color = new Color(0.06f, 0.08f, 0.10f, 1f)
            };
            AddChild(_background);

            _panel = new Panel
            {
                Position = PanelDesignPosition,
                Size = new Vector2(960f, 1600f)
            };
            var panelStyle = new StyleBoxFlat
            {
                BgColor = new Color(0.10f, 0.12f, 0.16f, 0.96f),
                BorderColor = new Color(0.82f, 0.88f, 0.94f),
                BorderWidthTop = 3,
                BorderWidthBottom = 3,
                BorderWidthLeft = 3,
                BorderWidthRight = 3,
                CornerRadiusTopLeft = 24,
                CornerRadiusTopRight = 24,
                CornerRadiusBottomLeft = 24,
                CornerRadiusBottomRight = 24
            };
            _panel.AddThemeStyleboxOverride("panel", panelStyle);
            AddChild(_panel);

            var title = CreateLabel(new Vector2(90f, 42f), new Vector2(780f, 72f), 56, HorizontalAlignment.Center);
            title.Text = "主人公選択";
            _panel.AddChild(title);

            var sub = CreateLabel(new Vector2(112f, 118f), new Vector2(736f, 64f), 28, HorizontalAlignment.Center);
            sub.Text = "立ち絵未実装のため、仮カードで表示しています。";
            _panel.AddChild(sub);

            _maleCard = CreateCardButton(new Vector2(86f, 246f), "男\n仮立ち絵\nPlaceholder");
            _maleCard.Pressed += () =>
            {
                _selectedGender = "男";
                RefreshSelection();
            };
            _panel.AddChild(_maleCard);

            _femaleCard = CreateCardButton(new Vector2(522f, 246f), "女\n仮立ち絵\nPlaceholder");
            _femaleCard.Pressed += () =>
            {
                _selectedGender = "女";
                RefreshSelection();
            };
            _panel.AddChild(_femaleCard);

            var nameLabel = CreateLabel(new Vector2(118f, 904f), new Vector2(220f, 44f), 30, HorizontalAlignment.Left);
            nameLabel.Text = "名前";
            _panel.AddChild(nameLabel);

            _nameEdit = new LineEdit
            {
                Position = new Vector2(118f, 958f),
                Size = new Vector2(500f, 70f),
                PlaceholderText = "主人公名を入力"
            };
            _nameEdit.AddThemeFontSizeOverride("font_size", 28);
            _nameEdit.TextChanged += _ => UpdateConfirmEnabled();
            _panel.AddChild(_nameEdit);

            var randomButton = new Button
            {
                Position = new Vector2(638f, 958f),
                Size = new Vector2(200f, 70f),
                Text = "ランダム"
            };
            randomButton.AddThemeFontSizeOverride("font_size", 24);
            randomButton.Pressed += () =>
            {
                _nameEdit.Text = CreateRandomName(_selectedGender);
                UpdateConfirmEnabled();
            };
            _panel.AddChild(randomButton);

            _confirmButton = new Button
            {
                Position = new Vector2(252f, 1160f),
                Size = new Vector2(456f, 96f),
                Text = "この主人公で開始"
            };
            _confirmButton.AddThemeFontSizeOverride("font_size", 36);
            _confirmButton.Pressed += () => Confirmed?.Invoke(_selectedGender, _nameEdit.Text.Trim());
            _panel.AddChild(_confirmButton);

            var cancelButton = new Button
            {
                Position = new Vector2(252f, 1276f),
                Size = new Vector2(456f, 78f),
                Text = "戻る"
            };
            cancelButton.AddThemeFontSizeOverride("font_size", 24);
            cancelButton.Pressed += () => CancelRequested?.Invoke(_returnToHub);
            _panel.AddChild(cancelButton);

            _built = true;
            UpdateConfirmEnabled();
            ApplyViewportLayout();
        }

        private void RefreshSelection()
        {
            ApplyCardStyle(_maleCard, _selectedGender == "男", new Color(0.25f, 0.52f, 0.94f));
            ApplyCardStyle(_femaleCard, _selectedGender == "女", new Color(0.94f, 0.38f, 0.42f));
            UpdateConfirmEnabled();
        }

        private void UpdateConfirmEnabled()
        {
            if (_confirmButton == null)
            {
                return;
            }

            _confirmButton.Disabled = string.IsNullOrWhiteSpace(_nameEdit.Text);
        }

        private void FocusPrimaryAction()
        {
            if (!_confirmButton.Disabled)
            {
                _confirmButton.GrabFocus();
                return;
            }

            _maleCard.GrabFocus();
        }

        private static Button CreateCardButton(Vector2 position, string text)
        {
            var button = new Button
            {
                Position = position,
                Size = new Vector2(352f, 560f),
                Text = text,
                Alignment = HorizontalAlignment.Center,
                VerticalIconAlignment = VerticalAlignment.Center
            };
            button.AddThemeFontSizeOverride("font_size", 36);
            return button;
        }

        private static void ApplyCardStyle(Button button, bool selected, Color accent)
        {
            var style = new StyleBoxFlat
            {
                BgColor = selected ? accent.Lightened(0.10f) : new Color(0.16f, 0.18f, 0.24f, 0.95f),
                BorderColor = selected ? new Color(0.96f, 0.97f, 0.99f) : accent,
                BorderWidthTop = selected ? 4 : 2,
                BorderWidthBottom = selected ? 4 : 2,
                BorderWidthLeft = selected ? 4 : 2,
                BorderWidthRight = selected ? 4 : 2,
                CornerRadiusTopLeft = 22,
                CornerRadiusTopRight = 22,
                CornerRadiusBottomLeft = 22,
                CornerRadiusBottomRight = 22
            };
            button.AddThemeStyleboxOverride("normal", style);
            button.AddThemeStyleboxOverride("hover", style);
            button.AddThemeStyleboxOverride("pressed", style);
        }

        private string CreateRandomName(string gender)
        {
            var source = gender == "女" ? FemaleNames : MaleNames;
            return source[_rng.RandiRange(0, source.Length - 1)];
        }

        private void SetVisibleState(bool visible)
        {
            if (!_built)
            {
                return;
            }

            _background.Visible = visible;
            _panel.Visible = visible;
        }

        private static Label CreateLabel(Vector2 position, Vector2 size, int fontSize, HorizontalAlignment alignment)
        {
            var label = new Label
            {
                Position = position,
                Size = size,
                HorizontalAlignment = alignment,
                VerticalAlignment = VerticalAlignment.Center,
                AutowrapMode = TextServer.AutowrapMode.WordSmart
            };
            label.AddThemeFontSizeOverride("font_size", fontSize);
            label.AddThemeColorOverride("font_color", new Color(0.96f, 0.97f, 0.99f));
            return label;
        }
    }
}