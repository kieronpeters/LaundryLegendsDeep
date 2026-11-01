using UnityEngine;

namespace LaundryLegends.Arcade
{
    /// <summary>
    /// Draws the maze game into a texture and shows it on the cabinet screen.
    /// One texture, blocky cells, no sprites or extra assets.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MazeGameView : MonoBehaviour
    {
        private const int Cell = 16;
        private const int Width = 512;
        private const int Height = 384;

        [SerializeField] private Renderer _screen;
        [SerializeField] private float _refreshSeconds = 1f / 12f;

        private Texture2D _texture;
        private Color32[] _pixels;
        private float _nextRefresh;
        private MazeGame _game;

        public Renderer Screen => _screen;

        public Texture2D Texture => _texture;

        public void Configure(Renderer screen)
        {
            _screen = screen;
            EnsureTexture();
            BindTexture();
        }

        public void Show(MazeGame game)
        {
            _game = game;
            EnsureTexture();
            Redraw();
        }

        /// <summary>
        /// Stops drawing and clears the screen. Without this a stopped cabinet keeps
        /// redrawing a frozen maze forever.
        /// </summary>
        public void Hide()
        {
            _game = null;
            _nextRefresh = 0f;

            if (_texture == null)
            {
                return;
            }

            Fill(new Color32(8, 10, 20, 255));
            _texture.SetPixels32(_pixels);
            _texture.Apply(false);
        }

        private void BindTexture()
        {
            if (_screen == null || _texture == null)
            {
                return;
            }

            // At runtime the screen needs its own material instance; in the editor
            // (and headless validation) writing sharedMaterial avoids leaking a
            // material that would otherwise be saved into the scene.
            Material material = Application.isPlaying ? _screen.material : _screen.sharedMaterial;
            if (material != null)
            {
                material.mainTexture = _texture;
            }
        }

        private void Awake()
        {
            EnsureTexture();
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
            Fill(new Color32(8, 10, 20, 255));

            BindTexture();
        }

        private void Update()
        {
            if (_game == null || Time.time < _nextRefresh)
            {
                return;
            }

            _nextRefresh = Time.time + _refreshSeconds;
            Redraw();
        }

        /// <summary>Renders one frame of the game into the texture.</summary>
        public void Redraw()
        {
            if (_game == null || _texture == null)
            {
                return;
            }

            Fill(new Color32(8, 10, 20, 255));

            MazeLayout layout = _game.Layout;
            int offsetX = (Width - layout.Columns * Cell) / 2;
            int offsetY = (Height - layout.Rows * Cell) / 2;

            for (int y = 0; y < layout.Rows; y++)
            {
                for (int x = 0; x < layout.Columns; x++)
                {
                    MazeTile tile = layout.GetTile(x, y);
                    int px = offsetX + x * Cell;
                    int py = offsetY + (layout.Rows - 1 - y) * Cell;

                    if (tile == MazeTile.Wall)
                    {
                        Block(px, py, Cell, new Color32(24, 30, 74, 255));
                    }
                    else if (tile == MazeTile.Dot)
                    {
                        Block(px + 6, py + 6, 4, new Color32(238, 226, 180, 255));
                    }
                    else if (tile == MazeTile.PowerPellet)
                    {
                        Block(px + 4, py + 4, 8, new Color32(255, 240, 160, 255));
                    }
                }
            }

            foreach (Vector2Int ghost in _game.Ghosts)
            {
                Block(offsetX + ghost.x * Cell + 2, offsetY + (layout.Rows - 1 - ghost.y) * Cell + 2,
                    Cell - 4, new Color32(235, 64, 160, 255));
            }

            Block(offsetX + _game.Player.x * Cell + 2, offsetY + (layout.Rows - 1 - _game.Player.y) * Cell + 2,
                Cell - 4, new Color32(255, 208, 64, 255));

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

        private void Block(int x, int y, int size, Color32 color)
        {
            for (int row = 0; row < size; row++)
            {
                int py = y + row;
                if (py < 0 || py >= Height)
                {
                    continue;
                }

                int start = py * Width;
                for (int col = 0; col < size; col++)
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
