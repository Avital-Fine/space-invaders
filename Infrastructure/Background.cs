using Microsoft.Xna.Framework;
using Infrastructure.ObjectModel;

namespace Infrastructure
{
    public class Background : Sprite 
    {
        public Background(Game i_Game, string i_AssetName)
            : base(i_AssetName, i_Game)
        {
        }

        private void Window_ClientSizeChanged(object sender, System.EventArgs e)
        {
            resizeBackground();
        }

        private void resizeBackground()
        {
            int width = Game.Window.ClientBounds.Width;
            int height = Game.Window.ClientBounds.Height;

            this.Scales = new Vector2(width / this.WidthBeforeScale, height / this.HeightBeforeScale);
        }

        public Background(Game i_Game, string i_AssetName, int i_Opacity)
            : this(i_Game, i_AssetName)
        {
            this.Opacity = i_Opacity;
        }

        public override void Initialize()
        {
            base.Initialize();

            this.DrawOrder = int.MinValue;
            resizeBackground();
            Game.Window.ClientSizeChanged += Window_ClientSizeChanged;
        }
    }
}
