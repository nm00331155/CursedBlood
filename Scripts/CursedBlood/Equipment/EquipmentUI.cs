using System;
using CursedBlood.Player;
using Godot;

namespace CursedBlood.Equipment
{
    public partial class EquipmentUI : CanvasLayer
    {
        private PlayerStats _stats;
        private bool _uiBuilt;
        private ColorRect _overlay;
        private Panel _panel;
        private Label _messageLabel;
        private Button _closeButton;
        private readonly Button[] _equippedButtons = new Button[4];
        private readonly Button[] _bagButtons = new Button[20];

        public event Action Closed;

        public bool IsOpen => _panel?.Visible == true;

        public void Initialize(PlayerStats stats)
        {
            _stats = stats;
            BuildUiIfNeeded();
            Refresh();
            SetVisibleState(false);
        }

        public void Toggle()
        {
            if (IsOpen)
            {
                HidePanel();
            }
            else
            {
                ShowPanel();
            }
        }

        public void ShowPanel()
        {
            Refresh();
            SetVisibleState(true);
        }

        public void HidePanel()
        {
            SetVisibleState(false);
            Closed?.Invoke();
        }

        public void NotifyReplacement(EquipmentData picked, EquipmentData removed)
        {
            BuildUiIfNeeded();
            _messageLabel.Text = removed == null
                ? $"{picked.Name} を回収"
                : $"バッグ満杯: {removed.Name} を捨てて {picked.Name} を回収";
        }

        public void Refresh()
        {
            if (_stats == null)
            {
                return;
            }

            for (var index = 0; index < _equippedButtons.Length; index++)
            {
                var item = _stats.Inventory.EquippedSlots[index];
                _equippedButtons[index].Text = item == null
                    ? $"{(EquipmentCategory)index}: 空き"
                    : $"{(EquipmentCategory)index}: {item.Name}";
            }

            for (var index = 0; index < _bagButtons.Length; index++)
            {
                if (index < _stats.Inventory.Bag.Count)
                {
                    _bagButtons[index].Text = _stats.Inventory.Bag[index].GetSummary();
                    _bagButtons[index].Disabled = false;
                    _bagButtons[index].Visible = true;
                }
                else
                {
                    _bagButtons[index].Text = string.Empty;
                    _bagButtons[index].Disabled = true;
                    _bagButtons[index].Visible = false;
                }
            }
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
                Size = new Vector2(1080f, 1920f),
                Color = new Color(0f, 0f, 0f, 0.65f)
            };
            AddChild(_overlay);

            _panel = new Panel
            {
                Position = new Vector2(70f, 200f),
                Size = new Vector2(940f, 1380f)
            };
            AddChild(_panel);

            var title = new Label
            {
                Position = new Vector2(40f, 24f),
                Size = new Vector2(520f, 40f),
                Text = "装備とバッグ"
            };
            title.AddThemeFontSizeOverride("font_size", 30);
            _panel.AddChild(title);

            _closeButton = new Button
            {
                Position = new Vector2(760f, 24f),
                Size = new Vector2(140f, 44f),
                Text = "閉じる"
            };
            _closeButton.Pressed += HidePanel;
            _panel.AddChild(_closeButton);

            _messageLabel = new Label
            {
                Position = new Vector2(40f, 78f),
                Size = new Vector2(860f, 36f)
            };
            _messageLabel.AddThemeFontSizeOverride("font_size", 18);
            _panel.AddChild(_messageLabel);

            for (var index = 0; index < _equippedButtons.Length; index++)
            {
                var slotIndex = index;
                var button = new Button
                {
                    Position = new Vector2(40f, 140f + index * 76f),
                    Size = new Vector2(360f, 56f)
                };
                button.Pressed += () => OnEquippedSlotPressed((EquipmentCategory)slotIndex);
                _equippedButtons[index] = button;
                _panel.AddChild(button);
            }

            for (var index = 0; index < _bagButtons.Length; index++)
            {
                var bagIndex = index;
                var button = new Button
                {
                    Position = new Vector2(430f, 140f + index * 56f),
                    Size = new Vector2(470f, 48f),
                    ClipText = true
                };
                button.Pressed += () => OnBagButtonPressed(bagIndex);
                _bagButtons[index] = button;
                _panel.AddChild(button);
            }

            _uiBuilt = true;
        }

        private void OnEquippedSlotPressed(EquipmentCategory category)
        {
            _stats?.Inventory.Unequip(category);
            _stats?.RefreshDerivedStats();
            Refresh();
        }

        private void OnBagButtonPressed(int index)
        {
            _stats?.Inventory.EquipFromBag(index);
            _stats?.RefreshDerivedStats();
            Refresh();
        }

        private void SetVisibleState(bool visible)
        {
            BuildUiIfNeeded();
            _overlay.Visible = visible;
            _panel.Visible = visible;
        }
    }
}