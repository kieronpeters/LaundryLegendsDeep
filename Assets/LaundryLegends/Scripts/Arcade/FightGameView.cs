using System.Collections.Generic;
using UnityEngine;

namespace LaundryLegends.Arcade
{
    /// <summary>
    /// Draws the cabinet session into a 512x384 texture: the PLAY/QUIT menu, the
    /// fight with health bars and names, and the result banner. Text uses a small
    /// built-in 5x7 font so no font asset is needed.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FightGameView : MonoBehaviour
    {
        private const int Width = 512;
        private const int Height = 384;
        private const int FloorY = 300;

        private static readonly Dictionary<char, byte[]> Glyphs = BuildGlyphs();

        [SerializeField] private Renderer _screen;
        [SerializeField] private float _refreshSeconds = 1f / 20f;

        private Texture2D _texture;
        private Color32[] _pixels;
        private float _nextRefresh;
        private FightSession _session;
        private Color32 _playerColor = new Color32(96, 200, 255, 255);
        private Color32 _opponentColor = new Color32(255, 120, 96, 255);
        private int _lastPlayerHealth = -1;
        private int _lastOpponentHealth = -1;
        private float _sparkRemaining;
        private float _shakeRemaining;
        private int _sparkX = Width / 2;
        private int _sparkY = FloorY - 70;
        private int _shakeX;
        private int _shakeY;

        public Renderer Screen => _screen;

        public Texture2D Texture => _texture;

        /// <summary>Draw the session fullscreen instead of only on the cabinet screen.</summary>
        public bool Fullscreen { get; set; }

        /// <summary>Per-machine colours so themed cabinets read apart at a glance.</summary>
        public void SetTheme(Color player, Color opponent)
        {
            _playerColor = player;
            _opponentColor = opponent;
        }

        private void OnGUI()
        {
            if (!Fullscreen || _session == null || _texture == null)
            {
                return;
            }

            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(0f, 0f, UnityEngine.Screen.width, UnityEngine.Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float sourceAspect = (float)Width / Height;
            float height = UnityEngine.Screen.height * 0.9f;
            float width = height * sourceAspect;
            if (width > UnityEngine.Screen.width * 0.9f)
            {
                width = UnityEngine.Screen.width * 0.9f;
                height = width / sourceAspect;
            }

            var rect = new Rect((UnityEngine.Screen.width - width) * 0.5f, (UnityEngine.Screen.height - height) * 0.5f, width, height);
            GUI.DrawTexture(rect, _texture, ScaleMode.ScaleToFit, false);

            var hint = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16
            };
            GUI.Label(
                new Rect(0f, UnityEngine.Screen.height - 30f, UnityEngine.Screen.width, 24f),
                "A / D move    W punch    S kick    Space block    Esc leave",
                hint);
        }

        public void Configure(Renderer screen)
        {
            _screen = screen;
            EnsureTexture();
            BindTexture();
        }

        public void Show(FightSession session)
        {
            _session = session;
            _lastPlayerHealth = -1;
            _lastOpponentHealth = -1;
            _sparkRemaining = 0f;
            _shakeRemaining = 0f;
            EnsureTexture();
            Redraw();
        }

        /// <summary>Stops drawing and blanks the screen.</summary>
        public void Hide()
        {
            _session = null;
            _nextRefresh = 0f;
            if (_texture == null)
            {
                return;
            }

            Fill(new Color32(8, 10, 20, 255));
            Apply();
        }

        private void Awake()
        {
            EnsureTexture();
        }

        private void Update()
        {
            float delta = Time.deltaTime;
            if (_sparkRemaining > 0f)
            {
                _sparkRemaining = Mathf.Max(0f, _sparkRemaining - delta);
            }

            if (_shakeRemaining > 0f)
            {
                _shakeRemaining = Mathf.Max(0f, _shakeRemaining - delta);
            }

            if (_session == null || Time.time < _nextRefresh)
            {
                return;
            }

            _nextRefresh = Time.time + _refreshSeconds;
            Redraw();
        }

        /// <summary>Renders one frame. Public so validation can capture a still.</summary>
        public void Redraw()
        {
            if (_session == null || _texture == null)
            {
                return;
            }

            switch (_session.Screen)
            {
                case FightScreen.Menu:
                    DrawMenu();
                    break;
                case FightScreen.Fight:
                    DrawFight();
                    break;
                case FightScreen.Result:
                    DrawFight();
                    DrawResult();
                    break;
            }

            Apply();
        }

        private void DrawMenu()
        {
            Fill(new Color32(10, 14, 28, 255));
            Block(0, 96, Width, 3, new Color32(230, 200, 90, 255));
            Block(0, Height - 150, Width, 3, new Color32(230, 200, 90, 255));

            DrawCentred("ARCADE", 120, 3, new Color32(235, 230, 200, 255));

            DrawOption("PLAY", 196, FightSession.PlayOption, new Color32(120, 220, 255, 255));
            DrawOption("QUIT", 250, FightSession.QuitOption, new Color32(255, 150, 120, 255));

            DrawCentred("W/S  E", 320, 1, new Color32(150, 160, 190, 255));
        }

        private void DrawOption(string label, int y, int option, Color32 colour)
        {
            bool selected = _session.Selection == option;
            int width = label.Length * 6 * 3 - 3;
            int x = (Width - width) / 2;

            if (selected)
            {
                Block(x - 14, y - 10, width + 28, 38, new Color32(40, 60, 96, 255));
                Block(x - 14, y - 10, 6, 38, colour);
            }

            DrawText(label, x, y, 3, selected ? colour : new Color32(150, 165, 200, 255));
        }

        private void DrawFight()
        {
            Fill(new Color32(12, 16, 32, 255));

            FightGame game = _session.Game;
            if (game == null)
            {
                return;
            }

            // Parallax skyline: two layers slide at different rates with the player.
            int scroll = Mathf.RoundToInt(game.PlayerX * 60f);
            DrawSkyline(scroll, new Color32(24, 30, 58, 255), FloorY - 130, 54, 1.0f);
            DrawSkyline(scroll / 2, new Color32(34, 42, 74, 255), FloorY - 96, 72, 0.6f);

            Block(0, FloorY, Width, 6, new Color32(46, 54, 82, 255));
            Block(0, FloorY + 6, Width, Height - FloorY - 6, new Color32(20, 26, 44, 255));

            // Hit feedback: sparks and a short screen shake when health drops.
            if (_lastPlayerHealth >= 0 && game.PlayerHealth < _lastPlayerHealth)
            {
                RegisterHit((int)(game.OpponentX * Width) - 12, FloorY - 70);
            }

            if (_lastOpponentHealth >= 0 && game.OpponentHealth < _lastOpponentHealth)
            {
                RegisterHit((int)(game.PlayerX * Width) + 12, FloorY - 70);
            }

            _lastPlayerHealth = game.PlayerHealth;
            _lastOpponentHealth = game.OpponentHealth;

            if (_shakeRemaining > 0f)
            {
                _shakeX = Mathf.RoundToInt(Mathf.Sin(Time.time * 90f) * 3f);
                _shakeY = Mathf.RoundToInt(Mathf.Cos(Time.time * 77f) * 2f);
            }
            else
            {
                _shakeX = 0;
                _shakeY = 0;
            }

            DrawHealth(24, 56, 180, game.PlayerHealth);
            DrawHealth(Width - 24 - 180, 56, 180, game.OpponentHealth);
            DrawPortrait(18, 12, _playerColor);
            DrawPortrait(Width - 50, 12, _opponentColor);
            DrawText(_session.PlayerName, 58, 18, 2, Lighten(_playerColor));
            DrawText(_session.OpponentName, Width - 58 - (_session.OpponentName.Length * 12), 18, 2, Lighten(_opponentColor));

            int timerWidth = 120;
            int timerX = (Width - timerWidth) / 2;
            Block(timerX, 30, timerWidth, 8, new Color32(10, 12, 22, 255));
            Block(timerX, 30, Mathf.RoundToInt(timerWidth * Mathf.Clamp01(game.TimeRemaining / FightGame.RoundSeconds)), 8,
                new Color32(230, 226, 180, 255));

            DrawFighter((int)(game.PlayerX * Width), _playerColor, true,
                game.PlayerAttacking, game.PlayerGuarding);
            DrawFighter((int)(game.OpponentX * Width), _opponentColor, false,
                game.OpponentAttacking, game.OpponentGuarding);

            if (_sparkRemaining > 0f)
            {
                DrawSpark(_sparkX, _sparkY);
            }

            // Scene shake only affects blocks; reset so HUD/menus stay still.
            _shakeX = 0;
            _shakeY = 0;
        }

        private void RegisterHit(int x, int y)
        {
            _sparkX = x;
            _sparkY = y;
            _sparkRemaining = 0.18f;
            _shakeRemaining = 0.2f;
        }

        private void DrawSkyline(int scroll, Color32 color, int baseY, int spacing, float heightScale)
        {
            for (int i = -1; i < Width / spacing + 2; i++)
            {
                int x = i * spacing - (scroll % (spacing * 4));
                int height = 18 + ((i * 37 + 11) % 44);
                int width = spacing - 8;
                Block(x, baseY - Mathf.RoundToInt(height * heightScale), width,
                    Mathf.RoundToInt(height * heightScale), color);
                Block(x + 6, baseY - Mathf.RoundToInt(height * heightScale) + 6, 4, 4,
                    new Color32(220, 226, 160, 255));
            }
        }

        private void DrawPortrait(int x, int y, Color32 color)
        {
            Block(x, y, 32, 32, new Color32(12, 14, 26, 255));
            Block(x + 3, y + 3, 26, 26, color);
            Block(x + 10, y + 9, 12, 12, new Color32(230, 214, 190, 255));
        }

        private void DrawSpark(int x, int y)
        {
            var hot = new Color32(255, 246, 180, 255);
            Block(x - 14, y, 10, 3, hot);
            Block(x + 4, y, 10, 3, hot);
            Block(x, y - 14, 3, 10, hot);
            Block(x, y + 4, 3, 10, hot);
            Block(x - 4, y - 4, 8, 8, new Color32(255, 150, 60, 255));
        }

        private static Color32 Lighten(Color32 color)
        {
            return new Color32(
                (byte)Mathf.Min(255, color.r + 60),
                (byte)Mathf.Min(255, color.g + 60),
                (byte)Mathf.Min(255, color.b + 60),
                255);
        }

        private void DrawResult()
        {
            Block(0, 150, Width, 60, new Color32(16, 20, 36, 255));

            string text = _session.Game != null && _session.Game.Outcome == FightOutcome.PlayerWon
                ? "YOU WIN"
                : _session.Game != null && _session.Game.Outcome == FightOutcome.Draw
                    ? "DRAW"
                    : "YOU LOSE";

            Color32 colour = text == "YOU WIN"
                ? new Color32(140, 240, 160, 255)
                : new Color32(255, 160, 140, 255);

            int scale = 3;
            int width = text.Length * 6 * scale - scale;
            DrawText(text, (Width - width) / 2, 164, scale, colour);

            float remaining = _session.ResultRemaining / FightSession.ResultHoldSeconds;
            Block(0, 200, Mathf.RoundToInt(Width * Mathf.Clamp01(remaining)), 4, new Color32(200, 200, 120, 255));
        }

        private void DrawHealth(int x, int y, int width, int health)
        {
            Block(x - 2, y - 2, width + 4, 26, new Color32(8, 10, 20, 255));
            int filled = Mathf.RoundToInt(width * Mathf.Clamp01(health / (float)FightGame.MaxHealth));
            float ratio = health / (float)FightGame.MaxHealth;
            var colour = new Color32((byte)Mathf.Lerp(220, 90, ratio), (byte)Mathf.Lerp(70, 210, ratio), 90, 255);
            Block(x, y, filled, 22, colour);
        }

        private void DrawFighter(int centreX, Color32 body, bool facingRight, bool attacking, bool guarding)
        {
            int reach = attacking ? 26 : 12;
            int armX = facingRight ? centreX + 10 : centreX - 10 - reach;

            Block(centreX - 10, FloorY - 26, 8, 26, body);
            Block(centreX + 2, FloorY - 26, 8, 26, body);
            Block(centreX - 12, FloorY - 78, 24, 52, body);
            Block(centreX - 8, FloorY - 98, 16, 20, new Color32(226, 196, 168, 255));
            Block(armX, FloorY - 66, reach, 7, body);

            if (guarding)
            {
                Block(centreX + (facingRight ? 12 : -20), FloorY - 70, 8, 22, new Color32(200, 220, 255, 255));
            }
        }

        private void DrawCentred(string text, int y, int scale, Color32 colour)
        {
            int width = text.Length * 6 * scale - scale;
            DrawText(text, (Width - width) / 2, y, scale, colour);
        }

        private void DrawText(string text, int x, int y, int scale, Color32 colour)
        {
            int cursor = x;
            foreach (char raw in text.ToUpperInvariant())
            {
                if (!Glyphs.TryGetValue(raw, out byte[] rows))
                {
                    cursor += 6 * scale;
                    continue;
                }

                for (int row = 0; row < 7; row++)
                {
                    for (int col = 0; col < 5; col++)
                    {
                        bool on = (rows[row] & (1 << (4 - col))) != 0;
                        if (on)
                        {
                            Block(cursor + col * scale, y + row * scale, scale, scale, colour);
                        }
                    }
                }

                cursor += 6 * scale;
            }
        }

        private static Dictionary<char, byte[]> BuildGlyphs()
        {
            var glyphs = new Dictionary<char, byte[]>();

            void Add(char c, params byte[] rows) => glyphs[c] = rows;

            Add('A', 0b01110, 0b10001, 0b10001, 0b11111, 0b10001, 0b10001, 0b10001);
            Add('B', 0b11110, 0b10001, 0b10001, 0b11110, 0b10001, 0b10001, 0b11110);
            Add('C', 0b01110, 0b10001, 0b10000, 0b10000, 0b10000, 0b10001, 0b01110);
            Add('D', 0b11110, 0b10001, 0b10001, 0b10001, 0b10001, 0b10001, 0b11110);
            Add('E', 0b11111, 0b10000, 0b10000, 0b11110, 0b10000, 0b10000, 0b11111);
            Add('F', 0b11111, 0b10000, 0b10000, 0b11110, 0b10000, 0b10000, 0b10000);
            Add('G', 0b01110, 0b10001, 0b10000, 0b10111, 0b10001, 0b10001, 0b01111);
            Add('H', 0b10001, 0b10001, 0b10001, 0b11111, 0b10001, 0b10001, 0b10001);
            Add('I', 0b11111, 0b00100, 0b00100, 0b00100, 0b00100, 0b00100, 0b11111);
            Add('K', 0b10001, 0b10010, 0b10100, 0b11000, 0b10100, 0b10010, 0b10001);
            Add('L', 0b10000, 0b10000, 0b10000, 0b10000, 0b10000, 0b10000, 0b11111);
            Add('M', 0b10001, 0b11011, 0b10101, 0b10101, 0b10001, 0b10001, 0b10001);
            Add('N', 0b10001, 0b11001, 0b10101, 0b10011, 0b10001, 0b10001, 0b10001);
            Add('O', 0b01110, 0b10001, 0b10001, 0b10001, 0b10001, 0b10001, 0b01110);
            Add('P', 0b11110, 0b10001, 0b10001, 0b11110, 0b10000, 0b10000, 0b10000);
            Add('Q', 0b01110, 0b10001, 0b10001, 0b10001, 0b10101, 0b10010, 0b01101);
            Add('R', 0b11110, 0b10001, 0b10001, 0b11110, 0b10100, 0b10010, 0b10001);
            Add('S', 0b01111, 0b10000, 0b10000, 0b01110, 0b00001, 0b00001, 0b11110);
            Add('T', 0b11111, 0b00100, 0b00100, 0b00100, 0b00100, 0b00100, 0b00100);
            Add('U', 0b10001, 0b10001, 0b10001, 0b10001, 0b10001, 0b10001, 0b01110);
            Add('V', 0b10001, 0b10001, 0b10001, 0b10001, 0b10001, 0b01010, 0b00100);
            Add('W', 0b10001, 0b10001, 0b10001, 0b10101, 0b10101, 0b11011, 0b10001);
            Add('X', 0b10001, 0b10001, 0b01010, 0b00100, 0b01010, 0b10001, 0b10001);
            Add('Y', 0b10001, 0b10001, 0b01010, 0b00100, 0b00100, 0b00100, 0b00100);
            Add('Z', 0b11111, 0b00001, 0b00010, 0b00100, 0b01000, 0b10000, 0b11111);
            Add('J', 0b00111, 0b00010, 0b00010, 0b00010, 0b00010, 0b10010, 0b01100);
            Add('0', 0b01110, 0b10001, 0b10011, 0b10101, 0b11001, 0b10001, 0b01110);
            Add('1', 0b00100, 0b01100, 0b00100, 0b00100, 0b00100, 0b00100, 0b01110);
            Add('2', 0b01110, 0b10001, 0b00001, 0b00010, 0b00100, 0b01000, 0b11111);
            Add('3', 0b11110, 0b00001, 0b00001, 0b01110, 0b00001, 0b00001, 0b11110);
            Add('4', 0b00010, 0b00110, 0b01010, 0b10010, 0b11111, 0b00010, 0b00010);
            Add('5', 0b11111, 0b10000, 0b11110, 0b00001, 0b00001, 0b10001, 0b01110);
            Add('6', 0b00110, 0b01000, 0b10000, 0b11110, 0b10001, 0b10001, 0b01110);
            Add('7', 0b11111, 0b00001, 0b00010, 0b00100, 0b01000, 0b01000, 0b01000);
            Add('8', 0b01110, 0b10001, 0b10001, 0b01110, 0b10001, 0b10001, 0b01110);
            Add('9', 0b01110, 0b10001, 0b10001, 0b01111, 0b00001, 0b00010, 0b01100);
            Add(':', 0, 0b00100, 0b00100, 0, 0b00100, 0b00100, 0);
            Add('-', 0, 0, 0, 0b11111, 0, 0, 0);
            Add('.', 0, 0, 0, 0, 0, 0b00110, 0b00110);
            Add('!', 0b00100, 0b00100, 0b00100, 0b00100, 0b00100, 0, 0b00100);
            Add('/', 0b00001, 0b00010, 0b00010, 0b00100, 0b01000, 0b01000, 0b10000);
            Add(' ', 0, 0, 0, 0, 0, 0, 0);
            return glyphs;
        }

        private void EnsureTexture()
        {
            if (_texture != null)
            {
                return;
            }

            _texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            _pixels = new Color32[Width * Height];
            Fill(new Color32(12, 16, 32, 255));
            BindTexture();
        }

        private void BindTexture()
        {
            if (_screen == null || _texture == null)
            {
                return;
            }

            Material material = Application.isPlaying ? _screen.material : _screen.sharedMaterial;
            if (material != null)
            {
                material.mainTexture = _texture;
            }
        }

        private void Apply()
        {
            _texture.SetPixels32(_pixels);
            _texture.Apply(false);
        }

        private void Fill(Color32 color)
        {
            for (int i = 0; i < _pixels.Length; i++)
            {
                _pixels[i] = color;
            }
        }

        private void Block(int x, int y, int width, int height, Color32 color)
        {
            x += _shakeX;
            y += _shakeY;
            for (int row = 0; row < height; row++)
            {
                // The cabinet screen quad presents the texture rotated, so the image is
                // drawn pre-rotated 180 degrees to read correctly from the player's view.
                int py = Height - 1 - (y + row);
                if (py < 0 || py >= Height)
                {
                    continue;
                }

                int start = py * Width;
                for (int col = 0; col < width; col++)
                {
                    int px = x + col;
                    if (px < 0 || px >= Width)
                    {
                        continue;
                    }

                    _pixels[start + px] = color;
                }
            }
        }
    }
}
