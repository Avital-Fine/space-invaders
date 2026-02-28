using Microsoft.Xna.Framework;
using Infrastructure.ObjectModel.Screens;

namespace Infrastructure.ObjectModel
{
    public class Headline : Text
    {
        public Headline(GameScreen i_GameScreen, string i_FileName, string i_Content)
            : base(i_GameScreen, i_FileName, i_Content)
        {
        }

        public override void Initialize()
        {
            base.Initialize();

            setPosition();
            Scales = new Vector2(2, 2);
            Game.Window.ClientSizeChanged += Window_ClientSizeChanged;
        }

        private void Window_ClientSizeChanged(object sender, System.EventArgs e)
        {
            setPosition();
        }

        private void setPosition()
        {
            this.Centralize();
            this.Position -= new Vector2(0, GameScreen.CenterOfViewPort.Y / 1.5f);
        }
    }
}
