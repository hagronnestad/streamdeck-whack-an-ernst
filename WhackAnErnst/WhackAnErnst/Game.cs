using OpenMacroBoard.SDK;
using StreamDeckSharp;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using WhackAnErnst.Tiles;

namespace WhackAnErnst
{
    public class Game : IDisposable
    {
        private const int NUMBER_OF_ERNST_TILES = 7;
        private const int NUMBER_OF_HOLE_TILES = 3;
        private const int GAME_LENGTH = NUMBER_OF_ERNST_TILES + NUMBER_OF_HOLE_TILES;

        private readonly IStreamDeckBoard _streamDeck;

        private readonly Random _random = new();

        private readonly Bitmap _bPlayField;
        private readonly Bitmap _bErnst = new("images/ernst.png");
        private readonly Bitmap _bErnstHammer = new("images/ernst-hammer.png");
        private readonly Bitmap _bErnstHit = new("images/ernst-hit.png");
        private readonly Bitmap _bHole = new("images/hole.png");
        private readonly Bitmap _bHammer = new("images/hammer.png");

        private readonly List<ITile> _gameTiles = new();
        private readonly Dictionary<int, ITile> _currentGameTiles = new();

        private GameState _gameState = GameState.Idle;
        private int _gameProgress = 0;
        private int _gameScore = 0;


        public Game()
        {
            _streamDeck = StreamDeck.OpenDevice();

            _bPlayField = new Bitmap(new Bitmap("images/playfield.png"),
                new Size(_streamDeck.Keys.Area.Width, _streamDeck.Keys.Area.Height));
        }

        public async Task StartAsync()
        {
            _streamDeck.SetBrightness(100);
            _streamDeck.ClearKeys();

            _streamDeck.KeyStateChanged += StreamDeck_KeyStateChangedAsync;

            while (true) await GameLoop();
        }

        /// <summary>
        /// Stream Deck key press handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void StreamDeck_KeyStateChangedAsync(object? sender, KeyEventArgs e)
        {
            if (!e.IsDown) return;

            var pointsScored = 0;

            switch (_gameState)
            {
                case GameState.Idle:
                    _gameState = GameState.Starting;
                    break;

                case GameState.Active:
                    // Player pressed an empty key, punish!
                    if (!_currentGameTiles.ContainsKey(e.Key))
                    {
                        pointsScored = -1000;
                        break;
                    };

                    // Get Tile and perform Tile actions
                    var tile = _currentGameTiles[e.Key];
                    await tile.TilePressed((r) =>
                    {
                        DrawBitmap(e.Key, SuperimposeBitmapOnPlayField(e.Key, r.Bitmap));
                    });
                    pointsScored = tile.Points;

                    // Remove tile
                    _currentGameTiles.Remove(e.Key);
                    DrawBitmap(e.Key, SuperimposeBitmapOnPlayField(e.Key));
                    break;

                case GameState.GameOver:
                    _gameState = GameState.Idle;
                    break;

                default:
                    break;
            }

            // Update score
            _gameScore += pointsScored;
            Console.WriteLine($"Scored {pointsScored} points!");
        }

        /// <summary>
        /// Get a random Stream Deck key
        /// </summary>
        /// <returns>The zero based index of the key</returns>
        private int GetRandomKeyIndex()
        {
            return _random.Next(0, _streamDeck.Keys.Count);
        }

        /// <summary>
        /// Get a random game tile from the list of available game tiles
        /// </summary>
        /// <returns>A random game ITile</returns>
        private ITile GetRandomGameTile()
        {
            var i = _random.Next(0, _gameTiles.Count);
            var item = _gameTiles[i];
            _gameTiles.Remove(item);
            return item;
        }

