using Microsoft.Xna.Framework;
using Infrastructure.ObjectModel.Screens;

namespace Invaders.Screens
{
    public class ComingSoonScreen : MenuScreen
    {
        private const string k_Title = "Coming Soon!";
        private readonly MenuItem r_Back;

        public ComingSoonScreen(Game i_Game) : base(i_Game, k_Title)
        {
            r_Back = new MenuItem(this, "Back");
        }

        public override void Initialize()
        {
            base.Initialize();

            r_Title.TintColor      = new Color(200, 200, 100); // muted yellow
            r_Back.ButtonTintColor = new Color(45, 45, 70);
            r_Back.TextColor       = Color.White;
            r_Back.Clicked        += ExitScreen;
        }
    }
}
