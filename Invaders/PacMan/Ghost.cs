using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Invaders.PacMan
{
    public enum eGhostType
    {
        Blinky, // Red
        Pinky,  // Pink
        Inky,   // Cyan
        Clyde   // Orange
    }

    public enum eGhostState
    {
        InHouse,
        LeavingHouse,
        Chase,
        Scatter,
        Frightened,
        EatenEyes
    }

    public class Ghost
    {
        public eGhostType Type { get; }
        public eGhostState State { get; set; }
        public Vector2 Position;
        public Point CurrentDir { get; set; }
        public Point TargetTile { get; set; }

        public Color BodyColor { get; }
        public float HouseReleaseTimer { get; set; }
        public float Speed { get; set; } = 84f;

        private float m_SkirtAnimTimer;
        private Point m_LastTurnTile = new Point(-1, -1);
        private readonly Random m_Random = new Random();

        public const float k_Radius = 7.5f;
        public Point TilePos => new Point((int)Math.Floor(Position.X / PacManMaze.k_TileSize), (int)Math.Floor(Position.Y / PacManMaze.k_TileSize));

        public Point ScatterTarget => Type switch
        {
            eGhostType.Blinky => new Point(25, -2), // Top-Right
            eGhostType.Pinky  => new Point(2, -2),  // Top-Left
            eGhostType.Inky   => new Point(27, 31), // Bottom-Right
            eGhostType.Clyde  => new Point(0, 31),  // Bottom-Left
            _ => new Point(0, 0)
        };

        public Ghost(eGhostType i_Type, Vector2 i_SpawnPos, float i_ReleaseDelay)
        {
            Type = i_Type;
            BodyColor = Type switch
            {
                eGhostType.Blinky => new Color(255, 0, 0),      // Red
                eGhostType.Pinky  => new Color(255, 184, 255),  // Pink
                eGhostType.Inky   => new Color(0, 255, 255),    // Cyan
                eGhostType.Clyde  => new Color(255, 184, 82),   // Orange
                _ => Color.White
            };

            Reset(i_SpawnPos, i_ReleaseDelay);
        }

        public void Reset(Vector2 i_SpawnPos, float i_ReleaseDelay)
        {
            Position = i_SpawnPos;
            HouseReleaseTimer = i_ReleaseDelay;
            State = i_ReleaseDelay > 0 ? eGhostState.InHouse : eGhostState.Scatter;
            CurrentDir = new Point(-1, 0);
            TargetTile = ScatterTarget;
            m_LastTurnTile = new Point(-1, -1);
        }

        public void Update(float i_Dt, PacManMaze i_Maze, PacManPlayer i_PacMan, Ghost i_Blinky, eGhostState i_GlobalState, float i_FrightenedTimer)
        {
            m_SkirtAnimTimer += i_Dt * 8f;
            float tileSize = PacManMaze.k_TileSize;

            // 1. In House state: bob up and down
            if (State == eGhostState.InHouse)
            {
                HouseReleaseTimer -= i_Dt;
                Position.Y += (float)Math.Sin(m_SkirtAnimTimer) * 12f * i_Dt;

                if (HouseReleaseTimer <= 0)
                {
                    State = eGhostState.LeavingHouse;
                }
                return;
            }

            // 2. Leaving House state: navigate to door gate
            if (State == eGhostState.LeavingHouse)
            {
                Vector2 gatePos = new Vector2(13f * tileSize + tileSize / 2f, 11f * tileSize + tileSize / 2f);
                if (Math.Abs(Position.X - gatePos.X) > 1.5f)
                {
                    Position.X = MathHelper.Lerp(Position.X, gatePos.X, 10f * i_Dt);
                }
                else if (Position.Y > gatePos.Y)
                {
                    Position.X = gatePos.X;
                    Position.Y -= 60f * i_Dt;
                }
                else
                {
                    Position = gatePos;
                    CurrentDir = new Point(-1, 0);
                    State = (i_FrightenedTimer > 0) ? eGhostState.Frightened : i_GlobalState;
                    m_LastTurnTile = new Point(13, 11);
                }
                return;
            }

            // 3. Eaten Eyes returning to house
            if (State == eGhostState.EatenEyes)
            {
                Vector2 gatePos = new Vector2(13f * tileSize + tileSize / 2f, 11f * tileSize + tileSize / 2f);
                Vector2 houseCenter = new Vector2(13f * tileSize + tileSize / 2f, 14f * tileSize + tileSize / 2f);

                if (Vector2.Distance(Position, gatePos) < 8f)
                {
                    Position.Y += 80f * i_Dt;
                    if (Position.Y >= houseCenter.Y)
                    {
                        State = eGhostState.LeavingHouse;
                    }
                    return;
                }
                else
                {
                    TargetTile = new Point(13, 11);
                }
            }
            else if (State == eGhostState.Frightened)
            {
                // Frightened
            }
            else
            {
                State = i_GlobalState;

                if (State == eGhostState.Chase)
                {
                    computeChaseTarget(i_PacMan, i_Blinky);
                }
                else
                {
                    TargetTile = ScatterTarget;
                }
            }

            // 4. Move along grid with proper tile center alignment
            float currentSpeed = State switch
            {
                eGhostState.EatenEyes  => 160f,
                eGhostState.Frightened => 48f,
                _ => Speed
            };

            int col = (int)Math.Floor(Position.X / tileSize);
            int row = (int)Math.Floor(Position.Y / tileSize);
            Vector2 tileCenter = new Vector2(col * tileSize + tileSize / 2f, row * tileSize + tileSize / 2f);

            Point currentTile = new Point(col, row);
            float distToCenter = Vector2.Distance(Position, tileCenter);

            if (distToCenter <= Math.Max(5.0f, currentSpeed * i_Dt * 1.5f) && currentTile != m_LastTurnTile)
            {
                m_LastTurnTile = currentTile;
                Position = tileCenter;
                chooseNextDirection(col, row, i_Maze);
            }

            // Move forward
            Vector2 step = new Vector2(CurrentDir.X, CurrentDir.Y) * (currentSpeed * i_Dt);
            Vector2 newPos = Position + step;

            // Stop at walls
            bool isEyes = (State == eGhostState.EatenEyes);
            if (CurrentDir.X > 0 && !i_Maze.IsPassableForGhost(col + 1, row, isEyes, false) && newPos.X >= tileCenter.X)
            {
                newPos.X = tileCenter.X;
                chooseNextDirection(col, row, i_Maze);
            }
            else if (CurrentDir.X < 0 && !i_Maze.IsPassableForGhost(col - 1, row, isEyes, false) && newPos.X <= tileCenter.X)
            {
                newPos.X = tileCenter.X;
                chooseNextDirection(col, row, i_Maze);
            }
            else if (CurrentDir.Y > 0 && !i_Maze.IsPassableForGhost(col, row + 1, isEyes, false) && newPos.Y >= tileCenter.Y)
            {
                newPos.Y = tileCenter.Y;
                chooseNextDirection(col, row, i_Maze);
            }
            else if (CurrentDir.Y < 0 && !i_Maze.IsPassableForGhost(col, row - 1, isEyes, false) && newPos.Y <= tileCenter.Y)
            {
                newPos.Y = tileCenter.Y;
                chooseNextDirection(col, row, i_Maze);
            }

            Position = newPos;

            // 5. Tunnel Wrap (Row 14)
            float mazePixelWidth = PacManMaze.k_Cols * tileSize;
            if (Position.X < -tileSize / 2f)
            {
                Position.X = mazePixelWidth + tileSize / 2f;
            }
            else if (Position.X > mazePixelWidth + tileSize / 2f)
            {
                Position.X = -tileSize / 2f;
            }
        }

        private void computeChaseTarget(PacManPlayer i_PacMan, Ghost i_Blinky)
        {
            Point pacTile = i_PacMan.TilePos;
            Point pacDir = i_PacMan.CurrentDir;

            switch (Type)
            {
                case eGhostType.Blinky:
                    TargetTile = pacTile;
                    break;

                case eGhostType.Pinky:
                    TargetTile = new Point(pacTile.X + pacDir.X * 4, pacTile.Y + pacDir.Y * 4);
                    break;

                case eGhostType.Inky:
                    Point pivot = new Point(pacTile.X + pacDir.X * 2, pacTile.Y + pacDir.Y * 2);
                    Point blinkyTile = i_Blinky != null ? i_Blinky.TilePos : pacTile;
                    TargetTile = new Point(pivot.X + (pivot.X - blinkyTile.X), pivot.Y + (pivot.Y - blinkyTile.Y));
                    break;

                case eGhostType.Clyde:
                    float dist = Vector2.Distance(new Vector2(TilePos.X, TilePos.Y), new Vector2(pacTile.X, pacTile.Y));
                    TargetTile = (dist > 8f) ? pacTile : ScatterTarget;
                    break;
            }
        }

        private void chooseNextDirection(int i_Col, int i_Row, PacManMaze i_Maze)
        {
            Point[] candidates = new Point[]
            {
                new Point(0, -1), // Up
                new Point(-1, 0), // Left
                new Point(0, 1),  // Down
                new Point(1, 0)   // Right
            };

            List<Point> validDirs = new List<Point>();
            bool isEyes = (State == eGhostState.EatenEyes);

            foreach (var dir in candidates)
            {
                // Never reverse directly unless no other option
                if (dir.X == -CurrentDir.X && dir.Y == -CurrentDir.Y && (CurrentDir.X != 0 || CurrentDir.Y != 0))
                {
                    continue;
                }

                int nextCol = i_Col + dir.X;
                int nextRow = i_Row + dir.Y;

                if (i_Maze.IsPassableForGhost(nextCol, nextRow, isEyes, false))
                {
                    validDirs.Add(dir);
                }
            }

            if (validDirs.Count == 0)
            {
                CurrentDir = new Point(-CurrentDir.X, -CurrentDir.Y);
                return;
            }

            if (State == eGhostState.Frightened)
            {
                CurrentDir = validDirs[m_Random.Next(validDirs.Count)];
                return;
            }

            Point bestDir = validDirs[0];
            float bestDistSq = float.MaxValue;

            foreach (var dir in validDirs)
            {
                int nextCol = i_Col + dir.X;
                int nextRow = i_Row + dir.Y;

                float dx = nextCol - TargetTile.X;
                float dy = nextRow - TargetTile.Y;
                float distSq = dx * dx + dy * dy;

                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    bestDir = dir;
                }
            }

            CurrentDir = bestDir;
        }

        public void Draw(SpriteBatch i_Batch, Texture2D i_Pixel, Vector2 i_ScreenCenter, float i_FrightenedTimer)
        {
            int r = (int)k_Radius;

            if (State == eGhostState.EatenEyes)
            {
                drawEyes(i_Batch, i_Pixel, i_ScreenCenter);
                return;
            }

            Color bodyColor;
            if (State == eGhostState.Frightened)
            {
                bool isFlashingWhite = (i_FrightenedTimer <= 2.2f) && ((int)(i_FrightenedTimer * 5f) % 2 == 1);
                bodyColor = isFlashingWhite ? Color.White : new Color(36, 36, 255);
            }
            else
            {
                bodyColor = BodyColor;
            }

            // Dome Head
            for (int y = -r; y <= 0; y++)
            {
                for (int x = -r; x <= r; x++)
                {
                    if (x * x + y * y <= r * r)
                    {
                        i_Batch.Draw(i_Pixel, new Rectangle((int)(i_ScreenCenter.X + x), (int)(i_ScreenCenter.Y + y), 1, 1), bodyColor);
                    }
                }
            }

            // Torso & Skirt
            int waveOffset = ((int)(m_SkirtAnimTimer * 2f) % 2 == 0) ? 0 : 2;
            for (int y = 0; y <= r + 1; y++)
            {
                for (int x = -r; x <= r; x++)
                {
                    if (y >= r && (x + r + waveOffset) % 4 == 0)
                    {
                        continue;
                    }
                    i_Batch.Draw(i_Pixel, new Rectangle((int)(i_ScreenCenter.X + x), (int)(i_ScreenCenter.Y + y), 1, 1), bodyColor);
                }
            }

            // Face
            if (State == eGhostState.Frightened)
            {
                Color mouthColor = (bodyColor == Color.White) ? Color.Red : new Color(255, 184, 174);
                i_Batch.Draw(i_Pixel, new Rectangle((int)(i_ScreenCenter.X - 4), (int)(i_ScreenCenter.Y - 3), 2, 2), mouthColor);
                i_Batch.Draw(i_Pixel, new Rectangle((int)(i_ScreenCenter.X + 2), (int)(i_ScreenCenter.Y - 3), 2, 2), mouthColor);
                i_Batch.Draw(i_Pixel, new Rectangle((int)(i_ScreenCenter.X - 5), (int)(i_ScreenCenter.Y + 3), 10, 2), mouthColor);
            }
            else
            {
                drawEyes(i_Batch, i_Pixel, i_ScreenCenter);
            }
        }

        private void drawEyes(SpriteBatch i_Batch, Texture2D i_Pixel, Vector2 i_ScreenCenter)
        {
            int eyeOffsetX = CurrentDir.X * 2;
            int eyeOffsetY = CurrentDir.Y * 2;

            Rectangle leftEye = new Rectangle((int)(i_ScreenCenter.X - 5 + eyeOffsetX), (int)(i_ScreenCenter.Y - 4 + eyeOffsetY), 4, 5);
            Rectangle rightEye = new Rectangle((int)(i_ScreenCenter.X + 1 + eyeOffsetX), (int)(i_ScreenCenter.Y - 4 + eyeOffsetY), 4, 5);

            i_Batch.Draw(i_Pixel, leftEye, Color.White);
            i_Batch.Draw(i_Pixel, rightEye, Color.White);

            int pupilX = CurrentDir.X;
            int pupilY = CurrentDir.Y;
            i_Batch.Draw(i_Pixel, new Rectangle(leftEye.X + 1 + pupilX, leftEye.Y + 1 + pupilY, 2, 2), new Color(12, 12, 180));
            i_Batch.Draw(i_Pixel, new Rectangle(rightEye.X + 1 + pupilX, rightEye.Y + 1 + pupilY, 2, 2), new Color(12, 12, 180));
        }
    }
}