        /// <summary>
        /// The main game loop
        /// </summary>
        /// <returns></returns>
        private async Task GameLoop()
        {
            switch (_gameState)
            {
                case GameState.Idle:
                    _gameScore = 0;
                    _gameProgress = 0;
                    await ShowIdleScreen();
                    break;

                case GameState.Starting:
                    await ShowGameStartingScreen();
                    _gameState = GameState.Active;
                    break;

                case GameState.Active:
                    _streamDeck.DrawFullScreenBitmap(_bPlayField);
                    await Task.Delay(1000);

                    // Play new tiles for the duration of the game
                    while (_gameProgress < GAME_LENGTH)
                    {
                        var keyIndex = GetRandomKeyIndex();
                        var gameTile = GetRandomGameTile();

                        _currentGameTiles[keyIndex] = gameTile;

                        await gameTile.ShowTile((r) => DrawBitmap(keyIndex, SuperimposeBitmapOnPlayField(keyIndex, r.Bitmap)));

                        // Clear key and remove tile
                        DrawBitmap(keyIndex, SuperimposeBitmapOnPlayField(keyIndex));
                        _currentGameTiles.Remove(keyIndex);

                        // Continue game!
                        await Task.Delay(500);
                        _gameProgress++;
                    }

                    _gameState = GameState.GameOver;
                    break;

                case GameState.GameOver:
                    await ShowGameOverScreen();
                    break;
            }
        }

        /// <summary>
        /// Shows the idle screen animation
        /// </summary>
        /// <returns></returns>
        private async Task ShowIdleScreen()
        {
            _streamDeck.DrawFullScreenBitmap(_bPlayField);

            await Task.Delay(500);
            DrawBitmap(6, SuperimposeBitmapOnPlayField(6, _bHammer));

            await Task.Delay(500);
            DrawBitmap(7, SuperimposeBitmapOnPlayField(7, CreateTextTileBitmap("AN", 20, Color.DarkGreen)));

            await Task.Delay(500);
            DrawBitmap(8, SuperimposeBitmapOnPlayField(8, _bHole));

            await Task.Delay(500);
            DrawBitmap(8, SuperimposeBitmapOnPlayField(8, _bErnst));

            await Task.Delay(750);
            DrawBitmap(8, SuperimposeBitmapOnPlayField(8, _bErnstHammer));

            await Task.Delay(150);
            DrawBitmap(8, SuperimposeBitmapOnPlayField(8, _bErnstHit));

            await Task.Delay(500);
            DrawBitmap(14, CreateTextTileBitmap("START", 12, null, Color.Purple));

            var idleScreenReplayCounter = 0;
            while (_gameState == GameState.Idle)
            {
                if (idleScreenReplayCounter == 50)
                {
                    await ShowIdleScreen();
                }

                await Task.Delay(100);
                idleScreenReplayCounter++;
            }
        }

        /// <summary>
        /// Show the "ready, set, go" animation
        /// </summary>
        /// <returns></returns>
        private async Task ShowGameStartingScreen()
        {
            _streamDeck.ClearKeys();

            _gameTiles.Clear();
            _gameTiles.AddRange(Enumerable.Range(0, NUMBER_OF_ERNST_TILES).Select(x => new ErnstTile()));
            _gameTiles.AddRange(Enumerable.Range(0, NUMBER_OF_HOLE_TILES).Select(x => new HoleTile()));

            DrawBitmap(7, CreateTextTileBitmap("READY", 12, null, Color.Red));
            await Task.Delay(1000);
            DrawBitmap(7, CreateTextTileBitmap("SET", 12, Color.Black, Color.Yellow));
            await Task.Delay(1000);
            DrawBitmap(7, CreateTextTileBitmap("GO!", 12, null, Color.Green));
            await Task.Delay(1000);
        }

