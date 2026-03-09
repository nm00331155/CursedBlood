using Godot;

namespace CursedBlood.Enemy
{
    public static class DebtCollectorEnemy
    {
        public const int ContactDamage = 15;

        public static Vector2I GetStep(Vector2I currentPosition, Vector2I targetPosition)
        {
            var delta = targetPosition - currentPosition;
            if (Mathf.Abs(delta.X) > Mathf.Abs(delta.Y))
            {
                return new Vector2I(delta.X > 0 ? 1 : -1, 0);
            }

            if (delta.Y != 0)
            {
                return new Vector2I(0, delta.Y > 0 ? 1 : -1);
            }

            return Vector2I.Zero;
        }
    }
}