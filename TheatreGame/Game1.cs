using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

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
        private Texture2D _lightGradientTexture;

        private Texture2D _particleTexture;
        private BasicEffect _colorEffect;
        private List<Particle> _lightParticles;
        private List<Particle> _dustParticles;
        private List<Particle> _fireParticles;
        private List<Particle> _smokeParticles;
        private Texture2D _grainTexture;
        private Color[] _grainData;
        private Random _random;
        private float _time;

        private Vector2 _campfireScreenPos;

        private const float CellSize = 2.5f;

        private class Character
        {
            public Point BoardPos;
            public Vector2 ScreenPos;
        }

        private List<Character> _characters;

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
            var heroPos = new Point(5, 4);
            _characters.Add(new Character { BoardPos = heroPos, ScreenPos = Vector2.Zero });
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
            _lightGradientTexture = Texture2D.FromStream(
                GraphicsDevice,
                TitleContainer.OpenStream("Content/light_gradient.png"));

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
            UpdateParticles(gameTime, _lightParticles);
            UpdateParticles(gameTime, _dustParticles);
            UpdateFireParticles(gameTime, _fireParticles, _campfireScreenPos, 10f);
            UpdateFireParticles(gameTime, _smokeParticles, _campfireScreenPos, 20f);

            for (int i = 0; i < _characters.Count; i++)
            {
                var c = _characters[i];
                c.ScreenPos = BoardToScreen(c.BoardPos);
                _characters[i] = c;
            }
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
            DrawCampfire();
            DrawCharacters();
            DrawParticles();

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _spriteBatch.Draw(_grainTexture,
                new Rectangle(0, 0, _graphics.PreferredBackBufferWidth,
                    _graphics.PreferredBackBufferHeight),
                Color.White * 0.05f);
            _spriteBatch.End();

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
            float flicker = 0.8f + (float)_random.NextDouble() * 0.2f;
            _spriteBatch.Begin(blendState: BlendState.AlphaBlend);
            _spriteBatch.Draw(_campfireTexture, _campfireScreenPos - new Vector2(32, 64),
                null, Color.White, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
            _spriteBatch.Draw(_lightGradientTexture, _campfireScreenPos - new Vector2(128, 128),
                null, Color.White * flicker, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0f);
            _spriteBatch.End();
        }

        private Vector2 BoardToScreen(Point boardPos)
        {
            Vector3 world = new Vector3((boardPos.X - 3.5f) * CellSize, 0f,
                (boardPos.Y - 3.5f) * CellSize);
            var screen = GraphicsDevice.Viewport.Project(world,
                _projectionMatrix, _viewMatrix, Matrix.Identity);
            return new Vector2(screen.X, screen.Y);
        }

        private void DrawCharacters()
        {
            _spriteBatch.Begin(blendState: BlendState.AlphaBlend);
            foreach (var c in _characters)
            {
                _spriteBatch.Draw(_pawnTexture, c.ScreenPos - new Vector2(32, 64),
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
    }
}