        /// <summary>
        /// Show the game over screen
        /// </summary>
        /// <returns></returns>
        private async Task ShowGameOverScreen()
        {
            _streamDeck.DrawFullScreenBitmap(_bPlayField);

            await Task.Delay(250);
            DrawBitmap(2, CreateTextTileBitmap("GAME\nOVER", 12, null, Color.Red));

            await Task.Delay(250);
            DrawBitmap(5, CreateTextTileBitmap("YOU", 12));

            await Task.Delay(250);
            DrawBitmap(6, CreateTextTileBitmap("GOT", 12));

            await Task.Delay(500);
            DrawBitmap(7, CreateTextTileBitmap($"{_gameScore}", 16, Color.Orange));

            await Task.Delay(250);
            DrawBitmap(8, CreateTextTileBitmap("POINTS", 12));

            await Task.Delay(250);
            DrawBitmap(9, CreateTextTileBitmap("!!", 12));

            await Task.Delay(1000);
            DrawBitmap(14, CreateTextTileBitmap("AGAIN", 12, null, Color.Blue));

            while (_gameState == GameState.GameOver) await Task.Delay(100);
        }

        /// <summary>
        /// Draws a Tile to a Stream Deck key
        /// </summary>
        /// <param name="key">The key to draw the tile on</param>
        /// <param name="b">The tile bitmap to draw</param>
        private void DrawBitmap(int key, Bitmap b)
        {
            _streamDeck.SetKeyBitmap(key, KeyBitmap.Create.FromBitmap(b));
        }

        /// <summary>
        /// Superimposes a bitmap on the playfield background
        /// </summary>
        /// <param name="keyIndex">The Stream Deck key index</param>
        /// <param name="bitmap">The bitmap to superimpose on the playfield background</param>
        /// <returns></returns>
        private Bitmap SuperimposeBitmapOnPlayField(int keyIndex, Bitmap? bitmap = null)
        {
            var bPlayField = new Bitmap(_bPlayField, new Size(_streamDeck.Keys.Area.Width, _streamDeck.Keys.Area.Height));
            var bSuperimposed = new Bitmap(_streamDeck.Keys.KeyWidth, _streamDeck.Keys.KeyHeight);

            using var gSuperimposed = Graphics.FromImage(bSuperimposed);

            // Create the correct playfield source rectangle to use based on key index
            var src = new Rectangle(
                _streamDeck.Keys[keyIndex].X,
                _streamDeck.Keys[keyIndex].Y,
                _streamDeck.Keys.KeyWidth,
                _streamDeck.Keys.KeyHeight
            );

            var dest = new Rectangle(0, 0, _streamDeck.Keys.KeyWidth, _streamDeck.Keys.KeyHeight);

            gSuperimposed.DrawImage(bPlayField, dest, src, GraphicsUnit.Pixel);

            if (bitmap != null) gSuperimposed.DrawImage(bitmap, 0, 0, _streamDeck.Keys.KeyWidth, _streamDeck.Keys.KeyHeight);

            return bSuperimposed;
        }

        /// <summary>
        /// Creates a bitmap with the specified text
        /// </summary>
        /// <param name="text">The text to draw</param>
        /// <param name="fontSize">The font size of the text</param>
        /// <param name="textColor">The text color</param>
        /// <param name="bgColor">The background color</param>
        /// <returns></returns>
        private Bitmap CreateTextTileBitmap(string text, float fontSize = 16, Color? textColor = null, Color? bgColor = null)
        {
            var b = new Bitmap(_streamDeck.Keys.KeyWidth, _streamDeck.Keys.KeyHeight);
            using var g = Graphics.FromImage(b);

            var brush = new SolidBrush(textColor ?? Color.White);
            var font = new Font("Comic Sans MS", fontSize, FontStyle.Bold);

            g.TextRenderingHint = TextRenderingHint.AntiAlias;
            g.Clear(bgColor ?? Color.Transparent);

            var size = g.MeasureString(text, font);

            g.DrawString(text, font, brush,
                (_streamDeck.Keys.KeyWidth / 2) - size.Width / 2,
                (_streamDeck.Keys.KeyHeight / 2) - size.Height / 2);

            return b;
        }

        public void Dispose()
        {
            _streamDeck.Dispose();
        }
    }
}
