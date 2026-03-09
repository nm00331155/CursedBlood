using Godot;

namespace CursedBlood.Equipment
{
    public sealed class DroppedItem
    {
        public EquipmentData Item { get; set; }

        public Vector2I GridPosition { get; set; }

        public string Label => Item?.Name ?? string.Empty;

        public Color GetColor()
        {
            return Item?.GetRarityColor() ?? Colors.White;
        }
    }
}