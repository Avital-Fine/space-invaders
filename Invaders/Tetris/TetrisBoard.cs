using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Invaders.Tetris
{
    public class TetrisBoard
    {
        public const int Columns = 10;
        public const int Rows = 20;

        // Grid storage: [col, row]. null means empty cell, otherwise contains block color
        private readonly Color?[,] m_Grid = new Color?[Columns, Rows];

        public Color?[,] Grid => m_Grid;

        public void Clear()
        {
            for (int x = 0; x < Columns; x++)
            {
                for (int y = 0; y < Rows; y++)
                {
                    m_Grid[x, y] = null;
                }
            }
        }

        public bool IsValidPosition(Tetromino i_Piece, Point i_Position, int i_Rotation)
        {
            Point[] localBlocks = i_Piece.GetLocalBlockPositions(i_Rotation);
            for (int i = 0; i < 4; i++)
            {
                int x = i_Position.X + localBlocks[i].X;
                int y = i_Position.Y + localBlocks[i].Y;

                // Horizontal bounds check
                if (x < 0 || x >= Columns)
                {
                    return false;
                }

                // Bottom bound check
                if (y >= Rows)
                {
                    return false;
                }

                // Grid occupancy check (blocks above ceiling y < 0 are allowed in buffer)
                if (y >= 0 && m_Grid[x, y] != null)
                {
                    return false;
                }
            }

            return true;
        }

        public bool TryMove(Tetromino i_Piece, Point i_Offset)
        {
            Point newPos = new Point(i_Piece.Position.X + i_Offset.X, i_Piece.Position.Y + i_Offset.Y);
            if (IsValidPosition(i_Piece, newPos, i_Piece.RotationIndex))
            {
                i_Piece.Position = newPos;
                return true;
            }

            return false;
        }

        public bool TryRotate(Tetromino i_Piece, bool i_Clockwise)
        {
            int fromRot = i_Piece.RotationIndex;
            int toRot = i_Piece.GetNextRotationIndex(i_Clockwise);
            Point[] kicks = i_Piece.GetKicks(fromRot, toRot);

            foreach (Point kick in kicks)
            {
                Point testPos = new Point(i_Piece.Position.X + kick.X, i_Piece.Position.Y + kick.Y);
                if (IsValidPosition(i_Piece, testPos, toRot))
                {
                    i_Piece.Position = testPos;
                    i_Piece.SetRotation(toRot);
                    return true;
                }
            }

            return false;
        }

        public Point GetGhostPosition(Tetromino i_Piece)
        {
            Point ghostPos = i_Piece.Position;
            while (IsValidPosition(i_Piece, new Point(ghostPos.X, ghostPos.Y + 1), i_Piece.RotationIndex))
            {
                ghostPos.Y++;
            }
            return ghostPos;
        }

        public bool LockPiece(Tetromino i_Piece)
        {
            Point[] blocks = i_Piece.GetCurrentBlockPositions();
            bool lockedAboveVisibleGrid = true;

            for (int i = 0; i < blocks.Length; i++)
            {
                int x = blocks[i].X;
                int y = blocks[i].Y;

                if (y >= 0 && y < Rows && x >= 0 && x < Columns)
                {
                    m_Grid[x, y] = i_Piece.Color;
                    lockedAboveVisibleGrid = false;
                }
            }

            // Top-out condition: piece locked entirely above row 0
            return lockedAboveVisibleGrid;
        }

        public List<int> FindFullLines()
        {
            List<int> fullLines = new List<int>();

            for (int y = 0; y < Rows; y++)
            {
                bool isFull = true;
                for (int x = 0; x < Columns; x++)
                {
                    if (m_Grid[x, y] == null)
                    {
                        isFull = false;
                        break;
                    }
                }

                if (isFull)
                {
                    fullLines.Add(y);
                }
            }

            return fullLines;
        }

        public void ClearLines(List<int> i_LineIndices)
        {
            if (i_LineIndices == null || i_LineIndices.Count == 0)
            {
                return;
            }

            // Process lines from top to bottom
            i_LineIndices.Sort();

            foreach (int lineY in i_LineIndices)
            {
                // Shift all rows above down by 1
                for (int y = lineY; y > 0; y--)
                {
                    for (int x = 0; x < Columns; x++)
                    {
                        m_Grid[x, y] = m_Grid[x, y - 1];
                    }
                }

                // Clear top row
                for (int x = 0; x < Columns; x++)
                {
                    m_Grid[x, 0] = null;
                }
            }
        }
    }
}

