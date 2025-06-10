using Microsoft.Xna.Framework;

namespace TheatreGame
{
    internal class FogSystem
    {
        private readonly bool[,] _fog = new bool[BoardUtils.Size, BoardUtils.Size];

        public bool IsVisible(Point tile)
        {
            if (tile.X < 0 || tile.X >= BoardUtils.Size || tile.Y < 0 || tile.Y >= BoardUtils.Size)
                return true;
            return !_fog[tile.X, tile.Y];
        }

        public void SetAll(bool value)
        {
            for (int x = 0; x < BoardUtils.Size; x++)
                for (int y = 0; y < BoardUtils.Size; y++)
                    _fog[x, y] = value;
        }

        public void ClearAround(Point center, int radius)
        {
            for (int x = 0; x < BoardUtils.Size; x++)
            {
                for (int y = 0; y < BoardUtils.Size; y++)
                {
                    int dx = System.Math.Abs(x - center.X);
                    int dy = System.Math.Abs(y - center.Y);
                    if (dx + dy <= radius)
                        _fog[x, y] = false;
                }
            }
        }
    }
}
