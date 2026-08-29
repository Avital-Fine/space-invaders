using Microsoft.Xna.Framework;

namespace Invaders.Snake
{
    public enum eFoodType
    {
        Apple,
        GoldenFruit
    }

    public class SnakeFood
    {
        public Point GridPosition { get; set; }
        public eFoodType Type { get; set; }
        public float Timer { get; set; }
        public float MaxTimer { get; set; }
        public int ScoreValue { get; set; }

        public SnakeFood(Point i_Position, eFoodType i_Type, int i_Level)
        {
            GridPosition = i_Position;
            Type = i_Type;

            if (i_Type == eFoodType.GoldenFruit)
            {
                MaxTimer = Timer = 10f;
                ScoreValue = 500 * i_Level;
            }
            else
            {
                MaxTimer = Timer = 0f;
                ScoreValue = 100 * i_Level;
            }
        }
    }
}

