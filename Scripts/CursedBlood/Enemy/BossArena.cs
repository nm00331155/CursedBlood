using System.Collections.Generic;
using Godot;

namespace CursedBlood.Enemy
{
    public static class BossArena
    {
        public static IEnumerable<Vector2I> EnumerateArena(Vector2I center, bool isDemonLord)
        {
            var radius = isDemonLord ? 5 : 3;
            for (var row = center.Y - radius; row <= center.Y + radius; row++)
            {
                for (var column = 0; column < 7; column++)
                {
                    yield return new Vector2I(column, row);
                }
            }
        }

        public static IEnumerable<Vector2I> EnumerateBossCells(Vector2I center, bool isDemonLord)
        {
            var radius = isDemonLord ? 2 : 1;
            var minColumn = isDemonLord ? 1 : 2;
            var maxColumn = isDemonLord ? 5 : 4;

            for (var row = center.Y - radius; row <= center.Y + radius; row++)
            {
                for (var column = minColumn; column <= maxColumn; column++)
                {
                    yield return new Vector2I(column, row);
                }
            }
        }
    }
}