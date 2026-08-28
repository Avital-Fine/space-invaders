using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Invaders.Tetris
{
    public class Tetromino
    {
        public TetrominoType Type { get; private set; }
        public int RotationIndex { get; private set; } // 0 = 0°, 1 = 90°, 2 = 180°, 3 = 270°
        public Point Position { get; set; } // Grid coordinates (X = col, Y = row)
        public Color Color => TetrominoColors.GetColor(Type);

        // Predefined relative block coordinates for all 7 types across all 4 rotation states: [type][rotation][block, 0..3]
        private static readonly Dictionary<TetrominoType, Point[][]> s_Shapes = initShapes();

        // SRS Wall kick data
        private static readonly Dictionary<string, Point[]> s_StandardKicks = initStandardKicks();
        private static readonly Dictionary<string, Point[]> s_IKicks = initIKicks();

        public Tetromino(TetrominoType i_Type)
        {
            Type = i_Type;
            RotationIndex = 0;
            ResetSpawnPosition();
        }

        public void ResetSpawnPosition()
        {
            // Standard Tetris spawn: column 3 (or 4 for O/I), top rows
            Position = (Type == TetrominoType.O || Type == TetrominoType.I) ? new Point(3, 0) : new Point(3, 0);
            RotationIndex = 0;
        }

        public Point[] GetCurrentBlockPositions()
        {
            Point[] localBlocks = s_Shapes[Type][RotationIndex];
            Point[] globalBlocks = new Point[4];
            for (int i = 0; i < 4; i++)
            {
                globalBlocks[i] = new Point(Position.X + localBlocks[i].X, Position.Y + localBlocks[i].Y);
            }
            return globalBlocks;
        }

        public Point[] GetLocalBlockPositions(int i_Rotation = -1)
        {
            int rot = (i_Rotation >= 0) ? (i_Rotation % 4) : RotationIndex;
            return s_Shapes[Type][rot];
        }

        public int GetNextRotationIndex(bool i_Clockwise)
        {
            return i_Clockwise ? (RotationIndex + 1) % 4 : (RotationIndex + 3) % 4;
        }

        public Point[] GetKicks(int i_FromRotation, int i_ToRotation)
        {
            if (Type == TetrominoType.O)
            {
                return new Point[] { Point.Zero };
            }

            string transition = $"{i_FromRotation}->{i_ToRotation}";
            if (Type == TetrominoType.I)
            {
                if (s_IKicks.TryGetValue(transition, out Point[] kicks))
                {
                    return kicks;
                }
            }
            else
            {
                if (s_StandardKicks.TryGetValue(transition, out Point[] kicks))
                {
                    return kicks;
                }
            }

            return new Point[] { Point.Zero };
        }

        public void SetRotation(int i_RotationIndex)
        {
            RotationIndex = (i_RotationIndex % 4 + 4) % 4;
        }

        public Tetromino Clone()
        {
            Tetromino clone = new Tetromino(Type)
            {
                Position = this.Position,
                RotationIndex = this.RotationIndex
            };
            return clone;
        }

        #region Shape Initialization
        private static Dictionary<TetrominoType, Point[][]> initShapes()
        {
            var shapes = new Dictionary<TetrominoType, Point[][]>();

            // I Piece (4x4 bounding box)
            shapes[TetrominoType.I] = new Point[][]
            {
                new Point[] { new Point(0, 1), new Point(1, 1), new Point(2, 1), new Point(3, 1) }, // 0
                new Point[] { new Point(2, 0), new Point(2, 1), new Point(2, 2), new Point(2, 3) }, // R (90)
                new Point[] { new Point(0, 2), new Point(1, 2), new Point(2, 2), new Point(3, 2) }, // 2 (180)
                new Point[] { new Point(1, 0), new Point(1, 1), new Point(1, 2), new Point(1, 3) }  // L (270)
            };

            // J Piece (3x3 bounding box)
            shapes[TetrominoType.J] = new Point[][]
            {
                new Point[] { new Point(0, 0), new Point(0, 1), new Point(1, 1), new Point(2, 1) },
                new Point[] { new Point(1, 0), new Point(2, 0), new Point(1, 1), new Point(1, 2) },
                new Point[] { new Point(0, 1), new Point(1, 1), new Point(2, 1), new Point(2, 2) },
                new Point[] { new Point(1, 0), new Point(1, 1), new Point(0, 2), new Point(1, 2) }
            };

            // L Piece (3x3 bounding box)
            shapes[TetrominoType.L] = new Point[][]
            {
                new Point[] { new Point(2, 0), new Point(0, 1), new Point(1, 1), new Point(2, 1) },
                new Point[] { new Point(1, 0), new Point(1, 1), new Point(1, 2), new Point(2, 2) },
                new Point[] { new Point(0, 1), new Point(1, 1), new Point(2, 1), new Point(0, 2) },
                new Point[] { new Point(0, 0), new Point(1, 0), new Point(1, 1), new Point(1, 2) }
            };

            // O Piece (2x2 bounding box)
            shapes[TetrominoType.O] = new Point[][]
            {
                new Point[] { new Point(1, 0), new Point(2, 0), new Point(1, 1), new Point(2, 1) },
                new Point[] { new Point(1, 0), new Point(2, 0), new Point(1, 1), new Point(2, 1) },
                new Point[] { new Point(1, 0), new Point(2, 0), new Point(1, 1), new Point(2, 1) },
                new Point[] { new Point(1, 0), new Point(2, 0), new Point(1, 1), new Point(2, 1) }
            };

            // S Piece (3x3 bounding box)
            shapes[TetrominoType.S] = new Point[][]
            {
                new Point[] { new Point(1, 0), new Point(2, 0), new Point(0, 1), new Point(1, 1) },
                new Point[] { new Point(1, 0), new Point(1, 1), new Point(2, 1), new Point(2, 2) },
                new Point[] { new Point(1, 1), new Point(2, 1), new Point(0, 2), new Point(1, 2) },
                new Point[] { new Point(0, 0), new Point(0, 1), new Point(1, 1), new Point(1, 2) }
            };

            // T Piece (3x3 bounding box)
            shapes[TetrominoType.T] = new Point[][]
            {
                new Point[] { new Point(1, 0), new Point(0, 1), new Point(1, 1), new Point(2, 1) },
                new Point[] { new Point(1, 0), new Point(1, 1), new Point(2, 1), new Point(1, 2) },
                new Point[] { new Point(0, 1), new Point(1, 1), new Point(2, 1), new Point(1, 2) },
                new Point[] { new Point(1, 0), new Point(0, 1), new Point(1, 1), new Point(1, 2) }
            };

            // Z Piece (3x3 bounding box)
            shapes[TetrominoType.Z] = new Point[][]
            {
                new Point[] { new Point(0, 0), new Point(1, 0), new Point(1, 1), new Point(2, 1) },
                new Point[] { new Point(2, 0), new Point(1, 1), new Point(2, 1), new Point(1, 2) },
                new Point[] { new Point(0, 1), new Point(1, 1), new Point(1, 2), new Point(2, 2) },
                new Point[] { new Point(1, 0), new Point(0, 1), new Point(1, 1), new Point(0, 2) }
            };

            return shapes;
        }

        private static Dictionary<string, Point[]> initStandardKicks()
        {
            // SRS wall kicks for J, L, S, T, Z
            // Note: In screen grid, Y goes down (+Y is down), so SRS (+Y is up) is inverted for Y:
            return new Dictionary<string, Point[]>
            {
                { "0->1", new Point[] { new Point(0, 0), new Point(-1, 0), new Point(-1, -1), new Point(0, 2), new Point(-1, 2) } },
                { "1->0", new Point[] { new Point(0, 0), new Point(1, 0),  new Point(1, 1),   new Point(0, -2), new Point(1, -2) } },
                { "1->2", new Point[] { new Point(0, 0), new Point(1, 0),  new Point(1, 1),   new Point(0, -2), new Point(1, -2) } },
                { "2->1", new Point[] { new Point(0, 0), new Point(-1, 0), new Point(-1, -1), new Point(0, 2), new Point(-1, 2) } },
                { "2->3", new Point[] { new Point(0, 0), new Point(1, 0),  new Point(1, -1),  new Point(0, 2), new Point(1, 2) } },
                { "3->2", new Point[] { new Point(0, 0), new Point(-1, 0), new Point(-1, 1),  new Point(0, -2), new Point(-1, -2) } },
                { "3->0", new Point[] { new Point(0, 0), new Point(-1, 0), new Point(-1, 1),  new Point(0, -2), new Point(-1, -2) } },
                { "0->3", new Point[] { new Point(0, 0), new Point(1, 0),  new Point(1, -1),  new Point(0, 2), new Point(1, 2) } }
            };
        }

        private static Dictionary<string, Point[]> initIKicks()
        {
            // SRS wall kicks for I-piece
            return new Dictionary<string, Point[]>
            {
                { "0->1", new Point[] { new Point(0, 0), new Point(-2, 0), new Point(1, 0),  new Point(-2, 1),  new Point(1, -2) } },
                { "1->0", new Point[] { new Point(0, 0), new Point(2, 0),  new Point(-1, 0), new Point(2, -1),  new Point(-1, 2) } },
                { "1->2", new Point[] { new Point(0, 0), new Point(-1, 0), new Point(2, 0),  new Point(-1, -2), new Point(2, 1) } },
                { "2->1", new Point[] { new Point(0, 0), new Point(1, 0),  new Point(-2, 0), new Point(1, 2),   new Point(-2, -1) } },
                { "2->3", new Point[] { new Point(0, 0), new Point(2, 0),  new Point(-1, 0), new Point(2, -1),  new Point(-1, 2) } },
                { "3->2", new Point[] { new Point(0, 0), new Point(-2, 0), new Point(1, 0),  new Point(-2, 1),  new Point(1, -2) } },
                { "3->0", new Point[] { new Point(0, 0), new Point(1, 0),  new Point(-2, 0), new Point(1, 2),   new Point(-2, -1) } },
                { "0->3", new Point[] { new Point(0, 0), new Point(-1, 0), new Point(2, 0),  new Point(-1, -2), new Point(2, 1) } }
            };
        }
        #endregion
    }
}

