using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using FontStashSharp;

namespace TheatreGame
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private Matrix _viewMatrix;
        private Matrix _projectionMatrix;

        private VertexPositionNormalTexture[] _floorVertices;
        private VertexPositionNormalTexture[] _curtainVertices;
        private BasicEffect _effect;
        private Texture2D _floorTexture;
        private Texture2D _curtainTexture;
        private Texture2D _gridTexture;

        private Texture2D _campfireTexture;
        private Texture2D _pawnTexture;
        private Texture2D _bishopTexture;
        private Texture2D _spinnerTexture;
        private Texture2D _lightGradientTexture;

        private Texture2D _particleTexture;
        private BasicEffect _colorEffect;
        private FontStashSharp.FontSystem _fontSystem;
        private FontStashSharp.DynamicSpriteFont _font;
        private int _turn = 1;
        private const string MapName = "TheatreScene";
        private List<Particle> _lightParticles;
        private List<Particle> _dustParticles;
        private List<Particle> _fireParticles;
        private List<Particle> _smokeParticles;
        private Texture2D _grainTexture;
        private Color[] _grainData;
        private Random _random;
        private float _time;

        private Vector3 _cameraTarget;
        private Vector3 _cameraPosition;
        private float _cameraDistance;

        private const float MinZoom = 15f;
        private const float MaxZoom = 50f;
        private const float CameraMoveLimit = 30f;
        private const float CameraMoveSpeed = 10f;

        private Vector2 _campfireScreenPos;
        private float _campfireAlpha = 1f;

        private List<Vector3> _lightPositions;

        private Texture2D _endTurnButtonTexture;
        private Rectangle _endTurnButtonRect;
        private Texture2D _bagTexture;
        private Rectangle _bagRect;
        private Dictionary<Texture2D, Rectangle> _textureBounds = new();
        private MouseState _prevMouseState;

        private const int ToolbarHeight = 80;

        private const float CellSize = 2.5f;

        private class Character
        {
            public Point BoardPos;
            public Vector2 ScreenPos;
            public Queue<Point> Path = new Queue<Point>();
            public float MoveProgress = 0f;
            public Texture2D Texture;
            public bool IsPlayer;
            public float Visibility = 1f;
        }

        private List<Character> _characters = new List<Character>();
        private Point? _hoveredTile;
        private Point? _selectedTile;
        private List<Point> _playerPath;
        private List<Point> _aiPath;
        private Point? _playerPathStart;
        private Point? _aiPathStart;
        private bool _moving;
        private float _spinnerRotation;

        private float _initialCameraDistance;

        private bool _inventoryOpen;
        private bool _menuOpen;
        private bool _settingsOpen;
        private bool _saveOpen;
        private bool _loadOpen;
        private bool _prevEscDown;
        private bool _prevSave;
        private bool _prevLoad;
        private string _saveInput = string.Empty;
        private List<string> _saveFiles = new();
        private int _loadScroll;

        private enum DraggedPanel { None, Menu, Inventory, Save, Load }
        private DraggedPanel _dragging = DraggedPanel.None;
        private Point _dragOffset;
        private Vector2 _menuPos;
        private Vector2 _inventoryPos;
        private Vector2 _savePos;
        private Vector2 _loadPos;
        private Vector2 _settingsPos;
        private bool _fullscreen;

        private struct Particle
        {
            public Vector2 Position;
            public Vector2 Velocity;
            public float Lifetime;
            public float Age;
            public float Scale;
            public Color Color;
        }

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            _graphics.PreferredBackBufferWidth = 1280;
            _graphics.PreferredBackBufferHeight = 720;
        }

        protected override void Initialize()
        {
            base.Initialize();

            // Set up isometric camera
            _cameraTarget = Vector3.Zero;
            _cameraPosition = new Vector3(20, 20, 20);
            _cameraDistance = (_cameraPosition - _cameraTarget).Length();
            _initialCameraDistance = _cameraDistance;
            var up = Vector3.Up;
            _viewMatrix = Matrix.CreateLookAt(_cameraPosition, _cameraTarget, up);
            _projectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(45f),
                GraphicsDevice.Viewport.AspectRatio, 0.1f, 100f);

            // Create floor quad with normals for lighting
            _floorVertices = new VertexPositionNormalTexture[6];
            var floorNormal = Vector3.Up;
            // Tile the floor texture 8x8 across the stage
            _floorVertices[0] = new VertexPositionNormalTexture(new Vector3(-10, 0, -10), floorNormal, new Vector2(0, 0));
            _floorVertices[1] = new VertexPositionNormalTexture(new Vector3(-10, 0, 10), floorNormal, new Vector2(0, 8));
            _floorVertices[2] = new VertexPositionNormalTexture(new Vector3(10, 0, -10), floorNormal, new Vector2(8, 0));
            _floorVertices[3] = new VertexPositionNormalTexture(new Vector3(10, 0, -10), floorNormal, new Vector2(8, 0));
            _floorVertices[4] = new VertexPositionNormalTexture(new Vector3(-10, 0, 10), floorNormal, new Vector2(0, 8));
            _floorVertices[5] = new VertexPositionNormalTexture(new Vector3(10, 0, 10), floorNormal, new Vector2(8, 8));

            // Create curtain quad behind the stage
            _curtainVertices = new VertexPositionNormalTexture[6];
            var curtainNormal = Vector3.Backward;
            // Tile the curtain texture so it spans the stage width
            _curtainVertices[0] = new VertexPositionNormalTexture(new Vector3(-10, 0, -10), curtainNormal, new Vector2(0, 4));
            _curtainVertices[1] = new VertexPositionNormalTexture(new Vector3(10, 0, -10), curtainNormal, new Vector2(8, 4));
            _curtainVertices[2] = new VertexPositionNormalTexture(new Vector3(-10, 10, -10), curtainNormal, new Vector2(0, 0));
            _curtainVertices[3] = new VertexPositionNormalTexture(new Vector3(-10, 10, -10), curtainNormal, new Vector2(0, 0));
            _curtainVertices[4] = new VertexPositionNormalTexture(new Vector3(10, 0, -10), curtainNormal, new Vector2(8, 4));
            _curtainVertices[5] = new VertexPositionNormalTexture(new Vector3(10, 10, -10), curtainNormal, new Vector2(8, 0));

            _random = new Random();
            _lightParticles = new List<Particle>();
            _dustParticles = new List<Particle>();
            _fireParticles = new List<Particle>();
            _smokeParticles = new List<Particle>();
            _lightPositions = new List<Vector3> { Vector3.Zero }; // campfire
            SpawnParticles(_lightParticles, 100, new Color(255, 255, 200, 200));
            SpawnParticles(_dustParticles, 200, new Color(150, 120, 100, 150));

            var screenPoint = GraphicsDevice.Viewport.Project(_lightPositions[0],
                _projectionMatrix, _viewMatrix, Matrix.Identity);
            _campfireScreenPos = new Vector2(screenPoint.X, screenPoint.Y);
            SpawnParticlesAt(_fireParticles, 50, new Color(255, 170, 50, 255),
                _campfireScreenPos, 10f);
            SpawnParticlesAt(_smokeParticles, 40, new Color(80, 80, 80, 180),
                _campfireScreenPos, 15f);

            _characters = new List<Character>();


            var playerPos = new Point(4, 7);
            _characters.Add(new Character
            {
                BoardPos = playerPos,
                ScreenPos = BoardToScreen(playerPos),
                IsPlayer = true
            });

            var aiPos = new Point(4, 0);
            _characters.Add(new Character
            {
                BoardPos = aiPos,
                ScreenPos = BoardToScreen(aiPos),
                IsPlayer = false
            });

            UpdateUILayout();

            _menuPos = new Vector2(_graphics.PreferredBackBufferWidth / 2 - 100,
                _graphics.PreferredBackBufferHeight / 2 - 80);
            _inventoryPos = new Vector2(_graphics.PreferredBackBufferWidth / 2 - 192,
                _graphics.PreferredBackBufferHeight / 2 - 96);
            _savePos = new Vector2(_graphics.PreferredBackBufferWidth / 2 - 150,
                _graphics.PreferredBackBufferHeight / 2 - 60);
            _loadPos = new Vector2(_graphics.PreferredBackBufferWidth / 2 - 150,
                _graphics.PreferredBackBufferHeight / 2 - 120);
            _settingsPos = new Vector2(_graphics.PreferredBackBufferWidth / 2 - 150,
                _graphics.PreferredBackBufferHeight / 2 - 60);

            Window.TextInput += OnTextInput;
        }

        private Texture2D LoadTexture(string fileName)
        {
            string finalPath = Path.Combine("ContentFinal", fileName);
            if (File.Exists(finalPath))
            {
                using var stream = TitleContainer.OpenStream(finalPath);
                return LoadAndScale(stream);
            }
            using var fallback = TitleContainer.OpenStream(Path.Combine("Content", fileName));
            return LoadAndScale(fallback);
        }

        private Texture2D LoadAndScale(Stream stream)
        {
            var texture = Texture2D.FromStream(GraphicsDevice, stream);
            if (texture.Width == 128 && texture.Height == 128)
            {
                _textureBounds[texture] = GetTrimmedBounds(texture);
                return texture;
            }

            // Resize to 128x128 using a temporary render target
            var rt = new RenderTarget2D(GraphicsDevice, 128, 128);
            using var sb = new SpriteBatch(GraphicsDevice);
            GraphicsDevice.SetRenderTarget(rt);
            GraphicsDevice.Clear(Color.Transparent);
            sb.Begin(samplerState: SamplerState.LinearClamp);
            sb.Draw(texture, new Rectangle(0, 0, 128, 128), Color.White);
            sb.End();
            GraphicsDevice.SetRenderTarget(null);
            texture.Dispose();
            _textureBounds[rt] = GetTrimmedBounds(rt);
            return rt;
        }

        private Rectangle GetTrimmedBounds(Texture2D tex)
        {
            Color[] data = new Color[tex.Width * tex.Height];
            tex.GetData(data);
            int minX = tex.Width, minY = tex.Height, maxX = 0, maxY = 0;
            bool found = false;
            for (int y = 0; y < tex.Height; y++)
            {
                for (int x = 0; x < tex.Width; x++)
                {
                    Color c = data[y * tex.Width + x];
                    if (c.A > 0)
                    {
                        found = true;
                        if (x < minX) minX = x;
                        if (y < minY) minY = y;
                        if (x > maxX) maxX = x;
                        if (y > maxY) maxY = y;
                    }
                }
            }
            if (!found)
                return new Rectangle(0, 0, tex.Width, tex.Height);
            return new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            _effect = new BasicEffect(GraphicsDevice)
            {
                TextureEnabled = true,
                VertexColorEnabled = false,
                LightingEnabled = true,
                PreferPerPixelLighting = true
            };
            _effect.EnableDefaultLighting();

            // MonoGame's ContentManager expects pre-built XNB files. For this
            // simple prototype we generate the PNG textures at runtime and
            // load them directly from disk instead of using the content
            // pipeline.
            _floorTexture = LoadTexture("stage_floor.png");
            _curtainTexture = LoadTexture("curtain.png");
            _gridTexture = LoadTexture("grid_overlay.png");

            _campfireTexture = LoadTexture("campfire.png");

            _pawnTexture = LoadTexture("pawn.png");
            _bishopTexture = LoadTexture("bishop.png");
            _lightGradientTexture = LoadTexture("light_gradient.png");
            _endTurnButtonTexture = LoadTexture("end_turn.png");
            _spinnerTexture = LoadTexture("spinner.png");
            _bagTexture = LoadTexture("bag.png");

            for (int i = 0; i < _characters.Count; i++)
            {
                _characters[i].Texture = _bishopTexture;
            }

            _fontSystem = new FontStashSharp.FontSystem();
            using (var fs = TitleContainer.OpenStream("Content/DejaVuSans.ttf"))
            {
                _fontSystem.AddFont(fs);
            }
            _font = _fontSystem.GetFont(16);

            _colorEffect = new BasicEffect(GraphicsDevice)
            {
                TextureEnabled = false,
                VertexColorEnabled = true
            };

            _particleTexture = new Texture2D(GraphicsDevice, 1, 1);
            _particleTexture.SetData(new[] { Color.White });

            _grainTexture = new Texture2D(GraphicsDevice, 128, 128);
            _grainData = new Color[128 * 128];
        }

        protected override void Update(GameTime gameTime)
        {
            _time += (float)gameTime.ElapsedGameTime.TotalSeconds;

            var keyboard = Keyboard.GetState();
            bool esc = keyboard.IsKeyDown(Keys.Escape);
            if (esc && !_prevEscDown)
            {
                if (_menuOpen || _inventoryOpen || _saveOpen || _loadOpen || _settingsOpen)
                {
                    _menuOpen = false;
                    _inventoryOpen = false;
                    _saveOpen = false;
                    _loadOpen = false;
                    _settingsOpen = false;
                }
                else
                    _menuOpen = true;
            }
            _prevEscDown = esc;
            Vector3 direction = Vector3.Normalize(_cameraPosition - _cameraTarget);
            Vector3 right = Vector3.Normalize(Vector3.Cross(Vector3.Up, direction));
            Vector3 forward = Vector3.Normalize(Vector3.Cross(direction, right));

            Vector3 move = Vector3.Zero;
            if (!_menuOpen && !_inventoryOpen && !_saveOpen && !_loadOpen && !_settingsOpen)
            {
                if (keyboard.IsKeyDown(Keys.Left)) move -= right;
                if (keyboard.IsKeyDown(Keys.Right)) move += right;
                if (keyboard.IsKeyDown(Keys.Up)) move += forward;
                if (keyboard.IsKeyDown(Keys.Down)) move -= forward;
            }
            if (move != Vector3.Zero)
            {
                move *= CameraMoveSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;
                _cameraTarget += new Vector3(move.X, 0f, move.Z);
                _cameraPosition += new Vector3(move.X, 0f, move.Z);
            }

            if (keyboard.IsKeyDown(Keys.F6) && !_prevSave)
            {
                SaveGame("autosave");
            }
            if (keyboard.IsKeyDown(Keys.F7) && !_prevLoad)
            {
                LoadGame(Path.Combine("saves", "autosave.json"));
            }
            _prevSave = keyboard.IsKeyDown(Keys.F6);
            _prevLoad = keyboard.IsKeyDown(Keys.F7);

            var mouse = Mouse.GetState();
            if (mouse.RightButton == ButtonState.Pressed &&
                _prevMouseState.RightButton == ButtonState.Pressed &&
                !_menuOpen && !_inventoryOpen && !_saveOpen && !_loadOpen && !_settingsOpen)
            {
                float dragFactor = 0.002f * _cameraDistance;
                int dx = mouse.X - _prevMouseState.X;
                int dy = mouse.Y - _prevMouseState.Y;
                Vector3 delta = right * dx * dragFactor + forward * dy * dragFactor;
                _cameraTarget += new Vector3(delta.X, 0f, delta.Z);
                _cameraPosition += new Vector3(delta.X, 0f, delta.Z);
            }

            int scrollDelta = mouse.ScrollWheelValue - _prevMouseState.ScrollWheelValue;
            if (scrollDelta != 0)
            {
                float zoomStep = scrollDelta / 120f * 2f;
                _cameraDistance = MathHelper.Clamp(_cameraDistance - zoomStep, MinZoom, MaxZoom);
            }

            direction = Vector3.Normalize(_cameraPosition - _cameraTarget);
            _cameraPosition = _cameraTarget + direction * _cameraDistance;
            _cameraTarget.X = MathHelper.Clamp(_cameraTarget.X, -CameraMoveLimit, CameraMoveLimit);
            _cameraTarget.Z = MathHelper.Clamp(_cameraTarget.Z, -CameraMoveLimit, CameraMoveLimit);
            _cameraPosition = _cameraTarget + direction * _cameraDistance;
            _viewMatrix = Matrix.CreateLookAt(_cameraPosition, _cameraTarget, Vector3.Up);
            var campfireScreen = GraphicsDevice.Viewport.Project(Vector3.Zero, _projectionMatrix, _viewMatrix, Matrix.Identity);
            _campfireScreenPos = new Vector2(campfireScreen.X, campfireScreen.Y);
            if (!_moving)
            {
                if (mouse.LeftButton == ButtonState.Pressed &&
                    _prevMouseState.LeftButton == ButtonState.Released &&
                    _endTurnButtonRect.Contains(mouse.Position))
                {
                    EndTurn();
                }

                if (!_menuOpen && !_inventoryOpen && !_saveOpen && !_loadOpen && !_settingsOpen)
                    _hoveredTile = ScreenToBoard(mouse.Position);

                if (mouse.LeftButton == ButtonState.Pressed &&
                    _prevMouseState.LeftButton == ButtonState.Released &&
                    _bagRect.Contains(mouse.Position) &&
                    !_menuOpen && !_saveOpen && !_loadOpen && !_settingsOpen)
                {
                    _inventoryOpen = !_inventoryOpen;
                }

                if (_menuOpen)
                {
                    Rectangle menuRect = new Rectangle(_graphics.PreferredBackBufferWidth / 2 - 100,
                        _graphics.PreferredBackBufferHeight / 2 - 80, 200, 160);
                    Rectangle restartR = new Rectangle(menuRect.X + 10, menuRect.Y + 20, 180, 20);
                    Rectangle paramR = new Rectangle(menuRect.X + 10, menuRect.Y + 50, 180, 20);
                    Rectangle quitR = new Rectangle(menuRect.X + 10, menuRect.Y + 80, 180, 20);
                    if (mouse.LeftButton == ButtonState.Pressed && _prevMouseState.LeftButton == ButtonState.Released)
                    {
                        if (restartR.Contains(mouse.Position))
                            Initialize();
                        else if (paramR.Contains(mouse.Position))
                            _settingsOpen = !_settingsOpen;
                        else if (quitR.Contains(mouse.Position))
                            Exit();
                    }
                }

                if (mouse.LeftButton == ButtonState.Pressed &&
                    _prevMouseState.LeftButton == ButtonState.Released &&
                    !_endTurnButtonRect.Contains(mouse.Position) && _hoveredTile.HasValue &&
                    !_menuOpen && !_inventoryOpen && !_saveOpen && !_loadOpen && !_settingsOpen)
                {
                    var player = _characters.Find(c => c.IsPlayer);
                    bool[,] occ = GetOccupied();
                    var path = FindPath(player.BoardPos, _hoveredTile.Value, occ);
                    if (path != null && path.Count <= 8)
                    {
                        _selectedTile = _hoveredTile;
                        _playerPath = path;
                        _playerPathStart = player.BoardPos;
                    }
                }
            }
            if (mouse.LeftButton == ButtonState.Pressed && _dragging != DraggedPanel.None)
            {
                Point p = mouse.Position - _dragOffset;
                if (_dragging == DraggedPanel.Menu) _menuPos = p.ToVector2();
                else if (_dragging == DraggedPanel.Inventory) _inventoryPos = p.ToVector2();
                else if (_dragging == DraggedPanel.Save) _savePos = p.ToVector2();
                else if (_dragging == DraggedPanel.Load) _loadPos = p.ToVector2();
            }
            if (mouse.LeftButton == ButtonState.Released)
                _dragging = DraggedPanel.None;

            _prevMouseState = mouse;

            if (_moving)
            {
                bool anyMoving = false;
                for (int i = 0; i < _characters.Count; i++)
                {
                    var c = _characters[i];
                    if (c.Path.Count > 0)
                    {
                        anyMoving = true;
                        Point next = c.Path.Peek();
                        Vector2 from = BoardToScreen(c.BoardPos);
                        Vector2 to = BoardToScreen(next);
                        c.MoveProgress += 3f * (float)gameTime.ElapsedGameTime.TotalSeconds;
                        float t = MathF.Min(1f, c.MoveProgress);
                        c.ScreenPos = Vector2.Lerp(from, to, t);
                        if (c.MoveProgress >= 1f)
                        {
                            c.BoardPos = next;
                            c.Path.Dequeue();
                            c.MoveProgress = 0f;
                            c.ScreenPos = BoardToScreen(c.BoardPos);
                        }
                        _characters[i] = c;
                    }
                }

                _spinnerRotation += 5f * (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (!anyMoving)
                {
                    _moving = false;
                    _playerPath = null;
                    _aiPath = null;
                    _playerPathStart = null;
                    _aiPathStart = null;
                    _selectedTile = null;
                    _turn++;
                }
            }
            else
            {
                for (int i = 0; i < _characters.Count; i++)
                {
                    var c = _characters[i];
                    c.ScreenPos = BoardToScreen(c.BoardPos);
                    _characters[i] = c;
                }
            }

            var playerChar = _characters.Find(ch => ch.IsPlayer);
            foreach (var c in _characters)
            {
                float dist = Math.Abs(c.BoardPos.X - playerChar.BoardPos.X) + Math.Abs(c.BoardPos.Y - playerChar.BoardPos.Y);
                float target = MathHelper.Clamp(1f - (dist - 3f), 0f, 1f);
                c.Visibility = MathHelper.Lerp(c.Visibility, target, 5f * (float)gameTime.ElapsedGameTime.TotalSeconds);
            }

            float campDist = Math.Abs(playerChar.BoardPos.X) + Math.Abs(playerChar.BoardPos.Y);
            float campTarget = MathHelper.Clamp(1f - (campDist - 3f), 0f, 1f);
            _campfireAlpha = MathHelper.Lerp(_campfireAlpha, campTarget, 5f * (float)gameTime.ElapsedGameTime.TotalSeconds);

            UpdateParticles(gameTime, _lightParticles);
            UpdateParticles(gameTime, _dustParticles);
            UpdateFireParticles(gameTime, _fireParticles, _campfireScreenPos, 10f);
            UpdateFireParticles(gameTime, _smokeParticles, _campfireScreenPos, 20f);
            UpdateGrainTexture();

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            // The quads are defined with a counter-clockwise winding order.
            // MonoGame culls counter-clockwise primitives by default, which
            // resulted in nothing being rendered and the screen staying blue.
            // Switch to culling clockwise faces so our geometry becomes visible.
            GraphicsDevice.RasterizerState = RasterizerState.CullClockwise;

            // Ensure floor and curtain textures repeat when UVs exceed [0,1]
            GraphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;

            _effect.View = _viewMatrix;
            _effect.Projection = _projectionMatrix;
            _effect.World = Matrix.Identity;
            _effect.FogEnabled = true;
            _effect.FogColor = Vector3.Zero;
            _effect.FogStart = 30f;
            _effect.FogEnd = 80f;
            _effect.Texture = _floorTexture;

            foreach (var pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, _floorVertices, 0, 2);
            }

            // Draw semi-transparent grid overlay on top of the floor
            GraphicsDevice.BlendState = BlendState.AlphaBlend;
            _effect.Texture = _gridTexture;
            foreach (var pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, _floorVertices, 0, 2);
            }
            GraphicsDevice.BlendState = BlendState.Opaque;

            _effect.Texture = _curtainTexture;
            foreach (var pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, _curtainVertices, 0, 2);
            }

            DrawTileLights();
            DrawHighlights();
            DrawFog();
            if (_playerPath != null)
            {
                var player = _characters.Find(c => c.IsPlayer);
                Point start = _moving && _playerPathStart.HasValue ? _playerPathStart.Value : player.BoardPos;
                DrawPath(start, _playerPath, Color.LimeGreen);
            }
            if (_aiPath != null && (_moving || _aiPathStart.HasValue))
            {
                var ai = _characters.Find(c => !c.IsPlayer);
                Point start = _moving && _aiPathStart.HasValue ? _aiPathStart.Value : ai.BoardPos;
                DrawPath(start, _aiPath, Color.Orange);
            }
            DrawCampfire();
            DrawShadows();
            DrawCharacters();
            DrawParticles();

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _spriteBatch.Draw(_grainTexture,
                new Rectangle(0, 0, _graphics.PreferredBackBufferWidth,
                    _graphics.PreferredBackBufferHeight),
                Color.White * 0.05f);
            _spriteBatch.End();

            DrawUI();

            base.Draw(gameTime);
        }

        private void SpawnParticles(List<Particle> list, int count, Color color)
        {
            for (int i = 0; i < count; i++)
            {
                list.Add(new Particle
                {
                    Position = new Vector2(
                        _random.Next(0, _graphics.PreferredBackBufferWidth),
                        _random.Next(0, _graphics.PreferredBackBufferHeight)),
                    Velocity = new Vector2(
                        (float)(_random.NextDouble() * 2 - 1),
                        (float)(_random.NextDouble() * 2 - 1)),
                    Lifetime = 5f + (float)_random.NextDouble() * 5f,
                    Age = 0f,
                    Scale = 0.5f + (float)_random.NextDouble(),
                    Color = color
                });
            }
        }

        private void UpdateParticles(GameTime gameTime, List<Particle> particles)
        {
            for (int i = 0; i < particles.Count; i++)
            {
                var p = particles[i];
                p.Age += (float)gameTime.ElapsedGameTime.TotalSeconds;
                p.Position += p.Velocity;

                if (p.Age >= p.Lifetime ||
                    p.Position.X < 0 || p.Position.X > _graphics.PreferredBackBufferWidth ||
                    p.Position.Y < 0 || p.Position.Y > _graphics.PreferredBackBufferHeight)
                {
                    p.Position = new Vector2(
                        _random.Next(0, _graphics.PreferredBackBufferWidth),
                        _random.Next(0, _graphics.PreferredBackBufferHeight));
                    p.Age = 0f;
                    p.Lifetime = 5f + (float)_random.NextDouble() * 5f;
                    p.Velocity = new Vector2(
                        (float)(_random.NextDouble() * 2 - 1),
                        (float)(_random.NextDouble() * 2 - 1));
                }
                particles[i] = p;
            }
        }

        private void DrawParticles()
        {
            _spriteBatch.Begin(blendState: BlendState.Additive);
            foreach (var p in _lightParticles)
            {
                Vector2 offset = new Vector2((float)Math.Sin(_time * 0.5f) * 5f,
                    (float)Math.Cos(_time * 0.5f) * 5f);
                _spriteBatch.Draw(_particleTexture, p.Position + offset, null, p.Color,
                    0f, Vector2.Zero, p.Scale, SpriteEffects.None, 0f);
            }
            _spriteBatch.End();

            _spriteBatch.Begin();
            foreach (var p in _dustParticles)
            {
                Vector2 offset = new Vector2((float)Math.Sin(_time) * 2f,
                    (float)Math.Cos(_time) * 2f);
                _spriteBatch.Draw(_particleTexture, p.Position + offset, null, p.Color,
                    0f, Vector2.Zero, p.Scale, SpriteEffects.None, 0f);
            }
            _spriteBatch.End();

            _spriteBatch.Begin(blendState: BlendState.Additive);
            foreach (var p in _fireParticles)
            {
                _spriteBatch.Draw(_particleTexture, p.Position, null, p.Color,
                    0f, Vector2.Zero, p.Scale, SpriteEffects.None, 0f);
            }
            _spriteBatch.End();

            _spriteBatch.Begin();
            foreach (var p in _smokeParticles)
            {
                _spriteBatch.Draw(_particleTexture, p.Position, null, p.Color,
                    0f, Vector2.Zero, p.Scale, SpriteEffects.None, 0f);
            }
            _spriteBatch.End();
        }

        private void DrawCampfire()
        {
            float flicker = (0.1f + (float)_random.NextDouble() * 0.05f) * 0.2f;
            float spriteScale = 0.5f * _initialCameraDistance / _cameraDistance;
            _spriteBatch.Begin(blendState: BlendState.AlphaBlend);
            _spriteBatch.Draw(_campfireTexture, _campfireScreenPos - new Vector2(_campfireTexture.Width * spriteScale / 2f, _campfireTexture.Height * spriteScale),
                null, Color.White * _campfireAlpha, 0f, Vector2.Zero, spriteScale, SpriteEffects.None, 0f);
            _spriteBatch.Draw(_lightGradientTexture, _campfireScreenPos - new Vector2(128 * spriteScale, 128 * spriteScale),
                null, Color.White * flicker * _campfireAlpha, 0f, Vector2.Zero, 2f * spriteScale, SpriteEffects.None, 0f);
            _spriteBatch.End();
        }

        private Vector2 BoardToScreen(Point boardPos)
        {
            return BoardToScreen(new Vector2(boardPos.X, boardPos.Y));
        }

        private Vector2 BoardToScreen(Vector2 boardPos)
        {
            Vector3 world = new Vector3((boardPos.X - 3.5f) * CellSize, 0f,
                (boardPos.Y - 3.5f) * CellSize);
            var screen = GraphicsDevice.Viewport.Project(world,
                _projectionMatrix, _viewMatrix, Matrix.Identity);
            return new Vector2(screen.X, screen.Y);
        }

        private Point? ScreenToBoard(Point screen)
        {
            Vector3 nearSource = new Vector3(screen.X, screen.Y, 0f);
            Vector3 farSource = new Vector3(screen.X, screen.Y, 1f);
            Vector3 nearPoint = GraphicsDevice.Viewport.Unproject(nearSource,
                _projectionMatrix, _viewMatrix, Matrix.Identity);
            Vector3 farPoint = GraphicsDevice.Viewport.Unproject(farSource,
                _projectionMatrix, _viewMatrix, Matrix.Identity);
            Vector3 direction = Vector3.Normalize(farPoint - nearPoint);
            if (direction.Y >= 0f)
                return null;
            float distance = -nearPoint.Y / direction.Y;
            Vector3 world = nearPoint + direction * distance;
            int x = (int)Math.Floor(world.X / CellSize + 4f);
            int y = (int)Math.Floor(world.Z / CellSize + 4f);
            if (x < 0 || x > 7 || y < 0 || y > 7)
                return null;
            return new Point(x, y);
        }

        private void DrawLine(Vector2 start, Vector2 end, Color color, float thickness)
        {
            Vector2 edge = end - start;
            float angle = (float)Math.Atan2(edge.Y, edge.X);
            _spriteBatch.Draw(_particleTexture, start, null, color, angle, Vector2.Zero,
                new Vector2(edge.Length(), thickness), SpriteEffects.None, 0f);
        }

        private void DrawPanel(Rectangle rect)
        {
            _spriteBatch.Draw(_particleTexture, rect, Color.Black * 0.8f);
            int t = 2;
            _spriteBatch.Draw(_particleTexture, new Rectangle(rect.X, rect.Y, rect.Width, t), Color.Gray);
            _spriteBatch.Draw(_particleTexture, new Rectangle(rect.X, rect.Bottom - t, rect.Width, t), Color.Gray);
            _spriteBatch.Draw(_particleTexture, new Rectangle(rect.X, rect.Y, t, rect.Height), Color.Gray);
            _spriteBatch.Draw(_particleTexture, new Rectangle(rect.Right - t, rect.Y, t, rect.Height), Color.Gray);
        }

        private void DrawPath(Point start, List<Point> path, Color color)
        {
            if (path == null || path.Count == 0)
                return;

            _spriteBatch.Begin();
            Vector2 from = BoardToScreen(start);
            foreach (var step in path)
            {
                Vector2 to = BoardToScreen(step);
                DrawLine(from, to, color, 3f);
                from = to;
            }
            _spriteBatch.End();
        }

        private List<Point> FindPath(Point start, Point goal, bool[,] occupied)
        {
            if (occupied[goal.X, goal.Y])
                return null;

            Queue<Point> q = new Queue<Point>();
            q.Enqueue(start);
            Dictionary<Point, Point> prev = new Dictionary<Point, Point>();
            prev[start] = start;
            Point[] dirs = new[] { new Point(1,0), new Point(-1,0), new Point(0,1), new Point(0,-1) };

            while (q.Count > 0)
            {
                Point cur = q.Dequeue();
                if (cur == goal)
                    break;
                foreach (var d in dirs)
                {
                    int nx = cur.X + d.X;
                    int ny = cur.Y + d.Y;
                    if (nx < 0 || nx > 7 || ny < 0 || ny > 7)
                        continue;
                    Point np = new Point(nx, ny);
                    if (occupied[nx, ny] && np != goal)
                        continue;
                    if (!prev.ContainsKey(np))
                    {
                        prev[np] = cur;
                        q.Enqueue(np);
                    }
                }
            }

            if (!prev.ContainsKey(goal))
                return null;

            List<Point> path = new List<Point>();
            Point p = goal;
            while (p != start)
            {
                path.Insert(0, p);
                p = prev[p];
            }
            return path;
        }

        private bool[,] GetOccupied()
        {
            bool[,] occ = new bool[8, 8];
            foreach (var c in _characters)
            {
                occ[c.BoardPos.X, c.BoardPos.Y] = true;
            }
            return occ;
        }

        private void DrawCharacters()
        {
            float spriteScale = 0.5f * _initialCameraDistance / _cameraDistance;
            _spriteBatch.Begin(blendState: BlendState.AlphaBlend);
            foreach (var c in _characters)
            {
                var tex = c.Texture ?? _pawnTexture;
                Rectangle bounds = _textureBounds.ContainsKey(tex) ? _textureBounds[tex] : new Rectangle(0,0,tex.Width,tex.Height);
                Vector2 offset = new Vector2(bounds.Width * spriteScale / 2f,
                                             bounds.Height * spriteScale);
                _spriteBatch.Draw(tex, c.ScreenPos - offset,
                    bounds, Color.White * c.Visibility, 0f, new Vector2(bounds.Width/2f,bounds.Height), spriteScale, SpriteEffects.None, 0f);
                if (c.IsPlayer)
                {
                    Vector2 nameSize = _font.MeasureString("Player");
                    Vector2 namePos = new Vector2(c.ScreenPos.X - nameSize.X/2f, c.ScreenPos.Y - offset.Y - 20f);
                    _spriteBatch.DrawString(_font, "Player", namePos, Color.White);
                }
            }
            _spriteBatch.End();
        }

        private void DrawShadows()
        {
            _spriteBatch.Begin(blendState: BlendState.AlphaBlend);
            foreach (var light in _lightPositions)
            {
                var screen = GraphicsDevice.Viewport.Project(light,
                    _projectionMatrix, _viewMatrix, Matrix.Identity);
                Vector2 lightPos = new Vector2(screen.X, screen.Y);

                foreach (var c in _characters)
                {
                    var tex = c.Texture ?? _pawnTexture;
                    Rectangle bounds = _textureBounds.ContainsKey(tex) ? _textureBounds[tex] : new Rectangle(0,0,tex.Width,tex.Height);
                    Vector2 dir = c.ScreenPos - lightPos;
                    float dist = dir.Length();
                    if (dist < 1f)
                        dist = 1f;
                    dir /= dist;

                    float rotation = (float)Math.Atan2(dir.Y, dir.X) + MathHelper.PiOver2;
                    float baseScale = 0.5f * _initialCameraDistance / _cameraDistance;
                    float length = MathHelper.Clamp(dist / 100f, 0.5f, 2f);
                    Vector2 scale = new Vector2(baseScale * 0.3f, baseScale) * length;
                    Vector2 pos = c.ScreenPos + new Vector2(0f, baseScale * 2f);

                    _spriteBatch.Draw(tex, pos, bounds, new Color(0, 0, 0, (byte)(150 * c.Visibility)), rotation,
                        new Vector2(bounds.Width / 2f, bounds.Height), scale, SpriteEffects.None, 0f);
                }
            }
            _spriteBatch.End();
        }
        
        private void DrawTileLights()
        {
            const int range = 3;
            const float tileSize = 20f / 8f; // board is 8x8 tiles on a 20x20 plane

            _colorEffect.View = _viewMatrix;
            _colorEffect.Projection = _projectionMatrix;
            _colorEffect.World = Matrix.Identity;
            _colorEffect.FogEnabled = true;
            _colorEffect.FogColor = Vector3.Zero;
            _colorEffect.FogStart = 30f;
            _colorEffect.FogEnd = 80f;

            GraphicsDevice.BlendState = BlendState.Additive;
            for (int x = -range; x <= range - 1; x++)
            {
                for (int z = -range; z <= range - 1; z++)
                {
                    DrawTileQuad(x * tileSize, z * tileSize, tileSize,
                        new Color(255, 240, 150, 100));
                }
            }
            GraphicsDevice.BlendState = BlendState.Opaque;
        }

        private void DrawTileQuad(float startX, float startZ, float size, Color color)
        {
            VertexPositionColor[] verts = new VertexPositionColor[6];
            float y = 0.01f; // slightly above floor to avoid z-fighting
            verts[0] = new VertexPositionColor(new Vector3(startX, y, startZ), color);
            verts[1] = new VertexPositionColor(new Vector3(startX + size, y, startZ), color);
            verts[2] = new VertexPositionColor(new Vector3(startX + size, y, startZ + size), color);
            verts[3] = new VertexPositionColor(new Vector3(startX + size, y, startZ + size), color);
            verts[4] = new VertexPositionColor(new Vector3(startX, y, startZ + size), color);
            verts[5] = new VertexPositionColor(new Vector3(startX, y, startZ), color);

            foreach (var pass in _colorEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, verts, 0, 2);
            }
        }

        private void DrawTileBorder(Point tile, Color color)
        {
            Vector2 tl = BoardToScreen(new Vector2(tile.X - 0.5f, tile.Y - 0.5f));
            Vector2 tr = BoardToScreen(new Vector2(tile.X + 0.5f, tile.Y - 0.5f));
            Vector2 br = BoardToScreen(new Vector2(tile.X + 0.5f, tile.Y + 0.5f));
            Vector2 bl = BoardToScreen(new Vector2(tile.X - 0.5f, tile.Y + 0.5f));

            _spriteBatch.Begin();
            DrawLine(tl, tr, color, 2f);
            DrawLine(tr, br, color, 2f);
            DrawLine(br, bl, color, 2f);
            DrawLine(bl, tl, color, 2f);
            _spriteBatch.End();
        }

        private void DrawHighlights()
        {
            if (_hoveredTile.HasValue && !_moving)
            {
                var player = _characters.Find(c => c.IsPlayer);
                bool[,] occ = GetOccupied();
                var path = FindPath(player.BoardPos, _hoveredTile.Value, occ);
                bool ok = path != null && path.Count <= 8 && !occ[_hoveredTile.Value.X, _hoveredTile.Value.Y];
                Color fill = ok ? new Color(0, 255, 0, 40) : new Color(255, 0, 0, 40);
                Color border = ok ? Color.LimeGreen : Color.Red;

                GraphicsDevice.BlendState = BlendState.AlphaBlend;
                DrawTileQuad((_hoveredTile.Value.X - 4) * CellSize, (_hoveredTile.Value.Y - 4) * CellSize, CellSize, fill);
                DrawTileBorder(_hoveredTile.Value, border);
                GraphicsDevice.BlendState = BlendState.Opaque;
            }

            if (_selectedTile.HasValue)
            {
                GraphicsDevice.BlendState = BlendState.AlphaBlend;
                DrawTileQuad((_selectedTile.Value.X - 4) * CellSize, (_selectedTile.Value.Y - 4) * CellSize, CellSize, new Color(0, 200, 0, 60));
                DrawTileBorder(_selectedTile.Value, Color.Green);
                GraphicsDevice.BlendState = BlendState.Opaque;
            }
        }

        private void DrawFog()
        {
            var player = _characters.Find(c => c.IsPlayer);
            GraphicsDevice.BlendState = BlendState.AlphaBlend;
            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    float dist = Math.Abs(x - player.BoardPos.X) + Math.Abs(y - player.BoardPos.Y);
                    float alpha = MathHelper.Clamp((dist - 3f), 0f, 1f);
                    if (alpha > 0f)
                    {
                        DrawTileQuad((x - 4) * CellSize, (y - 4) * CellSize, CellSize, new Color(0, 0, 0, (byte)(alpha * 200)));
                    }
                }
            }
            GraphicsDevice.BlendState = BlendState.Opaque;
        }

        private void SpawnParticlesAt(List<Particle> list, int count, Color color,
            Vector2 center, float range)
        {
            for (int i = 0; i < count; i++)
            {
                list.Add(new Particle
                {
                    Position = center + new Vector2(
                        (float)(_random.NextDouble() * 2 - 1) * range,
                        (float)(_random.NextDouble() * 2 - 1) * range),
                    Velocity = new Vector2(
                        (float)(_random.NextDouble() * 2 - 1),
                        (float)(_random.NextDouble() * -2 - 0.5f)),
                    Lifetime = 1f + (float)_random.NextDouble(),
                    Age = 0f,
                    Scale = 0.5f + (float)_random.NextDouble() * 0.5f,
                    Color = color
                });
            }
        }

        private void UpdateFireParticles(GameTime gameTime, List<Particle> particles,
            Vector2 center, float range)
        {
            for (int i = 0; i < particles.Count; i++)
            {
                var p = particles[i];
                p.Age += (float)gameTime.ElapsedGameTime.TotalSeconds;
                p.Position += p.Velocity;

                if (p.Age >= p.Lifetime)
                {
                    p.Position = center + new Vector2(
                        (float)(_random.NextDouble() * 2 - 1) * range,
                        (float)(_random.NextDouble() * 2 - 1) * range);
                    p.Age = 0f;
                    p.Lifetime = 1f + (float)_random.NextDouble();
                    p.Velocity = new Vector2(
                        (float)(_random.NextDouble() * 2 - 1),
                        (float)(_random.NextDouble() * -2 - 0.5f));
                }
                particles[i] = p;
            }
        }

        private void UpdateGrainTexture()
        {
            for (int i = 0; i < _grainData.Length; i++)
            {
                byte value = (byte)_random.Next(256);
                _grainData[i] = new Color(value, value, value);
            }
            _grainTexture.SetData(_grainData);
        }

        private void EndTurn()
        {
            if (_moving)
                return;

            var player = _characters.Find(c => c.IsPlayer);
            var ai = _characters.Find(c => !c.IsPlayer);

            bool[,] occ = GetOccupied();

            if (_playerPath != null)
            {
                foreach (var step in _playerPath)
                    player.Path.Enqueue(step);
                if (!_playerPathStart.HasValue)
                    _playerPathStart = player.BoardPos;
            }

            Point aiDest;
            List<Point> aiPath = null;
            do
            {
                aiDest = new Point(_random.Next(8), _random.Next(8));
                aiPath = FindPath(ai.BoardPos, aiDest, occ);
            } while (aiPath == null || aiPath.Count > 8);

            foreach (var step in aiPath)
                ai.Path.Enqueue(step);

            _aiPath = aiPath;
            _aiPathStart = ai.BoardPos;

            for (int i = 0; i < _characters.Count; i++)
            {
                if (_characters[i].IsPlayer)
                    _characters[i] = player;
                else
                    _characters[i] = ai;
            }

            _moving = true;
            _spinnerRotation = 0f;
            _hoveredTile = null;
        }

        private void DrawUI()
        {
            Rectangle bar = new Rectangle(0,
                _graphics.PreferredBackBufferHeight - ToolbarHeight,
                _graphics.PreferredBackBufferWidth, ToolbarHeight);
            _spriteBatch.Begin();
            _spriteBatch.Draw(_particleTexture, bar, Color.Black * 0.5f);
            if (!_moving)
            {
                _spriteBatch.Draw(_endTurnButtonTexture, _endTurnButtonRect, Color.White);
                _spriteBatch.Draw(_bagTexture, _bagRect, Color.White);

                string buttonText = "End Turn";
                Vector2 btnSize = _font.MeasureString(buttonText);
                Vector2 btnPos = new Vector2(
                    _endTurnButtonRect.Center.X - btnSize.X / 2f,
                    _endTurnButtonRect.Center.Y - btnSize.Y / 2f);
                _spriteBatch.DrawString(_font, buttonText, btnPos, Color.White);
            }

            float marginX = 20f;
            float lineHeight = _font.LineHeight;
            float vertSpace = (ToolbarHeight - 3 * lineHeight) / 4f;
            float startY = _graphics.PreferredBackBufferHeight - ToolbarHeight + vertSpace;
            _spriteBatch.DrawString(_font, $"Entities: {_characters.Count}", new Vector2(marginX, startY), Color.White);
            startY += lineHeight + vertSpace;
            _spriteBatch.DrawString(_font, $"Turn: {_turn}", new Vector2(marginX, startY), Color.White);
            startY += lineHeight + vertSpace;
            _spriteBatch.DrawString(_font, $"Map: {MapName}", new Vector2(marginX, startY), Color.White);
            _spriteBatch.End();

            if (_inventoryOpen)
            {
                Rectangle invRect = new Rectangle((int)_inventoryPos.X, (int)_inventoryPos.Y, 384, 192);
                Rectangle closeRect = new Rectangle(invRect.Right - 20, invRect.Y, 20, 20);
                _spriteBatch.Begin();
                DrawPanel(invRect);
                _spriteBatch.DrawString(_font, "X", closeRect.Location.ToVector2(), Color.White);
                for (int x = 0; x < 6; x++)
                {
                    for (int y = 0; y < 2; y++)
                    {
                        Rectangle slot = new Rectangle(invRect.X + 8 + x * 60,
                            invRect.Y + 30 + y * 60, 56, 56);
                        _spriteBatch.Draw(_particleTexture, slot, Color.Gray * 0.4f);
                    }
                }
                _spriteBatch.End();

                if (Mouse.GetState().LeftButton == ButtonState.Pressed && _prevMouseState.LeftButton == ButtonState.Released)
                {
                    if (closeRect.Contains(Mouse.GetState().Position))
                        _inventoryOpen = false;
                    else if (new Rectangle(invRect.X, invRect.Y, invRect.Width, 20).Contains(Mouse.GetState().Position))
                    {
                        _dragging = DraggedPanel.Inventory;
                        _dragOffset = Mouse.GetState().Position - new Point(invRect.X, invRect.Y);
                    }
                }
            }

            if (_menuOpen)
            {
                Rectangle menuRect = new Rectangle((int)_menuPos.X, (int)_menuPos.Y, 200, 200);
                Rectangle closeRect = new Rectangle(menuRect.Right - 20, menuRect.Y, 20, 20);
                _spriteBatch.Begin();
                DrawPanel(menuRect);
                _spriteBatch.DrawString(_font, "X", closeRect.Location.ToVector2(), Color.White);
                _spriteBatch.DrawString(_font, "Relancer", new Vector2(menuRect.X + 20, menuRect.Y + 30), Color.White);
                _spriteBatch.DrawString(_font, "Parametres", new Vector2(menuRect.X + 20, menuRect.Y + 60), Color.White);
                _spriteBatch.DrawString(_font, "Sauvegarder", new Vector2(menuRect.X + 20, menuRect.Y + 90), Color.White);
                _spriteBatch.DrawString(_font, "Charger", new Vector2(menuRect.X + 20, menuRect.Y + 120), Color.White);
                _spriteBatch.DrawString(_font, "Quitter", new Vector2(menuRect.X + 20, menuRect.Y + 150), Color.White);
                _spriteBatch.End();

                if (Mouse.GetState().LeftButton == ButtonState.Pressed && _prevMouseState.LeftButton == ButtonState.Released)
                {
                    Point mp = Mouse.GetState().Position;
                    if (closeRect.Contains(mp))
                        _menuOpen = false;
                    else if (new Rectangle(menuRect.X, menuRect.Y, menuRect.Width, 20).Contains(mp))
                    {
                        _dragging = DraggedPanel.Menu;
                        _dragOffset = mp - new Point(menuRect.X, menuRect.Y);
                    }
                    else if (new Rectangle(menuRect.X + 20, menuRect.Y + 30, 160, 20).Contains(mp))
                        Initialize();
                    else if (new Rectangle(menuRect.X + 20, menuRect.Y + 60, 160, 20).Contains(mp))
                        _settingsOpen = !_settingsOpen;
                    else if (new Rectangle(menuRect.X + 20, menuRect.Y + 90, 160, 20).Contains(mp))
                        {_saveOpen = true; _saveInput = string.Empty; }
                    else if (new Rectangle(menuRect.X + 20, menuRect.Y + 120, 160, 20).Contains(mp))
                        { _loadOpen = true; _saveFiles = new List<string>(Directory.Exists("saves") ? Directory.GetFiles("saves", "*.json") : Array.Empty<string>()); _loadScroll = 0; }
                    else if (new Rectangle(menuRect.X + 20, menuRect.Y + 150, 160, 20).Contains(mp))
                        Exit();
                }
            }

            if (_saveOpen)
            {
                Rectangle svRect = new Rectangle((int)_savePos.X, (int)_savePos.Y, 300, 120);
                Rectangle closeRect = new Rectangle(svRect.Right - 20, svRect.Y, 20, 20);
                Rectangle btnRect = new Rectangle(svRect.X + 10, svRect.Bottom - 30, 80, 20);
                _spriteBatch.Begin();
                DrawPanel(svRect);
                _spriteBatch.DrawString(_font, "X", closeRect.Location.ToVector2(), Color.White);
                _spriteBatch.DrawString(_font, "Nom:", new Vector2(svRect.X + 10, svRect.Y + 30), Color.White);
                _spriteBatch.DrawString(_font, _saveInput, new Vector2(svRect.X + 60, svRect.Y + 30), Color.White);
                _spriteBatch.Draw(_particleTexture, btnRect, Color.Gray);
                _spriteBatch.DrawString(_font, "Sauver", new Vector2(btnRect.X + 5, btnRect.Y + 2), Color.White);
                _spriteBatch.End();

                if (Mouse.GetState().LeftButton == ButtonState.Pressed && _prevMouseState.LeftButton == ButtonState.Released)
                {
                    Point mp = Mouse.GetState().Position;
                    if (closeRect.Contains(mp))
                        _saveOpen = false;
                    else if (btnRect.Contains(mp))
                    {
                        string name = string.IsNullOrWhiteSpace(_saveInput) ? "save" + DateTime.Now.Ticks : _saveInput;
                        SaveGame(name);
                        _saveOpen = false;
                    }
                    else if (new Rectangle(svRect.X, svRect.Y, svRect.Width, 20).Contains(mp))
                    {
                        _dragging = DraggedPanel.Save;
                        _dragOffset = mp - new Point(svRect.X, svRect.Y);
                    }
                }
            }

            if (_loadOpen)
            {
                Rectangle ldRect = new Rectangle((int)_loadPos.X, (int)_loadPos.Y, 300, 240);
                Rectangle closeRect = new Rectangle(ldRect.Right - 20, ldRect.Y, 20, 20);
                _spriteBatch.Begin();
                DrawPanel(ldRect);
                _spriteBatch.DrawString(_font, "X", closeRect.Location.ToVector2(), Color.White);
                int y = ldRect.Y + 30;
                int index = 0;
                foreach (var file in _saveFiles)
                {
                    if (index >= _loadScroll && index < _loadScroll + 8)
                    {
                        string name = Path.GetFileNameWithoutExtension(file);
                        _spriteBatch.DrawString(_font, name, new Vector2(ldRect.X + 10, y), Color.White);
                        y += 20;
                    }
                    index++;
                }
                _spriteBatch.End();

                if (Mouse.GetState().LeftButton == ButtonState.Pressed && _prevMouseState.LeftButton == ButtonState.Released)
                {
                    Point mp = Mouse.GetState().Position;
                    if (closeRect.Contains(mp))
                        _loadOpen = false;
                    else if (new Rectangle(ldRect.X, ldRect.Y, ldRect.Width, 20).Contains(mp))
                    {
                        _dragging = DraggedPanel.Load;
                        _dragOffset = mp - new Point(ldRect.X, ldRect.Y);
                    }
                    else
                    {
                        y = ldRect.Y + 30;
                        index = 0;
                        foreach (var file in _saveFiles)
                        {
                            if (index >= _loadScroll && index < _loadScroll + 8)
                            {
                                Rectangle r = new Rectangle(ldRect.X + 10, y, 200, 20);
                                if (r.Contains(mp))
                                {
                                    LoadGame(file);
                                    _loadOpen = false;
                                    _menuOpen = false;
                                    break;
                                }
                                y += 20;
                            }
                            index++;
                        }
                    }
                }

                int scroll = Mouse.GetState().ScrollWheelValue - _prevMouseState.ScrollWheelValue;
                if (scroll != 0)
                {
                    int maxScroll = Math.Max(0, _saveFiles.Count - 8);
                    _loadScroll = Math.Clamp(_loadScroll - scroll / 120, 0, maxScroll);
                }
            }

            if (_settingsOpen)
            {
                Rectangle stRect = new Rectangle((int)_settingsPos.X, (int)_settingsPos.Y, 300, 120);
                Rectangle closeRect = new Rectangle(stRect.Right - 20, stRect.Y, 20, 20);
                Rectangle winBtn = new Rectangle(stRect.X + 20, stRect.Y + 40, 120, 20);
                Rectangle fsBtn = new Rectangle(stRect.X + 160, stRect.Y + 40, 120, 20);
                _spriteBatch.Begin();
                DrawPanel(stRect);
                _spriteBatch.DrawString(_font, "X", closeRect.Location.ToVector2(), Color.White);
                _spriteBatch.Draw(_particleTexture, winBtn, _fullscreen ? Color.Gray : Color.White);
                _spriteBatch.Draw(_particleTexture, fsBtn, _fullscreen ? Color.White : Color.Gray);
                _spriteBatch.DrawString(_font, "Fenetre", new Vector2(winBtn.X + 10, winBtn.Y + 2), Color.Black);
                _spriteBatch.DrawString(_font, "Plein", new Vector2(fsBtn.X + 10, fsBtn.Y + 2), Color.Black);
                _spriteBatch.End();

                if (Mouse.GetState().LeftButton == ButtonState.Pressed && _prevMouseState.LeftButton == ButtonState.Released)
                {
                    Point mp = Mouse.GetState().Position;
                    if (closeRect.Contains(mp))
                        _settingsOpen = false;
                    else if (winBtn.Contains(mp))
                    {
                        _fullscreen = false;
                        ApplyResolution(1280, 720, false);
                    }
                    else if (fsBtn.Contains(mp))
                    {
                        _fullscreen = true;
                        ApplyResolution(1920, 1080, true);
                    }
                }
            }

            if (_moving)
            {
                Vector2 center = new Vector2(_graphics.PreferredBackBufferWidth / 2f, _graphics.PreferredBackBufferHeight / 2f);
                _spriteBatch.Begin();
                _spriteBatch.Draw(_spinnerTexture, center, null, Color.White, _spinnerRotation,
                    new Vector2(_spinnerTexture.Width / 2f, _spinnerTexture.Height / 2f), 1f, SpriteEffects.None, 0f);
                _spriteBatch.End();
            }
        }

        private class SaveData
        {
            public Point Player;
            public Point AI;
            public int Turn;
        }

        private void SaveGame(string name)
        {
            var data = new SaveData
            {
                Player = _characters.Find(c => c.IsPlayer).BoardPos,
                AI = _characters.Find(c => !c.IsPlayer).BoardPos,
                Turn = _turn
            };
            Directory.CreateDirectory("saves");
            string path = Path.Combine("saves", name + ".json");
            File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(data));
        }

        private void LoadGame(string path)
        {
            if (!File.Exists(path)) return;
            var data = System.Text.Json.JsonSerializer.Deserialize<SaveData>(File.ReadAllText(path));
            var player = _characters.Find(c => c.IsPlayer);
            var ai = _characters.Find(c => !c.IsPlayer);
            player.BoardPos = data.Player;
            ai.BoardPos = data.AI;
            _turn = data.Turn;
            _characters[0] = player;
            _characters[1] = ai;
        }

        private void UpdateUILayout()
        {
            int buttonWidth = 140;
            int buttonHeight = 40;
            _endTurnButtonRect = new Rectangle(
                _graphics.PreferredBackBufferWidth - buttonWidth - 20,
                _graphics.PreferredBackBufferHeight - ToolbarHeight + (ToolbarHeight - buttonHeight) / 2,
                buttonWidth,
                buttonHeight);

            int bagSize = 48;
            _bagRect = new Rectangle(220,
                _graphics.PreferredBackBufferHeight - ToolbarHeight + (ToolbarHeight - bagSize) / 2,
                bagSize, bagSize);
        }

        private void OnTextInput(object sender, TextInputEventArgs e)
        {
            if (_saveOpen)
            {
                if (e.Key == Keys.Back && _saveInput.Length > 0)
                    _saveInput = _saveInput[..^1];
                else if (!char.IsControl(e.Character))
                    _saveInput += e.Character;
            }
        }

        private void ApplyResolution(int w, int h, bool fullscreen)
        {
            _graphics.IsFullScreen = fullscreen;
            _graphics.PreferredBackBufferWidth = w;
            _graphics.PreferredBackBufferHeight = h;
            _graphics.ApplyChanges();
            UpdateUILayout();
        }
    }
}
