using CursedBlood.Player;
using Godot;

namespace CursedBlood.Core
{
    public sealed class TerrainGenerator
    {
        private readonly int _seed;

        public TerrainGenerator(int seed)
        {
            _seed = seed;
        }

        public void FillChunk(ChunkData chunk)
        {
            for (var localRow = 0; localRow < ChunkData.Height; localRow++)
            {
                var absoluteRow = chunk.StartRow + localRow;
                for (var localCol = 0; localCol < ChunkData.Width; localCol++)
                {
                    var absoluteCol = chunk.StartCol + localCol;
                    chunk.SetCell(localCol, localRow, GenerateCell(absoluteCol, absoluteRow));
                }
            }
        }

        public static int GetDepthTier(int absoluteRow)
        {
            var depth = Mathf.Max(0, absoluteRow - PlayerStats.SurfaceRow);
            return depth switch
            {
                < 120 => 0,
                < 320 => 1,
                < 720 => 2,
                < 1400 => 3,
                _ => 4
            };
        }

        private byte GenerateCell(int col, int row)
        {
            if (row < PlayerStats.SurfaceRow)
            {
                return (byte)CellType.Bedrock;
            }

            if (IsInsideStartPocket(col, row))
            {
                return (byte)CellType.Empty;
            }

            var depth = Mathf.Max(0, row - PlayerStats.SurfaceRow);
            var cavernNoise = FractalNoise(col * 0.072f, row * 0.048f, 3, 2.08f, 0.52f, 137);
            var detailNoise = FractalNoise(col * 0.182f, row * 0.118f, 2, 2.20f, 0.50f, 233);
            var stratumNoise = FractalNoise(col * 0.052f, row * 0.031f, 3, 2.02f, 0.56f, 389);
            var flowNoise = FractalNoise(col * 0.038f, row * 0.020f, 2, 2.06f, 0.53f, 521);
            var pocketNoise = FractalNoise(col * 0.116f, row * 0.066f, 2, 2.12f, 0.48f, 613);
            var seamNoise = 0.5f + (0.5f * Mathf.Sin((row * 0.070f) + (col * 0.014f) + (_seed * 0.0014f)));

            var openThreshold = depth switch
            {
                < 80 => 0.80f,
                < 220 => 0.84f,
                < 520 => 0.88f,
                _ => 0.90f
            };

            if (cavernNoise > openThreshold || (cavernNoise > openThreshold - 0.032f && detailNoise > 0.73f) || (flowNoise > 0.82f && pocketNoise > 0.46f))
            {
                return (byte)CellType.Empty;
            }

            var oreChance = depth switch
            {
                < 80 => 0.014f,
                < 220 => 0.024f,
                < 520 => 0.034f,
                < 900 => 0.046f,
                _ => 0.058f
            };

            if (Hash01(col, row, 991) < oreChance && detailNoise > 0.34f)
            {
                return (byte)CellType.Ore;
            }

            var hardnessScore = stratumNoise + (seamNoise * 0.38f);
            var stoneThreshold = depth switch
            {
                < 120 => 0.90f,
                < 320 => 0.82f,
                < 720 => 0.75f,
                _ => 0.69f
            };
            var hardRockThreshold = depth switch
            {
                < 120 => 1.20f,
                < 320 => 1.10f,
                < 720 => 1.02f,
                _ => 0.94f
            };

            if (hardnessScore > hardRockThreshold)
            {
                return (byte)CellType.HardRock;
            }

            if (hardnessScore > stoneThreshold)
            {
                return (byte)CellType.Stone;
            }

            return (byte)CellType.Dirt;
        }

        private static bool IsInsideStartPocket(int col, int row)
        {
            var dx = Mathf.Abs(col - PlayerStats.StartGridPosition.X);
            if (row < PlayerStats.SurfaceRow || row > PlayerStats.StartGridPosition.Y + 2)
            {
                return false;
            }

            if (dx <= 7 && row <= PlayerStats.SurfaceRow + 1)
            {
                return true;
            }

            return dx <= 3 && row <= PlayerStats.StartGridPosition.Y + 2;
        }

        private float FractalNoise(float x, float y, int octaves, float lacunarity, float gain, int salt)
        {
            var amplitude = 1f;
            var frequency = 1f;
            var total = 0f;
            var totalAmplitude = 0f;

            for (var index = 0; index < octaves; index++)
            {
                total += ValueNoise(x * frequency, y * frequency, salt + (index * 97)) * amplitude;
                totalAmplitude += amplitude;
                amplitude *= gain;
                frequency *= lacunarity;
            }

            return total / Mathf.Max(totalAmplitude, 0.0001f);
        }

        private float ValueNoise(float x, float y, int salt)
        {
            var x0 = Mathf.FloorToInt(x);
            var y0 = Mathf.FloorToInt(y);
            var x1 = x0 + 1;
            var y1 = y0 + 1;
            var tx = Smooth(x - x0);
            var ty = Smooth(y - y0);

            var v00 = Hash01(x0, y0, salt);
            var v10 = Hash01(x1, y0, salt);
            var v01 = Hash01(x0, y1, salt);
            var v11 = Hash01(x1, y1, salt);

            var a = Mathf.Lerp(v00, v10, tx);
            var b = Mathf.Lerp(v01, v11, tx);
            return Mathf.Lerp(a, b, ty);
        }

        private float Hash01(int col, int row, int salt)
        {
            return (Hash(col * 92821 + salt, row * 68917 + salt * 13) & 0x7fffffff) / (float)int.MaxValue;
        }

        private int Hash(int a, int b)
        {
            unchecked
            {
                var value = _seed;
                value = (value * 397) ^ a;
                value = (value * 397) ^ b;
                value ^= value << 13;
                value ^= value >> 17;
                value ^= value << 5;
                return value & 0x7fffffff;
            }
        }

        private static float Smooth(float value)
        {
            return value * value * (3f - (2f * value));
        }
    }
}