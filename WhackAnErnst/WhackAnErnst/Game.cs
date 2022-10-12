using OpenMacroBoard.SDK;
using StreamDeckSharp;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using WhackAnErnst.Tiles;

namespace WhackAnErnst
{
    public class Game : IDisposable
    {
        private readonly IStreamDeckBoard _streamDeck;
        private readonly Random _random = new();
        private readonly Stopwatch _stopwatch = new();

        private readonly Bitmap _bErnst = new("images/ernst.png");
        private readonly Bitmap _bErnstHammer = new("images/ernst-hammer.png");
        private readonly Bitmap _bErnstHit = new("images/ernst-hit.png");
        private readonly Bitmap _bHole = new("images/hole.png");
        private readonly Bitmap _bHoleHit = new("images/hole-hit.png");
        private readonly Bitmap _bHammer = new("images/hammer.png");

        private GameState _gameState = GameState.Ready;

        private const int _ernstTiles = 7;
        private const int _holeTiles = 3;
        private const int _gameLength = _ernstTiles + _holeTiles;
        private readonly List<Tile> _tileList = new();

        private int _gameProgress = 0;
        private long _score = 0;

        private readonly Dictionary<int, Tile> _keyTiles = new();

        public Game()
        {
            _streamDeck = StreamDeck.OpenDevice();
        }

        public async Task StartAsync()
        {
            //Open the Stream Deck device
            _streamDeck.SetBrightness(100);
            _streamDeck.ClearKeys();

            _streamDeck.KeyStateChanged += _streamDeck_KeyStateChangedAsync;

            while (true)
            {
                await GameLoop();
            }
        }

        private async void _streamDeck_KeyStateChangedAsync(object? sender, KeyEventArgs e)
        {
            if (!e.IsDown) return;

            long s = 0;

            switch (_gameState)
            {
                case GameState.Ready:
                    _gameState = GameState.Starting;
                    break;

                case GameState.Active:
                    // Player pressed an empty key, punish!
                    if (!_keyTiles.ContainsKey(e.Key))
                    {
                        s = -1000;
                        _score += s;
                        Console.WriteLine($"Punished {s} points for pressing empty key!");
                        return;
                    };

                    // Check which tile was pressed
                    var tile = _keyTiles[e.Key];
                    switch (tile)
                    {
                        case ErnstTile t:
                            _streamDeck.SetKeyBitmap(e.Key, KeyBitmap.Create.FromBitmap(GetKeyBitmapWithPlayField(e.Key, _bErnstHammer)));
                            await Task.Delay(75);
                            _streamDeck.SetKeyBitmap(e.Key, KeyBitmap.Create.FromBitmap(GetKeyBitmapWithPlayField(e.Key, _bErnstHit)));
                            await Task.Delay(150);
                            s = t.Points - _stopwatch.ElapsedMilliseconds;
                            break;

                        case HoleTile t:
                            _streamDeck.SetKeyBitmap(e.Key, KeyBitmap.Create.FromBitmap(GetKeyBitmapWithPlayField(e.Key, _bHoleHit)));
                            await Task.Delay(75);
                            _streamDeck.SetKeyBitmap(e.Key, KeyBitmap.Create.FromBitmap(GetKeyBitmapWithPlayField(e.Key, _bHole)));
                            await Task.Delay(150);
                            s = t.Points;
                            break;
                    }

                    // Remove tile
                    _keyTiles.Remove(e.Key);
                    _streamDeck.SetKeyBitmap(e.Key, KeyBitmap.Create.FromBitmap(GetKeyBitmapWithPlayField(e.Key)));

                    // Update score
                    _score += s;
                    Console.WriteLine($"Scored {s} points!");
                    break;

                case GameState.GameOver:
                    _gameState = GameState.Ready;
                    break;

                default:
                    break;
            }
        }

        private int GetRandomKeyIndex()
        {
            return _random.Next(0, _streamDeck.Keys.Count);
        }

        private Tile GetRandomTile()
        {
            var i = _random.Next(0, _tileList.Count);
            var item = _tileList[i];
            _tileList.Remove(item);
            return item;
        }

