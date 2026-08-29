using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Invaders.PacMan
{
    public class PacManMaze
    {
        public const int k_Cols = 28;
        public const int k_Rows = 31;
        public const int k_TileSize = 18;

        public const int Tile_Empty = 0;
        public const int Tile_Wall = 1;
        public const int Tile_Dot = 2;
        public const int Tile_Energizer = 3;
        public const int Tile_Gate = 4;
        public const int Tile_GhostHouse = 5;

        private readonly int[,] m_Grid = new int[k_Rows, k_Cols];
        private readonly int[,] m_OriginalGrid = new int[k_Rows, k_Cols];

        public int TotalDots { get; private set; }
        public int DotsRemaining { get; private set; }

        private static readonly string[] s_ClassicMazeLayout = new string[]
        {
            "############################", // 0
            "#............##............#", // 1
            "#.####.#####.##.#####.####.#", // 2
            "#o####.#####.##.#####.####o#", // 3
            "#.####.#####.##.#####.####.#", // 4
            "#..........................#", // 5
            "#.####.##.########.##.####.#", // 6
            "#.####.##.########.##.####.#", // 7
            "#......##....##....##......#", // 8
            "######.##### ## #####.######", // 9
            "     #.##### ## #####.#     ", // 10
            "     #.##          ##.#     ", // 11
            "     #.## ###--### ##.#     ", // 12
            "######.## #gggg# ##.######", // 13
            "      .   #gggg#   .      ", // 14 (Tunnel)
            "######.## #gggg# ##.######", // 15
            "     #.## ######## ##.#     ", // 16
            "     #.##          ##.#     ", // 17
            "     #.## ######## ##.#     ", // 18
            "######.## ######## ##.######", // 19
            "#............##............#", // 20
            "#.####.#####.##.#####.####.#", // 21
            "#.####.#####.##.#####.####.#", // 22
            "#o..##................##..o#", // 23
            "###.##.##.########.##.##.###", // 24
            "###.##.##.########.##.##.###", // 25
            "#......##....##....##......#", // 26
            "#.##########.##.##########.#", // 27
            "#.##########.##.##########.#", // 28
            "#..........................#", // 29
            "############################"  // 30
        };

        public PacManMaze()
        {
            loadMazeFromLayout();
        }

        private void loadMazeFromLayout()
        {
            TotalDots = 0;

            for (int r = 0; r < k_Rows; r++)
            {
                string rowStr = s_ClassicMazeLayout[r];
                for (int c = 0; c < k_Cols; c++)
                {
                    char ch = c < rowStr.Length ? rowStr[c] : ' ';
                    int tile = Tile_Empty;

                    switch (ch)
                    {
                        case '#':
                            tile = Tile_Wall;
                            break;
                        case '.':
                            tile = Tile_Dot;
                            TotalDots++;
                            break;
                        case 'o':
                            tile = Tile_Energizer;
                            TotalDots++;
                            break;
                        case '-':
                            tile = Tile_Gate;
                            break;
                        case 'g':
                            tile = Tile_GhostHouse;
                            break;
                        default:
                            tile = Tile_Empty;
                            break;
                    }

                    m_Grid[r, c] = tile;
                    m_OriginalGrid[r, c] = tile;
                }
            }

            DotsRemaining = TotalDots;
        }

        public void ResetDots()
        {
            DotsRemaining = 0;
            for (int r = 0; r < k_Rows; r++)
            {
                for (int c = 0; c < k_Cols; c++)
                {
                    m_Grid[r, c] = m_OriginalGrid[r, c];
                    if (m_Grid[r, c] == Tile_Dot || m_Grid[r, c] == Tile_Energizer)
                    {
                        DotsRemaining++;
                    }
                }
            }
        }

        public bool IsWall(int i_Col, int i_Row)
        {
            if (i_Row < 0 || i_Row >= k_Rows) return true;
            if (i_Col < 0 || i_Col >= k_Cols)
            {
                // Tunnel is open horizontally on row 14
                if (i_Row == 14) return false;
                return true;
            }

            int tile = m_Grid[i_Row, i_Col];
            return tile == Tile_Wall || tile == Tile_Gate;
        }

        public bool IsPassableForGhost(int i_Col, int i_Row, bool i_IsEatenEyes, bool i_IsLeavingHouse)
        {
            if (i_Row < 0 || i_Row >= k_Rows) return false;
            if (i_Col < 0 || i_Col >= k_Cols)
            {
                // Tunnel
                return i_Row == 14;
            }

            int tile = m_Grid[i_Row, i_Col];
            if (tile == Tile_Wall) return false;

            if (tile == Tile_Gate)
            {
                return i_IsEatenEyes || i_IsLeavingHouse;
            }

            if (tile == Tile_GhostHouse)
            {
                return i_IsEatenEyes || i_IsLeavingHouse;
            }

            return true;
        }

        public int EatDot(int i_Col, int i_Row)
        {
            if (i_Row < 0 || i_Row >= k_Rows || i_Col < 0 || i_Col >= k_Cols) return 0;

            int tile = m_Grid[i_Row, i_Col];
            if (tile == Tile_Dot)
            {
                m_Grid[i_Row, i_Col] = Tile_Empty;
                DotsRemaining--;
                return 10;
            }
            else if (tile == Tile_Energizer)
            {
                m_Grid[i_Row, i_Col] = Tile_Empty;
                DotsRemaining--;
                return 50;
            }

            return 0;
        }

        public int GetTile(int i_Col, int i_Row)
        {
            if (i_Row < 0 || i_Row >= k_Rows || i_Col < 0 || i_Col >= k_Cols) return Tile_Wall;
            return m_Grid[i_Row, i_Col];
        }

        public void Draw(SpriteBatch i_Batch, Texture2D i_Pixel, int i_ArenaX, int i_ArenaY, bool i_EnergizerVisible, bool i_FlashWhite)
        {
            Color wallColor = i_FlashWhite ? Color.White : new Color(33, 33, 255);        // Classic blue neon
            Color innerWallColor = i_FlashWhite ? new Color(200, 200, 255) : new Color(15, 15, 120);
            Color dotColor = new Color(255, 183, 174);                                    // Classic peach dot
            Color gateColor = new Color(255, 182, 220);                                   // Pink gate

            for (int r = 0; r < k_Rows; r++)
            {
                for (int c = 0; c < k_Cols; c++)
                {
                    int tile = m_Grid[r, c];
                    Rectangle tileRect = new Rectangle(i_ArenaX + c * k_TileSize, i_ArenaY + r * k_TileSize, k_TileSize, k_TileSize);

                    if (tile == Tile_Wall)
                    {
                        // Outer wall border
                        i_Batch.Draw(i_Pixel, tileRect, wallColor);

                        // Subtle inner tile bevel
                        Rectangle inner = new Rectangle(tileRect.X + 2, tileRect.Y + 2, tileRect.Width - 4, tileRect.Height - 4);
                        i_Batch.Draw(i_Pixel, inner, innerWallColor);
                    }
                    else if (tile == Tile_Gate)
                    {
                        // Ghost house door
                        Rectangle gateRect = new Rectangle(tileRect.X, tileRect.Y + tileRect.Height / 2 - 2, tileRect.Width, 4);
                        i_Batch.Draw(i_Pixel, gateRect, gateColor);
                    }
                    else if (tile == Tile_Dot)
                    {
                        // 4x4 dot centered in tile
                        Rectangle dotRect = new Rectangle(tileRect.Center.X - 2, tileRect.Center.Y - 2, 4, 4);
                        i_Batch.Draw(i_Pixel, dotRect, dotColor);
                    }
                    else if (tile == Tile_Energizer && i_EnergizerVisible)
                    {
                        // 10x10 pulsating power pellet
                        Rectangle energizerRect = new Rectangle(tileRect.Center.X - 5, tileRect.Center.Y - 5, 10, 10);
                        i_Batch.Draw(i_Pixel, energizerRect, dotColor);
                        // Center shine
                        i_Batch.Draw(i_Pixel, new Rectangle(energizerRect.X + 2, energizerRect.Y + 2, 4, 4), Color.White);
                    }
                }
            }
        }
    }
}

