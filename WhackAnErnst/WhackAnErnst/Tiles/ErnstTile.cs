using System.Diagnostics;
using SixLabors.ImageSharp;

namespace WhackAnErnst.Tiles
{
    public class ErnstTile : ITile
    {
        private const int MIN_DURATION = 500;
        private const int MAX_DURATION = 1500;

        private int _duration;
        private readonly int _points = 1500;

        private readonly Random _random = new();
        private readonly Stopwatch _stopwatch = new();

        private Image _bitmap;
        private readonly Image _bErnst = Image.Load("images/ernst.png");
        private readonly Image _bErnstHammer = Image.Load("images/ernst-hammer.png");
        private readonly Image _bErnstHit = Image.Load("images/ernst-hit.png");

        public ErnstTile()
        {
            _bitmap = _bErnst;
        }

        public long Duration => _duration;
        public int Points => _points - (int)_stopwatch.ElapsedMilliseconds;
        public Image Bitmap => _bitmap;

        public async Task ShowTile(Action<ITile> callback)
        {
            _duration = _random.Next(MIN_DURATION, MAX_DURATION);
            _stopwatch.Restart();
            callback(this);
            await Task.Delay(_duration);
        }

        public async Task TilePressed(Action<ITile> callback)
        {
            _stopwatch.Stop();

            _bitmap = _bErnstHammer;
            callback(this);

            await Task.Delay(75);
            _bitmap = _bErnstHit;
            callback(this);

            await Task.Delay(150);
            callback(this);
        }

        public async IAsyncEnumerable<ITile> DoTilePressedAsync()
        {
            _bitmap = _bErnstHammer;
            yield return this;

            await Task.Delay(75);
            _bitmap = _bErnstHit;
            yield return this;

            await Task.Delay(150);
            yield return this;
        }
    }
}
