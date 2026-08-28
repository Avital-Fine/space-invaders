using System;
using Microsoft.Xna.Framework;

namespace Invaders.IcyTower
{
    public class IcyTowerPlatform
    {
        public int FloorIndex { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public Color BaseColor { get; set; }
        public Color BevelLightColor { get; set; }
        public Color BevelDarkColor { get; set; }
        public string ThemeName { get; set; }

        public const float k_DefaultHeight = 16f;

        public IcyTowerPlatform(int i_FloorIndex, float i_X, float i_Y, float i_Width, float i_Height = k_DefaultHeight)
        {
            FloorIndex = i_FloorIndex;
            X = i_X;
            Y = i_Y;
            Width = i_Width;
            Height = i_Height;

            applyTheme(i_FloorIndex);
        }

        public Rectangle Bounds => new Rectangle((int)X, (int)Y, (int)Width, (int)Height);

        private void applyTheme(int i_FloorIndex)
        {
            int segment = (i_FloorIndex / 50) % 5;
            switch (segment)
            {
                case 0: // Ice & Frost (0-49)
                    ThemeName = "Ice & Frost";
                    BaseColor = new Color(80, 190, 240);
                    BevelLightColor = new Color(190, 245, 255);
                    BevelDarkColor = new Color(30, 110, 170);
                    break;
                case 1: // Ancient Stone (50-99)
                    ThemeName = "Ancient Stone";
                    BaseColor = new Color(130, 140, 155);
                    BevelLightColor = new Color(200, 210, 220);
                    BevelDarkColor = new Color(65, 75, 90);
                    break;
                case 2: // Mystic Wood (100-149)
                    ThemeName = "Mystic Wood";
                    BaseColor = new Color(180, 110, 55);
                    BevelLightColor = new Color(230, 170, 110);
                    BevelDarkColor = new Color(100, 55, 25);
                    break;
                case 3: // Metallic Forge (150-199)
                    ThemeName = "Metallic Forge";
                    BaseColor = new Color(210, 140, 40);
                    BevelLightColor = new Color(255, 210, 100);
                    BevelDarkColor = new Color(130, 75, 15);
                    break;
                default: // Neon Rainbow (200+)
                    ThemeName = "Rainbow Palace";
                    BaseColor = new Color(175, 60, 230);
                    BevelLightColor = new Color(235, 150, 255);
                    BevelDarkColor = new Color(90, 20, 140);
                    break;
            }
        }
    }
}

