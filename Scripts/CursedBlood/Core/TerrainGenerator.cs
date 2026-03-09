using Godot;

namespace CursedBlood.Core
{
    public sealed class TerrainGenerator
    {
        private const int InitialPassageCenter = 33;
        private const int MinPassageCenter = 10;
        private const int MaxPassageCenter = 56;
        private const int PassageSegmentLength = 12;

        private readonly int _seed;
        private readonly Dictionary<int, int> _passageCenterCache = new();

        public TerrainGenerator(int seed)
        {
            _seed = seed;
            _passageCenterCache[0] = InitialPassageCenter;
        }

        public void FillChunk(ChunkData chunk)
        {
            Span<byte> previousRow = stackalloc byte[ChunkData.Width];
            Span<byte> rowBuffer = stackalloc byte[ChunkData.Width];
            previousRow.Clear();

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
            var passageStart = GetPassageStart(absoluteRow);
            var passageEnd = GetPassageEnd(absoluteRow);
            var makeBand = absoluteRow >= 48 && absoluteRow % 12 == 0;

            for (var col = 0; col < ChunkData.Width; col++)
            {
                var insidePassage = col >= passageStart && col <= passageEnd;
                if (makeBand)
                {
                    rowBuffer[col] = insidePassage ? ChoosePassageCellType(absoluteRow, col) : (byte)CellType.Bedrock;
                    continue;
                }

                var isEdge = col <= 2 || col >= ChunkData.Width - 3;
                if (!insidePassage && isEdge && Hash01(col, absoluteRow, 17) < 0.5f)
                {
                    rowBuffer[col] = (byte)CellType.Bedrock;
                    continue;
                }

                var emptyBias = GetOpenBias(previousRow, col, insidePassage);
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

                if (ShouldPlaceOre((CellType)rowBuffer[col], absoluteRow, col, insidePassage))
                {
                    rowBuffer[col] = (byte)CellType.Ore;
                }
            }
        }

        private void EnsurePassage(Span<byte> rowBuffer, int absoluteRow)
        {
            CarveGuaranteedPassage(rowBuffer, absoluteRow, GetPassageStart(absoluteRow), GetPassageEnd(absoluteRow));
        }

        private static float GetOpenBias(Span<byte> previousRow, int col, bool insidePassage)
        {
            var bias = insidePassage ? 0.08f : 0f;
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

            return Mathf.Clamp(bias, 0f, insidePassage ? 0.28f : 0.2f);
        }

        private Distribution GetDistribution(int absoluteRow)
        {
            return absoluteRow switch
            {
                < 200 => new Distribution(0.18f, 0.64f, 0.15f, 0.01f, 0.02f),
                < 500 => new Distribution(0.14f, 0.52f, 0.24f, 0.06f, 0.04f),
                < 1000 => new Distribution(0.12f, 0.36f, 0.30f, 0.14f, 0.08f),
                < 3000 => new Distribution(0.10f, 0.22f, 0.36f, 0.22f, 0.10f),
                _ => new Distribution(0.08f, 0.12f, 0.35f, 0.30f, 0.15f)
            };
        }

        private void CarveGuaranteedPassage(Span<byte> rowBuffer, int absoluteRow, int passageStart, int passageEnd)
        {
            for (var col = passageStart; col <= passageEnd; col++)
            {
                var type = (CellType)rowBuffer[col];
                if (type == CellType.Bedrock || absoluteRow < 180 && type == CellType.HardRock)
                {
                    rowBuffer[col] = ChoosePassageCellType(absoluteRow, col);
                    continue;
                }

                if (absoluteRow < 160 && type == CellType.Stone && Hash01(col, absoluteRow, 1201) < 0.35f)
                {
                    rowBuffer[col] = (byte)CellType.Dirt;
                }
            }
        }

        private bool ShouldPlaceOre(CellType currentType, int absoluteRow, int col, bool insidePassage)
        {
            if (currentType is not (CellType.Dirt or CellType.Stone or CellType.HardRock))
            {
                return false;
            }

            var baseChance = absoluteRow switch
            {
                < 200 => 0.016f,
                < 500 => 0.028f,
                < 1000 => 0.044f,
                < 3000 => 0.060f,
                _ => 0.072f
            };

            if (insidePassage)
            {
                baseChance *= 0.55f;
            }

            return Hash01(col, absoluteRow, 1471) < baseChance;
        }

        private byte ChoosePassageCellType(int absoluteRow, int col)
        {
            var roll = Hash01(col, absoluteRow, 877);
            if (absoluteRow < 200)
            {
                if (roll < 0.24f)
                {
                    return (byte)CellType.Empty;
                }

                if (roll < 0.88f)
                {
                    return (byte)CellType.Dirt;
                }

                return (byte)CellType.Stone;
            }

            if (absoluteRow < 600)
            {
                if (roll < 0.18f)
                {
                    return (byte)CellType.Empty;
                }

                if (roll < 0.70f)
                {
                    return (byte)CellType.Dirt;
                }

                if (roll < 0.94f)
                {
                    return (byte)CellType.Stone;
                }

                return (byte)CellType.HardRock;
            }

            if (roll < 0.14f)
            {
                return (byte)CellType.Empty;
            }

            if (roll < 0.48f)
            {
                return (byte)CellType.Dirt;
            }

            if (roll < 0.82f)
            {
                return (byte)CellType.Stone;
            }

            return (byte)CellType.HardRock;
        }

        private int GetPassageStart(int absoluteRow)
        {
            var width = GetPassageWidth(absoluteRow);
            var maxStart = ChunkData.Width - width - 4;
            return Mathf.Clamp(GetPassageCenter(absoluteRow) - width / 2, 4, maxStart);
        }

        private int GetPassageEnd(int absoluteRow)
        {
            return GetPassageStart(absoluteRow) + GetPassageWidth(absoluteRow) - 1;
        }

        private int GetPassageWidth(int absoluteRow)
        {
            return absoluteRow switch
            {
                < 220 => 13,
                < 800 => 11,
                _ => 9
            };
        }

        private int GetPassageCenter(int absoluteRow)
        {
            if (absoluteRow < 96)
            {
                return InitialPassageCenter;
            }

            var segment = absoluteRow / PassageSegmentLength;
            var current = GetSegmentPassageCenter(segment);
            var next = GetSegmentPassageCenter(segment + 1);
            var blend = (absoluteRow % PassageSegmentLength) / (float)PassageSegmentLength;
            return Mathf.RoundToInt(Mathf.Lerp(current, next, blend));
        }

        private int GetSegmentPassageCenter(int segment)
        {
            if (_passageCenterCache.TryGetValue(segment, out var center))
            {
                return center;
            }

            if (segment <= 0)
            {
                _passageCenterCache[segment] = InitialPassageCenter;
                return InitialPassageCenter;
            }

            var previous = GetSegmentPassageCenter(segment - 1);
            var drift = segment < 8 ? 0 : (Hash(segment, 911) % 3) - 1;
            center = Mathf.Clamp(previous + drift, MinPassageCenter, MaxPassageCenter);
            _passageCenterCache[segment] = center;
            return center;
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

        private readonly record struct Distribution(float Empty, float Dirt, float Stone, float HardRock, float Bedrock);
    }
}