using System;
using Microsoft.Xna.Framework;
using Infrastructure.ObjectModel;
using Infrastructure.ObjectModel.Screens;

namespace Invaders.Sprites
{
    public class Live : Sprite
    {
        private const float k_Opacity = 0.5f;
        private readonly Vector2 r_Scale = new Vector2(0.5f, 0.5f);
        private readonly Player r_Player;
        private readonly int r_LiveIdx;

        public Live(Player i_Player, GameScreen i_GameScreen, int i_LiveIdx) : base(i_Player.Ship.AssetName, i_GameScreen)
        {
            r_LiveIdx = i_LiveIdx;
            r_Player = i_Player;
        }

        public override void Initialize()
        {
            base.Initialize();

            Game.Window.ClientSizeChanged += window_ClientSizeChanged;
        }

        protected override void InitBounds()
        {
            base.InitBounds();

            Scales *= r_Scale;
            Opacity *= k_Opacity;
            setPosition();
        }

        private void setPosition()
        {
            float xPosition = Game.GraphicsDevice.Viewport.Width - (r_LiveIdx + 1) * Width;
            float yPosition = (int)r_Player.Ship.ShipType * this.Height;

            Position = new Vector2(xPosition, yPosition);
        }

        public void RemoveFromScreen()
        {
            this.GameScreen.Remove(this);
        }

        private void window_ClientSizeChanged(object sender, EventArgs e)
        {
            setPosition();
        }
    }
}
