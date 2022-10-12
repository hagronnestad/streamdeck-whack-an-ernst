using System.Drawing;

namespace WhackAnErnst.Tiles
{
    public class ErnstTile : Tile
    {
        private const int MIN_DURATION = 500;
        private const int MAX_DURATION = 1500;

        private readonly long _duration;
        private readonly int _points;
        private readonly Bitmap _bitmap;

        public ErnstTile()
        {
            var r = new Random();

            _duration = r.Next(MIN_DURATION, MAX_DURATION);
            _points = 1500;
            _bitmap = new Bitmap(@"images/ernst.png");
        }

        public long Duration => _duration;
        public int Points => _points;
        public Bitmap Bitmap => _bitmap;
    }
}
