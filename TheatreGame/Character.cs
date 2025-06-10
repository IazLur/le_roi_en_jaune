using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace TheatreGame
{
    internal class Character : Entity
    {
        public Queue<Point> Path = new();
        public float MoveProgress;
        public bool IsPlayer;

        public Character(Point pos, Texture2D texture, bool isPlayer)
            : base(pos, texture, 0.5f, false, new Vector2(32, 64))
        {
            IsPlayer = isPlayer;
        }
    }
}
