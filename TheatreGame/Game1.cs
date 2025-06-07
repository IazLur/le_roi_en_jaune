using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

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
        }

        protected override void Update(GameTime gameTime)
        {
            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

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

            _effect.Texture = _curtainTexture;
            foreach (var pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, _curtainVertices, 0, 2);
            }

            base.Draw(gameTime);
        }
    }
}
