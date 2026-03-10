using Godot;

namespace CursedBlood.Core
{
    public sealed class ChunkData
    {
        public const int Width = 32;
        public const int Height = 24;

        public ChunkData(int chunkX, int chunkY)
        {
            ChunkCoordinates = new Vector2I(chunkX, chunkY);
            Cells = new byte[Width * Height];
        }

        public byte[] Cells { get; }

        public Vector2I ChunkCoordinates { get; }

        public int StartCol => ChunkCoordinates.X * Width;

        public int StartRow => ChunkCoordinates.Y * Height;

        public byte GetCell(int localCol, int localRow)
        {
            return Cells[ToIndex(localCol, localRow)];
        }

        public void SetCell(int localCol, int localRow, byte value)
        {
            Cells[ToIndex(localCol, localRow)] = value;
        }

        private static int ToIndex(int col, int row)
        {
            return row * Width + col;
        }
    }
}