using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace TheatreGame
{
    internal class Entity
    {
        public Point BoardPos;
        public Vector2 ScreenPos;
        public Texture2D Texture;
        public float BaseScale;
        public bool IsLightSource;
        public Vector2 Origin;
        public List<Particle> Particles = new();

        public Entity(Point pos, Texture2D texture, float baseScale = 0.5f, bool isLight = false, Vector2? origin = null)
        {
            BoardPos = pos;
            Texture = texture;
            BaseScale = baseScale;
            IsLightSource = isLight;
            Origin = origin ?? new Vector2(32, 64);
        }
    }
}
