namespace CursedBlood.Core
{
    public sealed class ChunkData
    {
        public const int Width = 67;
        public const int Height = 16;

        public ChunkData(int chunkIndex)
        {
            ChunkIndex = chunkIndex;
            Cells = new byte[Width * Height];
        }

        public byte[] Cells { get; }

        public int ChunkIndex { get; }

        public int StartRow => ChunkIndex * Height;

        public byte GetCell(int localCol, int localRow)
        {
            return Cells[ToIndex(localCol, localRow)];
        }

        public void SetCell(int localCol, int localRow, byte value)
        {
            Cells[ToIndex(localCol, localRow)] = value;
        }

        public int ToIndex(int col, int row)
        {
            return row * Width + col;
        }
    }
}