        private async Task GameLoop()
        {
            switch (_gameState)
            {
                case GameState.Ready:
                    _streamDeck.ClearKeys();
                    _gameProgress = 0;
                    _score = 0;

                    _streamDeck.DrawFullScreenBitmap(new Bitmap("images/playfield.png"));

                    await Task.Delay(250);
                    SetBitmap(6, GetKeyBitmapWithPlayField(6, _bHammer));

                    await Task.Delay(250);
                    SetBitmap(7, GetKeyBitmapWithPlayField(7, CreateTextTile("AN", 20, Color.Brown)));

                    await Task.Delay(250);
                    SetBitmap(8, GetKeyBitmapWithPlayField(8, _bHole));

                    await Task.Delay(750);
                    SetBitmap(8, GetKeyBitmapWithPlayField(8, _bErnst));

                    await Task.Delay(500);
                    SetBitmap(8, GetKeyBitmapWithPlayField(8, _bErnstHammer));

                    await Task.Delay(100);
                    SetBitmap(8, GetKeyBitmapWithPlayField(8, _bErnstHit));

                    await Task.Delay(500);
                    SetBitmap(14, CreateTextTile("START", 12, null, Color.DarkGreen));

                    while (_gameState == GameState.Ready)
                    {
                        Thread.Sleep(100);
                    }
                    break;

                case GameState.Starting:
                    _streamDeck.ClearKeys();

                    _tileList.Clear();
                    _tileList.AddRange(Enumerable.Range(0, _ernstTiles).Select(x => new ErnstTile()));
                    _tileList.AddRange(Enumerable.Range(0, _holeTiles).Select(x => new HoleTile()));

                    SetBitmap(7, CreateTextTile("READY", 12, null, Color.Red));
                    await Task.Delay(1000);
                    SetBitmap(7, CreateTextTile("SET", 12, Color.Black, Color.Yellow));
                    await Task.Delay(1000);
                    SetBitmap(7, CreateTextTile("GO!", 12, null, Color.Green));
                    await Task.Delay(1000);
                    _streamDeck.ClearKey(7);

                    _gameState = GameState.Active;
                    break;

                case GameState.Active:
                    _streamDeck.DrawFullScreenBitmap(new Bitmap("images/playfield.png"));

                    await Task.Delay(1000);

                    while (_gameProgress < _gameLength)
                    {
                        var k = GetRandomKeyIndex();
                        var t = GetRandomTile();

                        _keyTiles[k] = t;
                        _streamDeck.SetKeyBitmap(k, KeyBitmap.Create.FromBitmap(GetKeyBitmapWithPlayField(k, t.Bitmap)));

                        // Keep track of how long the player uses to press the tile
                        _stopwatch.Restart();

                        Console.WriteLine($"t.Duration: {t.Duration}");

                        // Wait
                        while (_stopwatch.ElapsedMilliseconds < t.Duration)
                        {
                        }

                        // If the tile is still present in _keyTiles, that means that the player
                        // didn't press the tile. Score points if the player did a good thing.
                        if (_keyTiles.ContainsKey(k) && t is HoleTile)
                        {
                            //long s = _tileDuration;
                            //_score += s;
                            //Console.WriteLine($"Scored bonus points for not tapping! Score: {s}");
                        }

                        // Clear key and remove tile
                        _streamDeck.SetKeyBitmap(k, KeyBitmap.Create.FromBitmap(GetKeyBitmapWithPlayField(k)));
                        _keyTiles.Remove(k);

                        // Continue game!
                        await Task.Delay(500);
                        _gameProgress++;
                    }

                    _gameState = GameState.GameOver;
                    break;

                case GameState.GameOver:
                    SetBitmap(2, CreateTextTile("GAME\nOVER", 12, null, Color.Red));

                    await Task.Delay(250);
                    SetBitmap(5, CreateTextTile("YOU", 12));
                    await Task.Delay(250);
                    SetBitmap(6, CreateTextTile("GOT", 12));

                    await Task.Delay(500);
                    SetBitmap(7, CreateTextTile($"{_score}", 16, Color.Orange));

                    await Task.Delay(250);
                    SetBitmap(8, CreateTextTile("POINTS", 12));
                    await Task.Delay(250);
                    SetBitmap(9, CreateTextTile("!!", 12));

                    await Task.Delay(1000);
                    SetBitmap(14, CreateTextTile("AGAIN", 12, null, Color.Blue));

                    while (_gameState == GameState.GameOver)
                    {
                        Thread.Sleep(100);
                    }
                    break;

                default:
                    break;
            }
        }

        private void SetBitmap(int key, Bitmap b)
        {
            _streamDeck.SetKeyBitmap(key, KeyBitmap.Create.FromBitmap(b));
        }

        private Bitmap CreateTextTile(string s, float fontSize = 16, Color? color = null, Color? bgColor = null)
        {
            var b = new Bitmap(_streamDeck.Keys.KeyWidth, _streamDeck.Keys.KeyHeight);
            using var g = Graphics.FromImage(b);

            var brush = new SolidBrush(color ?? Color.White);
            var font = new Font("Comic Sans MS", fontSize, FontStyle.Bold);

            g.TextRenderingHint = TextRenderingHint.AntiAlias;
            g.Clear(bgColor ?? Color.Transparent);

            var size = g.MeasureString(s, font);

            g.DrawString(s, font, brush,
                (_streamDeck.Keys.KeyWidth / 2) - size.Width / 2,
                (_streamDeck.Keys.KeyHeight / 2) - size.Height / 2);

            return b;
        }

        private Bitmap GetKeyBitmapWithPlayField(int key, Bitmap? keyBitmap = null)
        {
            var pfb = new Bitmap("images/playfield.png");
            var b = new Bitmap(_streamDeck.Keys.KeyWidth, _streamDeck.Keys.KeyHeight);

            using var g = Graphics.FromImage(b);
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAlias;

            var col = key % _streamDeck.Keys.KeyCountX;
            var row = (key * _streamDeck.Keys.KeyCountY) / _streamDeck.Keys.Count;

            var src = new Rectangle(
                x: pfb.Width / _streamDeck.Keys.KeyCountX * col,
                y: pfb.Height / _streamDeck.Keys.KeyCountY * row,
                width: pfb.Width / _streamDeck.Keys.KeyCountX,
                height: pfb.Height / _streamDeck.Keys.KeyCountY
            );

            var dest = new Rectangle(0, 0, _streamDeck.Keys.KeyWidth, _streamDeck.Keys.KeyHeight);

            g.DrawImage(pfb, dest, src, GraphicsUnit.Pixel);

            if (keyBitmap != null) g.DrawImage(keyBitmap, 0, 0, _streamDeck.Keys.KeyWidth, _streamDeck.Keys.KeyHeight);

            return b;
        }

        public void Dispose()
        {
            _streamDeck.Dispose();
        }
    }
}
