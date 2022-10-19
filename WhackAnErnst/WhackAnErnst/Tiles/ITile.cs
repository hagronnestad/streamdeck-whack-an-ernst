using SixLabors.ImageSharp;

namespace WhackAnErnst.Tiles
{
    public interface ITile
    {
        public long Duration { get; }
        public int Points { get; }
        public Image Bitmap { get; }

        Task ShowTile(Action<ITile> callback);
        Task TilePressed(Action<ITile> callback);
    }
}
