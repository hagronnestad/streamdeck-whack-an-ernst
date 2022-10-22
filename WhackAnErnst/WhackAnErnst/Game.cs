using OpenMacroBoard.SDK;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using StreamDeckSharp;
using WhackAnErnst.Tiles;

namespace WhackAnErnst
{
    public class Game : IDisposable
    {
        private const int NUMBER_OF_ERNST_TILES = 7;
        private const int NUMBER_OF_HOLE_TILES = 3;
        private const int GAME_LENGTH = NUMBER_OF_ERNST_TILES + NUMBER_OF_HOLE_TILES;

        private IMacroBoard _streamDeck;

        private readonly Random _random = new();

        private Image? _bPlayField;
        private readonly Image _bErnst = Image.Load("images/ernst.png");
        private readonly Image _bErnstHammer = Image.Load("images/ernst-hammer.png");
        private readonly Image _bErnstHit = Image.Load("images/ernst-hit.png");
        private readonly Image _bHole = Image.Load("images/hole.png");
        private readonly Image _bHammer = Image.Load("images/hammer.png");

        private readonly List<ITile> _gameTiles = new();
        private readonly Dictionary<int, ITile> _currentGameTiles = new();

        private GameState _gameState = GameState.Idle;
        private int _gameProgress = 0;
        private int _gameScore = 0;

        private FontCollection _fontCollection = new();
        private FontFamily _fontFamily;

        public Game()
        {
            _fontFamily = _fontCollection.Add("fonts/comic.ttf");
        }

        public async Task StartAsync()
        {
            try
            {
                _streamDeck = StreamDeck.OpenDevice();
                _streamDeck.SetBrightness(100);
                _streamDeck.ClearKeys();

                _streamDeck.KeyStateChanged += StreamDeck_KeyStateChangedAsync;

                _bPlayField = Image.Load("images/playfield.png");
                _bPlayField.Mutate(x => x.Resize(_streamDeck.Keys.Area.Width, _streamDeck.Keys.Area.Height));

                while (true) await GameLoop();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return;
            }
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
                    SdlAudioWrapper.PlaySound(Sounds.Dat);
                    _gameState = GameState.Starting;
                    Console.WriteLine("Round starting!");
                    break;

                case GameState.Active:
                    // Does the pressed key hold a valid game tile?
                    if (_currentGameTiles.ContainsKey(e.Key))
                    {
                        // Get Tile and perform Tile actions
                        var tile = _currentGameTiles[e.Key];
                        await tile.TilePressed((r) =>
                        {
                            DrawBitmap(e.Key, SuperimposeBitmapOnPlayField(e.Key, r.Bitmap));
                        });
                        pointsScored = tile.Points;
                    }
                    else
                    {
                        // Player pressed an empty key, punish!
                        pointsScored = -1000;
                    };

                    // Update score
                    _gameScore += pointsScored;
                    Console.WriteLine($"Scored {pointsScored} points!");

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
                    Console.WriteLine("Round over!");
                    Console.WriteLine($"Total score: {_gameScore} points!");
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
            SdlAudioWrapper.PlaySound(Sounds.Dunk);
            DrawBitmap(6, SuperimposeBitmapOnPlayField(6, _bHammer));

            await Task.Delay(500);
            SdlAudioWrapper.PlaySound(Sounds.Dat);
            DrawBitmap(7, SuperimposeBitmapOnPlayField(7, CreateTextTileBitmap("AN", 25, Color.DarkGreen)));

            await Task.Delay(500);
            SdlAudioWrapper.PlaySound(Sounds.Bop);
            DrawBitmap(8, SuperimposeBitmapOnPlayField(8, _bHole));

            await Task.Delay(500);
            SdlAudioWrapper.PlaySound(Sounds.Dat);
            DrawBitmap(8, SuperimposeBitmapOnPlayField(8, _bErnst));

            await Task.Delay(750);
            SdlAudioWrapper.PlaySound(Sounds.Dunk);
            DrawBitmap(8, SuperimposeBitmapOnPlayField(8, _bErnstHammer));

            await Task.Delay(150);
            SdlAudioWrapper.PlaySound(Sounds.RandomAu());
            DrawBitmap(8, SuperimposeBitmapOnPlayField(8, _bErnstHit));

            await Task.Delay(500);
            SdlAudioWrapper.PlaySound(Sounds.Dat);
            DrawBitmap(14, CreateTextTileBitmap("START", 18, null, Color.Purple));

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

            SdlAudioWrapper.PlaySound(Sounds.Ready);
            DrawBitmap(7, CreateTextTileBitmap("READY", 18, null, Color.Red));
            await Task.Delay(1000);
            SdlAudioWrapper.PlaySound(Sounds.Set);
            DrawBitmap(7, CreateTextTileBitmap("SET", 18, Color.Black, Color.Yellow));
            await Task.Delay(1000);
            SdlAudioWrapper.PlaySound(Sounds.Go);
            DrawBitmap(7, CreateTextTileBitmap("GO!", 18, null, Color.Green));
            await Task.Delay(1000);
        }

