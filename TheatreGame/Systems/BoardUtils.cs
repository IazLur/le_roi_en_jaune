using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace TheatreGame
{
    internal static class BoardUtils
    {
        public const int Size = 8;
        public const float CellSize = 2.5f;

        public static Vector2 BoardToScreen(Point boardPos, GraphicsDevice device, Matrix projection, Matrix view)
        {
            return BoardToScreen(new Vector2(boardPos.X, boardPos.Y), device, projection, view);
        }

        public static Vector2 BoardToScreen(Vector2 boardPos, GraphicsDevice device, Matrix projection, Matrix view)
        {
            Vector3 world = new Vector3((boardPos.X - 3.5f) * CellSize, 0f, (boardPos.Y - 3.5f) * CellSize);
            var screen = device.Viewport.Project(world, projection, view, Matrix.Identity);
            return new Vector2(screen.X, screen.Y);
        }

        public static Vector3 BoardToWorld(Vector2 boardPos)
        {
            return new Vector3((boardPos.X - 3.5f) * CellSize, 0f, (boardPos.Y - 3.5f) * CellSize);
        }

        public static float GetScaleForWorldPosition(Vector3 worldPos, float baseScale, Vector3 cameraPosition, float initialCameraDistance)
        {
            float distance = Vector3.Distance(cameraPosition, worldPos);
            if (distance <= 0.001f)
                return baseScale;
            return baseScale * (initialCameraDistance / distance);
        }

        public static Point? ScreenToBoard(Point screen, GraphicsDevice device, Matrix projection, Matrix view)
        {
            Vector3 nearSource = new Vector3(screen.X, screen.Y, 0f);
            Vector3 farSource = new Vector3(screen.X, screen.Y, 1f);
            Vector3 nearPoint = device.Viewport.Unproject(nearSource, projection, view, Matrix.Identity);
            Vector3 farPoint = device.Viewport.Unproject(farSource, projection, view, Matrix.Identity);
            Vector3 direction = Vector3.Normalize(farPoint - nearPoint);
            if (direction.Y >= 0f)
                return null;
            float distance = -nearPoint.Y / direction.Y;
            Vector3 world = nearPoint + direction * distance;
            int x = (int)Math.Floor(world.X / CellSize + 4f);
            int y = (int)Math.Floor(world.Z / CellSize + 4f);
            if (x < 0 || x >= Size || y < 0 || y >= Size)
                return null;
            return new Point(x, y);
        }

        public static List<Point> FindPath(Point start, Point goal, Func<int, int, bool> isOccupied)
        {
            if (isOccupied(goal.X, goal.Y))
                return null;

            Queue<Point> q = new();
            q.Enqueue(start);
            Dictionary<Point, Point> prev = new();
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
                    if (nx < 0 || nx >= Size || ny < 0 || ny >= Size)
                        continue;
                    if (isOccupied(nx, ny) && !(nx == goal.X && ny == goal.Y))
                        continue;
                    Point np = new(nx, ny);
                    if (!prev.ContainsKey(np))
                    {
                        prev[np] = cur;
                        q.Enqueue(np);
                    }
                }
            }

            if (!prev.ContainsKey(goal))
                return null;

            List<Point> path = new();
            Point p = goal;
            while (p != start)
            {
                path.Insert(0, p);
                p = prev[p];
            }
            return path;
        }
    }
}
