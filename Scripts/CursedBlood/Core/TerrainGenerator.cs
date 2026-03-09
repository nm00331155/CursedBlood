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
            Span<byte> previousRow = stackalloc byte[ChunkData.Width];
            Span<byte> rowBuffer = stackalloc byte[ChunkData.Width];

            for (var localRow = 0; localRow < ChunkData.Height; localRow++)
            {
                var absoluteRow = chunk.StartRow + localRow;
                FillRow(rowBuffer, previousRow, absoluteRow);
                EnsurePassage(rowBuffer, absoluteRow);

                for (var col = 0; col < ChunkData.Width; col++)
                {
                    chunk.SetCell(col, localRow, rowBuffer[col]);
                    previousRow[col] = rowBuffer[col];
                }
            }
        }

        public static int GetDepthTier(int absoluteRow)
        {
            return absoluteRow switch
            {
                < 200 => 0,
                < 500 => 1,
                < 1000 => 2,
                < 3000 => 3,
                _ => 4
            };
        }

        private void FillRow(Span<byte> rowBuffer, Span<byte> previousRow, int absoluteRow)
        {
            var distribution = GetDistribution(absoluteRow);
            var makeBand = absoluteRow >= 32 && absoluteRow % 10 == 0;
            var openingStart = 4 + Hash(absoluteRow, 97) % 49;
            var openingWidth = 10 + Hash(absoluteRow, 211) % 5;
            var openingEnd = Mathf.Min(ChunkData.Width - 4, openingStart + openingWidth);

            for (var col = 0; col < ChunkData.Width; col++)
            {
                if (makeBand)
                {
                    rowBuffer[col] = (byte)(col >= openingStart && col < openingEnd ? CellType.Dirt : CellType.Bedrock);
                    continue;
                }

                var isEdge = col <= 2 || col >= ChunkData.Width - 3;
                if (isEdge && Hash01(col, absoluteRow, 17) < 0.5f)
                {
                    rowBuffer[col] = (byte)CellType.Bedrock;
                    continue;
                }

                var emptyBias = GetOpenBias(previousRow, col);
                var roll = Hash01(col, absoluteRow, 53);

                if (roll < distribution.Empty + emptyBias)
                {
                    rowBuffer[col] = (byte)CellType.Empty;
                }
                else if (roll < distribution.Empty + emptyBias + distribution.Dirt)
                {
                    rowBuffer[col] = (byte)CellType.Dirt;
                }
                else if (roll < distribution.Empty + emptyBias + distribution.Dirt + distribution.Stone)
                {
                    rowBuffer[col] = (byte)CellType.Stone;
                }
                else if (roll < distribution.Empty + emptyBias + distribution.Dirt + distribution.Stone + distribution.HardRock)
                {
                    rowBuffer[col] = (byte)CellType.HardRock;
                }
                else
                {
                    rowBuffer[col] = (byte)CellType.Bedrock;
                }
            }
        }

        private void EnsurePassage(Span<byte> rowBuffer, int absoluteRow)
        {
            var longestStart = -1;
            var longestLength = 0;
            var currentStart = -1;
            var currentLength = 0;

            for (var col = 0; col < ChunkData.Width; col++)
            {
                if ((CellType)rowBuffer[col] != CellType.Bedrock)
                {
                    if (currentStart < 0)
                    {
                        currentStart = col;
                        currentLength = 0;
                    }

                    currentLength++;
                    if (currentLength > longestLength)
                    {
                        longestLength = currentLength;
                        longestStart = currentStart;
                    }
                }
                else
                {
                    currentStart = -1;
                    currentLength = 0;
                }
            }

            if (longestLength >= 10)
            {
                return;
            }

            var openingStart = 8 + Hash(absoluteRow, 401) % 40;
            var openingWidth = 12;
            for (var col = openingStart; col < openingStart + openingWidth && col < ChunkData.Width - 4; col++)
            {
                rowBuffer[col] = (byte)(Hash01(col, absoluteRow, 877) < 0.3f ? CellType.Empty : CellType.Dirt);
            }
        }

        private static float GetOpenBias(Span<byte> previousRow, int col)
        {
            if (previousRow.IsEmpty)
            {
                return 0f;
            }

            var bias = 0f;
            if ((CellType)previousRow[col] == CellType.Empty)
            {
                bias += 0.16f;
            }

            if (col > 0 && (CellType)previousRow[col - 1] == CellType.Empty)
            {
                bias += 0.05f;
            }

            if (col < previousRow.Length - 1 && (CellType)previousRow[col + 1] == CellType.Empty)
            {
                bias += 0.05f;
            }

            return Mathf.Clamp(bias, 0f, 0.2f);
        }

        private Distribution GetDistribution(int absoluteRow)
        {
            return absoluteRow switch
            {
                < 200 => new Distribution(0.65f, 0.15f, 0.02f, 0.03f, 0.15f),
                < 500 => new Distribution(0.50f, 0.25f, 0.08f, 0.05f, 0.12f),
                < 1000 => new Distribution(0.35f, 0.30f, 0.15f, 0.08f, 0.12f),
                < 3000 => new Distribution(0.20f, 0.35f, 0.25f, 0.10f, 0.10f),
                _ => new Distribution(0.10f, 0.30f, 0.35f, 0.15f, 0.10f)
            };
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

        private readonly record struct Distribution(float Dirt, float Stone, float HardRock, float Bedrock, float Empty);
    }
}