        /// <summary>
        /// Show the game over screen
        /// </summary>
        /// <returns></returns>
        private async Task ShowGameOverScreen()
        {
            _streamDeck.DrawFullScreenBitmap(_bPlayField);

            SdlAudioWrapper.PlaySound(Sounds.GameOver);
            await Task.Delay(250);
            DrawBitmap(2, CreateTextTileBitmap("GAME\nOVER", 18, null, Color.Red));

            await Task.Delay(500);
            SdlAudioWrapper.PlaySound(Sounds.Dat);
            DrawBitmap(5, CreateTextTileBitmap("YOU", 17));

            await Task.Delay(250);
            SdlAudioWrapper.PlaySound(Sounds.Dat);
            DrawBitmap(6, CreateTextTileBitmap("SCORED", 17));

            await Task.Delay(500);
            SdlAudioWrapper.PlaySound(Sounds.Bop);
            DrawBitmap(7, CreateTextTileBitmap($"{_gameScore}", 17, Color.Orange));

            await Task.Delay(250);
            SdlAudioWrapper.PlaySound(Sounds.Dat);
            DrawBitmap(8, CreateTextTileBitmap("POINTS", 17));

            await Task.Delay(250);
            SdlAudioWrapper.PlaySound(Sounds.Dat);
            DrawBitmap(9, CreateTextTileBitmap("!!", 17));

            await Task.Delay(1000);
            SdlAudioWrapper.PlaySound(Sounds.Dat);
            DrawBitmap(14, CreateTextTileBitmap("AGAIN", 18, null, Color.Blue));

            while (_gameState == GameState.GameOver) await Task.Delay(100);
        }

        /// <summary>
        /// Draws a Tile to a Stream Deck key
        /// </summary>
        /// <param name="key">The key to draw the tile on</param>
        /// <param name="b">The tile bitmap to draw</param>
        private void DrawBitmap(int key, Image b)
        {
            _streamDeck.SetKeyBitmap(key, KeyBitmap.Create.FromImageSharpImage(b));
        }

        /// <summary>
        /// Superimposes a bitmap on the playfield background
        /// </summary>
        /// <param name="keyIndex">The Stream Deck key index</param>
        /// <param name="bitmap">The bitmap to superimpose on the playfield background</param>
        /// <returns></returns>
        private Image SuperimposeBitmapOnPlayField(int keyIndex, Image? bitmap = null)
        {
            // Create the correct playfield source rectangle to use based on key index
            var src = new Rectangle(
                _streamDeck.Keys[keyIndex].X,
                _streamDeck.Keys[keyIndex].Y,
                _streamDeck.Keys.KeySize,
                _streamDeck.Keys.KeySize
            );

            var dest = new Rectangle(0, 0, _streamDeck.Keys.KeySize, _streamDeck.Keys.KeySize);

            var s = _bPlayField.Clone(x => x.Crop(src));

            if (bitmap != null) {
                // TODO: It would be better to make sure that all bitmaps are resized on startup...
                bitmap.Mutate(x => x.Resize(_streamDeck.Keys.KeySize, _streamDeck.Keys.KeySize));
                s.Mutate(x => x.DrawImage(bitmap, new Point(0, 0), 1f));
            }

            return s;
        }

        /// <summary>
        /// Creates a bitmap with the specified text
        /// </summary>
        /// <param name="text">The text to draw</param>
        /// <param name="fontSize">The font size of the text</param>
        /// <param name="textColor">The text color</param>
        /// <param name="bgColor">The background color</param>
        /// <returns></returns>
        private Image CreateTextTileBitmap(string text, float fontSize = 16, Color? textColor = null, Color? bgColor = null)
        {
            var i = new Image<Rgba32>(_streamDeck.Keys.KeySize, _streamDeck.Keys.KeySize);

            Font font = _fontFamily.CreateFont(fontSize, FontStyle.Bold);
            //Font font = SystemFonts.CreateFont("Comic Sans MS", fontSize, FontStyle.Bold);
            FontRectangle size = TextMeasurer.Measure(text, new TextOptions(font));

            i.Mutate(x => x.BackgroundColor(bgColor ?? Color.Transparent));
            i.Mutate(x => x.DrawText(text, font, textColor ?? Color.White,
                new PointF(_streamDeck.Keys.KeySize / 2 - size.Width / 2,
                _streamDeck.Keys.KeySize / 2 - size.Height / 2)));

            return i;
        }

        public void Dispose()
        {
            _streamDeck?.Dispose();
        }
    }
}
