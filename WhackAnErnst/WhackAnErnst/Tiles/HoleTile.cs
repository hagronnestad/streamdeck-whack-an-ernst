using System.Drawing;

namespace WhackAnErnst.Tiles
{
    public class HoleTile : Tile
    {
        private const int MIN_DURATION = 500;
        private const int MAX_DURATION = 1500;

        private readonly long _duration;
        private readonly int _points;
        private readonly Bitmap _bitmap;

        public HoleTile()
        {
            var r = new Random();

            _duration = r.Next(MIN_DURATION, MAX_DURATION);
            _points = -1000;
            _bitmap = new Bitmap(@"images/hole.png");
        }

        public long Duration => _duration;
        public int Points => _points;
        public Bitmap Bitmap => _bitmap;
    }
}
