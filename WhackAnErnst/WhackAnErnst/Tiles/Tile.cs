using System.Drawing;

namespace WhackAnErnst.Tiles
{
    public interface Tile
    {
        public long Duration { get; }
        public int Points { get; }
        public Bitmap Bitmap { get; }
    }
}
