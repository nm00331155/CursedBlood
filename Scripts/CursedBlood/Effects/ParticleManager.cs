using System.Collections.Generic;
using Godot;

namespace CursedBlood.Effects
{
    public partial class ParticleManager : Node2D
    {
        private sealed class ParticleBurst
        {
            public Vector2 Position { get; set; }

            public Color Color { get; set; }

            public float Lifetime { get; set; }

            public float Radius { get; set; }
        }

        private readonly List<ParticleBurst> _bursts = new();

        public override void _Process(double delta)
        {
            if (_bursts.Count == 0)
            {
                return;
            }

            for (var index = _bursts.Count - 1; index >= 0; index--)
            {
                _bursts[index].Lifetime -= (float)delta;
                if (_bursts[index].Lifetime <= 0f)
                {
                    _bursts.RemoveAt(index);
                }
            }

            QueueRedraw();
        }

        public override void _Draw()
        {
            foreach (var burst in _bursts)
            {
                var alpha = Mathf.Clamp(burst.Lifetime, 0f, 1f);
                DrawCircle(burst.Position, burst.Radius * alpha, new Color(burst.Color.R, burst.Color.G, burst.Color.B, alpha));
            }
        }

        public void SpawnDigParticle(Vector2 position, Color color)
        {
            _bursts.Add(new ParticleBurst { Position = position, Color = color, Lifetime = 0.35f, Radius = 18f });
            QueueRedraw();
        }

        public void SpawnEnemyDeathParticle(Vector2 position, Color color)
        {
            _bursts.Add(new ParticleBurst { Position = position, Color = color, Lifetime = 0.45f, Radius = 24f });
            QueueRedraw();
        }

        public void SpawnItemDropParticle(Vector2 position, Color color)
        {
            _bursts.Add(new ParticleBurst { Position = position, Color = color, Lifetime = 0.50f, Radius = 20f });
            QueueRedraw();
        }

        public void SpawnBossExplosion(Vector2 position)
        {
            _bursts.Add(new ParticleBurst { Position = position, Color = new Color(1f, 0.4f, 0.2f), Lifetime = 0.80f, Radius = 40f });
            QueueRedraw();
        }
    }
}