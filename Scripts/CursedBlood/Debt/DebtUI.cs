using System;
using System.Collections.Generic;
using Godot;

namespace CursedBlood.Debt
{
    public partial class DebtUI : Control
    {
        private readonly List<Button> _buttons = new();
        private Label _titleLabel;

        public DebtPaymentKind SelectedKind { get; private set; } = DebtPaymentKind.None;

        public event Action<DebtPaymentKind> OptionSelected;

        public override void _Ready()
        {
            Visible = false;
            _titleLabel = new Label
            {
                Position = new Vector2(0f, 0f),
                Size = new Vector2(640f, 36f),
                Text = "返済額を選択"
            };
            _titleLabel.AddThemeFontSizeOverride("font_size", 24);
            AddChild(_titleLabel);
        }

        public void SetOptions(IReadOnlyList<DebtPaymentOption> options)
        {
            foreach (var button in _buttons)
            {
                button.QueueFree();
            }

            _buttons.Clear();
            SelectedKind = DebtPaymentKind.None;

            for (var index = 0; index < options.Count; index++)
            {
                var option = options[index];
                var button = new Button
                {
                    Position = new Vector2(0f, 52f + index * 52f),
                    Size = new Vector2(620f, 44f),
                    Text = option.Label,
                    Disabled = !option.IsAvailable
                };
                var selectedKind = option.Kind;
                button.Pressed += () => OnOptionPressed(selectedKind);
                _buttons.Add(button);
                AddChild(button);
            }

            Visible = true;
        }

        private void OnOptionPressed(DebtPaymentKind kind)
        {
            SelectedKind = kind;
            OptionSelected?.Invoke(kind);
        }
    }
}