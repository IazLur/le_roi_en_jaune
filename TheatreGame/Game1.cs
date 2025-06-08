using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
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

        private Vector2 _campfireScreenPos;

        private Texture2D _endTurnButtonTexture;
        private Rectangle _endTurnButtonRect;
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
            var cameraPosition = new Vector3(20, 20, 20);
            var target = Vector3.Zero;
            var up = Vector3.Up;
            _viewMatrix = Matrix.CreateLookAt(cameraPosition, target, up);
            _projectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(45f),
                GraphicsDevice.Viewport.AspectRatio, 0.1f, 100f);

            // Create floor quad with normals for lighting
            _floorVertices = new VertexPositionNormalTexture[6];
            var floorNormal = Vector3.Up;
            _floorVertices[0] = new VertexPositionNormalTexture(new Vector3(-10, 0, -10), floorNormal, new Vector2(0, 0));
            _floorVertices[1] = new VertexPositionNormalTexture(new Vector3(-10, 0, 10), floorNormal, new Vector2(0, 1));
            _floorVertices[2] = new VertexPositionNormalTexture(new Vector3(10, 0, -10), floorNormal, new Vector2(1, 0));
            _floorVertices[3] = new VertexPositionNormalTexture(new Vector3(10, 0, -10), floorNormal, new Vector2(1, 0));
            _floorVertices[4] = new VertexPositionNormalTexture(new Vector3(-10, 0, 10), floorNormal, new Vector2(0, 1));
            _floorVertices[5] = new VertexPositionNormalTexture(new Vector3(10, 0, 10), floorNormal, new Vector2(1, 1));

            // Create curtain quad behind the stage
            _curtainVertices = new VertexPositionNormalTexture[6];
            var curtainNormal = Vector3.Backward;
            _curtainVertices[0] = new VertexPositionNormalTexture(new Vector3(-10, 0, -10), curtainNormal, new Vector2(0, 1));
            _curtainVertices[1] = new VertexPositionNormalTexture(new Vector3(10, 0, -10), curtainNormal, new Vector2(1, 1));
            _curtainVertices[2] = new VertexPositionNormalTexture(new Vector3(-10, 10, -10), curtainNormal, new Vector2(0, 0));
            _curtainVertices[3] = new VertexPositionNormalTexture(new Vector3(-10, 10, -10), curtainNormal, new Vector2(0, 0));
            _curtainVertices[4] = new VertexPositionNormalTexture(new Vector3(10, 0, -10), curtainNormal, new Vector2(1, 1));
            _curtainVertices[5] = new VertexPositionNormalTexture(new Vector3(10, 10, -10), curtainNormal, new Vector2(1, 0));

            _random = new Random();
            _lightParticles = new List<Particle>();
            _dustParticles = new List<Particle>();
            _fireParticles = new List<Particle>();
            _smokeParticles = new List<Particle>();
            SpawnParticles(_lightParticles, 100, new Color(255, 255, 200, 200));
            SpawnParticles(_dustParticles, 200, new Color(150, 120, 100, 150));

            var screenPoint = GraphicsDevice.Viewport.Project(Vector3.Zero,
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

            int buttonWidth = 140;
            int buttonHeight = 40;
            _endTurnButtonRect = new Rectangle(
                _graphics.PreferredBackBufferWidth - buttonWidth - 20,
                _graphics.PreferredBackBufferHeight - ToolbarHeight + (ToolbarHeight - buttonHeight) / 2,
                buttonWidth,
                buttonHeight);
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
            _floorTexture = Texture2D.FromStream(
                GraphicsDevice,
                TitleContainer.OpenStream("Content/stage_floor.png"));
            _curtainTexture = Texture2D.FromStream(
                GraphicsDevice,
                TitleContainer.OpenStream("Content/curtain.png"));
            _gridTexture = Texture2D.FromStream(
                GraphicsDevice,
                TitleContainer.OpenStream("Content/grid_overlay.png"));

            _campfireTexture = Texture2D.FromStream(
                GraphicsDevice,
                TitleContainer.OpenStream("Content/campfire.png"));

            _pawnTexture = Texture2D.FromStream(
                GraphicsDevice,
                TitleContainer.OpenStream("Content/pawn.png"));
            _bishopTexture = Texture2D.FromStream(
                GraphicsDevice,
                TitleContainer.OpenStream("Content/bishop.png"));
            _lightGradientTexture = Texture2D.FromStream(
                GraphicsDevice,
                TitleContainer.OpenStream("Content/light_gradient.png"));
            _endTurnButtonTexture = Texture2D.FromStream(
                GraphicsDevice,
                TitleContainer.OpenStream("Content/end_turn.png"));
            _spinnerTexture = Texture2D.FromStream(
                GraphicsDevice,
                TitleContainer.OpenStream("Content/spinner.png"));

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
            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            _time += (float)gameTime.ElapsedGameTime.TotalSeconds;

            var mouse = Mouse.GetState();
            if (!_moving)
            {
                if (mouse.LeftButton == ButtonState.Pressed &&
                    _prevMouseState.LeftButton == ButtonState.Released &&
                    _endTurnButtonRect.Contains(mouse.Position))
                {
                    EndTurn();
                }

                _hoveredTile = ScreenToBoard(mouse.Position);

                if (mouse.LeftButton == ButtonState.Pressed &&
                    _prevMouseState.LeftButton == ButtonState.Released &&
                    !_endTurnButtonRect.Contains(mouse.Position) && _hoveredTile.HasValue)
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
                    c.ScreenPos = Vector2.Lerp(c.ScreenPos, BoardToScreen(c.BoardPos), 0.1f);
                    _characters[i] = c;
                }
            }
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

            _effect.View = _viewMatrix;
            _effect.Projection = _projectionMatrix;
            _effect.World = Matrix.Identity;
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
            _spriteBatch.Begin(blendState: BlendState.AlphaBlend);
            _spriteBatch.Draw(_campfireTexture, _campfireScreenPos - new Vector2(32, 64),
                null, Color.White, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
            _spriteBatch.Draw(_lightGradientTexture, _campfireScreenPos - new Vector2(128, 128),
                null, Color.White * flicker, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0f);
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
            _spriteBatch.Begin(blendState: BlendState.AlphaBlend);
            foreach (var c in _characters)
            {
                var tex = c.Texture ?? _pawnTexture;
                _spriteBatch.Draw(tex, c.ScreenPos - new Vector2(32, 64),
                    null, Color.White, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
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

            if (_moving)
            {
                Vector2 center = new Vector2(_graphics.PreferredBackBufferWidth / 2f, _graphics.PreferredBackBufferHeight / 2f);
                _spriteBatch.Begin();
                _spriteBatch.Draw(_spinnerTexture, center, null, Color.White, _spinnerRotation,
                    new Vector2(_spinnerTexture.Width / 2f, _spinnerTexture.Height / 2f), 1f, SpriteEffects.None, 0f);
                _spriteBatch.End();
            }
        }
    }
}
