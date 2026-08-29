using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Invaders.PacMan
{
    public class PacManPlayer
    {
        public Vector2 Position;
        public Point CurrentDir { get; set; } = new Point(-1, 0); // Start moving left
        public Point QueuedDir { get; set; } = new Point(-1, 0);

        public float BaseSpeed { get; set; } = 96f;
        public float SpeedMultiplier { get; set; } = 1.0f;

        public bool IsDead { get; set; }
        public float DeathAnimProgress { get; set; } = 0f;
        public int Lives { get; set; } = 3;

        private float m_MouthAnimTimer;
        private float m_MouthOpenRatio = 0.5f;

        public const float k_Radius = 7.5f;

        public Point TilePos
        {
            get
            {
                float tileSize = PacManMaze.k_TileSize;
                return new Point((int)Math.Floor(Position.X / tileSize), (int)Math.Floor(Position.Y / tileSize));
            }
        }

        public PacManPlayer(Vector2 i_SpawnPos)
        {
            ResetPosition(i_SpawnPos);
        }

        public void ResetPosition(Vector2 i_SpawnPos)
        {
            Position = i_SpawnPos;
            CurrentDir = new Point(-1, 0);
            QueuedDir = new Point(-1, 0);
            IsDead = false;
            DeathAnimProgress = 0f;
            m_MouthAnimTimer = 0f;
        }

        public void Update(float i_Dt, PacManMaze i_Maze)
        {
            if (IsDead)
            {
                DeathAnimProgress = Math.Min(1.0f, DeathAnimProgress + i_Dt * 1.2f);
                return;
            }

            float tileSize = PacManMaze.k_TileSize;
            float speed = BaseSpeed * SpeedMultiplier;

            int col = (int)Math.Floor(Position.X / tileSize);
            int row = (int)Math.Floor(Position.Y / tileSize);
            Vector2 tileCenter = new Vector2(col * tileSize + tileSize / 2f, row * tileSize + tileSize / 2f);

            // 1. Direct 180-degree reversal check (can reverse anytime)
            if (QueuedDir.X == -CurrentDir.X && QueuedDir.Y == -CurrentDir.Y && (QueuedDir.X != 0 || QueuedDir.Y != 0))
            {
                CurrentDir = QueuedDir;
            }

            // 2. Corner turning at tile center
            if (QueuedDir != CurrentDir && (QueuedDir.X != 0 || QueuedDir.Y != 0))
            {
                float distToCenter = Vector2.Distance(Position, tileCenter);
                if (distToCenter <= Math.Max(4.0f, speed * i_Dt * 1.5f))
                {
                    int targetCol = col + QueuedDir.X;
                    int targetRow = row + QueuedDir.Y;

                    if (!i_Maze.IsWall(targetCol, targetRow))
                    {
                        Position = tileCenter;
                        CurrentDir = QueuedDir;
                    }
                }
            }

            // 3. Move forward in CurrentDir
            Vector2 newPos = Position + new Vector2(CurrentDir.X, CurrentDir.Y) * (speed * i_Dt);

            // Wall collision clamping
            if (CurrentDir.X > 0) // Moving Right
            {
                if (i_Maze.IsWall(col + 1, row) && newPos.X >= tileCenter.X)
                {
                    newPos.X = tileCenter.X;
                }
            }
            else if (CurrentDir.X < 0) // Moving Left
            {
                if (i_Maze.IsWall(col - 1, row) && newPos.X <= tileCenter.X)
                {
                    newPos.X = tileCenter.X;
                }
            }
            else if (CurrentDir.Y > 0) // Moving Down
            {
                if (i_Maze.IsWall(col, row + 1) && newPos.Y >= tileCenter.Y)
                {
                    newPos.Y = tileCenter.Y;
                }
            }
            else if (CurrentDir.Y < 0) // Moving Up
            {
                if (i_Maze.IsWall(col, row - 1) && newPos.Y <= tileCenter.Y)
                {
                    newPos.Y = tileCenter.Y;
                }
            }

            Position = newPos;

            // 4. Tunnel Wrapping (Row 14)
            float mazePixelWidth = PacManMaze.k_Cols * tileSize;
            if (Position.X < -tileSize / 2f)
            {
                Position.X = mazePixelWidth + tileSize / 2f;
            }
            else if (Position.X > mazePixelWidth + tileSize / 2f)
            {
                Position.X = -tileSize / 2f;
            }

            // 5. Chomp Mouth Animation
            m_MouthAnimTimer += i_Dt * 14f;
            m_MouthOpenRatio = (float)(Math.Sin(m_MouthAnimTimer) * 0.5 + 0.5);
        }

        public void Draw(SpriteBatch i_Batch, Texture2D i_Pixel, Vector2 i_ScreenCenter)
        {
            Color pacColor = Color.Yellow;

            if (IsDead)
            {
                float angleSpan = DeathAnimProgress * MathHelper.Pi;
                if (DeathAnimProgress < 1.0f)
                {
                    drawPacManPie(i_Batch, i_Pixel, i_ScreenCenter, k_Radius * (1f - DeathAnimProgress * 0.3f), pacColor, 0f, angleSpan);
                }
                return;
            }

            // Facing angle
            float facingAngle = 0f;
            if (CurrentDir.X == 1) facingAngle = 0f;
            else if (CurrentDir.X == -1) facingAngle = MathHelper.Pi;
            else if (CurrentDir.Y == -1) facingAngle = -MathHelper.PiOver2;
            else if (CurrentDir.Y == 1) facingAngle = MathHelper.PiOver2;

            float mouthAngle = m_MouthOpenRatio * 0.75f;
            drawPacManPie(i_Batch, i_Pixel, i_ScreenCenter, k_Radius, pacColor, facingAngle, mouthAngle);
        }

        private void drawPacManPie(SpriteBatch i_Batch, Texture2D i_Pixel, Vector2 i_Center, float i_Radius, Color i_Color, float i_FacingAngle, float i_MouthAngle)
        {
            int r = (int)i_Radius;
            for (int y = -r; y <= r; y++)
            {
                for (int x = -r; x <= r; x++)
                {
                    if (x * x + y * y <= r * r)
                    {
                        float angle = (float)Math.Atan2(y, x);
                        float diff = MathHelper.WrapAngle(angle - i_FacingAngle);

                        if (Math.Abs(diff) >= i_MouthAngle)
                        {
                            i_Batch.Draw(i_Pixel, new Rectangle((int)(i_Center.X + x), (int)(i_Center.Y + y), 1, 1), i_Color);
                        }
                    }
                }
            }
        }
    }
}
