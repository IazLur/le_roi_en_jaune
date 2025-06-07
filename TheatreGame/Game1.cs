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

        private VertexPositionTexture[] _floorVertices;
        private VertexPositionTexture[] _curtainVertices;
        private BasicEffect _effect;
        private Texture2D _floorTexture;
        private Texture2D _curtainTexture;
        private Texture2D _gridTexture;

        private Texture2D _campfireTexture;
        private Texture2D _lightGradientTexture;

        private Texture2D _particleTexture;
        private List<Particle> _lightParticles;
        private List<Particle> _dustParticles;
        private List<Particle> _fireParticles;
        private List<Particle> _smokeParticles;
        private Random _random;
        private float _time;

        private Vector2 _campfireScreenPos;

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

            // Create floor quad
            _floorVertices = new VertexPositionTexture[6];
            _floorVertices[0] = new VertexPositionTexture(new Vector3(-10, 0, -10), new Vector2(0, 0));
            _floorVertices[1] = new VertexPositionTexture(new Vector3(-10, 0, 10), new Vector2(0, 1));
            _floorVertices[2] = new VertexPositionTexture(new Vector3(10, 0, -10), new Vector2(1, 0));
            _floorVertices[3] = new VertexPositionTexture(new Vector3(10, 0, -10), new Vector2(1, 0));
            _floorVertices[4] = new VertexPositionTexture(new Vector3(-10, 0, 10), new Vector2(0, 1));
            _floorVertices[5] = new VertexPositionTexture(new Vector3(10, 0, 10), new Vector2(1, 1));

            // Create curtain quad behind the stage
            _curtainVertices = new VertexPositionTexture[6];
            _curtainVertices[0] = new VertexPositionTexture(new Vector3(-10, 0, -10), new Vector2(0, 1));
            _curtainVertices[1] = new VertexPositionTexture(new Vector3(10, 0, -10), new Vector2(1, 1));
            _curtainVertices[2] = new VertexPositionTexture(new Vector3(-10, 10, -10), new Vector2(0, 0));
            _curtainVertices[3] = new VertexPositionTexture(new Vector3(-10, 10, -10), new Vector2(0, 0));
            _curtainVertices[4] = new VertexPositionTexture(new Vector3(10, 0, -10), new Vector2(1, 1));
            _curtainVertices[5] = new VertexPositionTexture(new Vector3(10, 10, -10), new Vector2(1, 0));

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
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            _effect = new BasicEffect(GraphicsDevice)
            {
                TextureEnabled = true,
                VertexColorEnabled = false
            };

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
            _lightGradientTexture = Texture2D.FromStream(
                GraphicsDevice,
                TitleContainer.OpenStream("Content/light_gradient.png"));

            _particleTexture = new Texture2D(GraphicsDevice, 1, 1);
            _particleTexture.SetData(new[] { Color.White });
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

            DrawCampfire();
            DrawParticles();

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
            _spriteBatch.Draw(_campfireTexture, _campfireScreenPos - new Vector2(32, 48),
                null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
            _spriteBatch.Draw(_lightGradientTexture, _campfireScreenPos - new Vector2(128, 128),
                null, Color.White * flicker, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0f);
            _spriteBatch.End();
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
    }
}
