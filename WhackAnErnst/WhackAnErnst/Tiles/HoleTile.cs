using System.Drawing;

namespace WhackAnErnst.Tiles
{
    public class HoleTile : ITile
    {
        private const int MIN_DURATION = 500;
        private const int MAX_DURATION = 1500;

        private int _duration;
        private readonly int _points = -1000;

        private readonly Random _random = new();

        private Bitmap _bitmap;
        private readonly Bitmap _bHole = new("images/hole.png");
        private readonly Bitmap _bHoleHit = new("images/hole-hit.png");

        public HoleTile()
        {
            var r = new Random();

            _duration = r.Next(MIN_DURATION, MAX_DURATION);
            _bitmap = _bHole;
        }

        public long Duration => _duration;
        public int Points => _points;
        public Bitmap Bitmap => _bitmap;

        public async Task ShowTile(Action<ITile> callback)
        {
            _duration = _random.Next(MIN_DURATION, MAX_DURATION);
            callback(this);
            await Task.Delay(_duration);
        }

        public async Task TilePressed(Action<ITile> callback)
        {
            _bitmap = _bHoleHit;
            callback(this);

            await Task.Delay(75);
            _bitmap = _bHole;
            callback(this);
        }
    }
}
