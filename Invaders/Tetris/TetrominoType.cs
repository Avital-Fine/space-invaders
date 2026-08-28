using Microsoft.Xna.Framework;

namespace Invaders.Tetris
{
    public enum TetrominoType
    {
        I,
        J,
        L,
        O,
        S,
        T,
        Z
    }

    public static class TetrominoColors
    {
        public static readonly Color Cyan   = new Color(0, 229, 255);    // I piece
        public static readonly Color Blue   = new Color(41, 121, 255);   // J piece
        public static readonly Color Orange = new Color(255, 145, 0);    // L piece
        public static readonly Color Yellow = new Color(255, 234, 0);    // O piece
        public static readonly Color Green  = new Color(0, 230, 118);    // S piece
        public static readonly Color Purple = new Color(213, 0, 249);    // T piece
        public static readonly Color Red    = new Color(255, 23, 68);     // Z piece

        public static Color GetColor(TetrominoType i_Type)
        {
            switch (i_Type)
            {
                case TetrominoType.I: return Cyan;
                case TetrominoType.J: return Blue;
                case TetrominoType.L: return Orange;
                case TetrominoType.O: return Yellow;
                case TetrominoType.S: return Green;
                case TetrominoType.T: return Purple;
                case TetrominoType.Z: return Red;
                default: return Color.White;
            }
        }
    }
}

