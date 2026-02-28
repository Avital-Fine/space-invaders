using Microsoft.Xna.Framework;
using Infrastructure;
using Infrastructure.ObjectModel.Screens;

namespace Invaders.Screens
{
    public class ScreenMenuScreen : MenuScreen
    {
        private const string k_Title = "Screen Settings";
        private readonly MultiOptionsItem r_AllowResizing;
        private readonly MultiOptionsItem r_FullScreenMode;
        private readonly MultiOptionsItem r_MouseVisability;
        private readonly MenuItem r_Done;

        public ScreenMenuScreen(Game i_Game)
            : base(i_Game, k_Title)
        {
            r_AllowResizing = new MultiOptionsItem(this, "Allow Window Resizing: ");
            r_FullScreenMode = new MultiOptionsItem(this, "Full Screen Mode: ");
            r_MouseVisability = new MultiOptionsItem(this, "Mouse Visability: ", "Visible", "Invisible");
            r_Done = new MenuItem(this, "Done");
        }

        public override void Initialize()
        {
            base.Initialize();

            r_AllowResizing.Clicked += AllowResizing_Clicked;
            r_AllowResizing.CurrentOption = Game.Window.AllowUserResizing ? "On" : "Off";
            r_FullScreenMode.Clicked += FullScreenMode_Clicked;
            r_FullScreenMode.CurrentOption = (Game as BaseGame).GraphicsDeviceManager.IsFullScreen ? "On" : "Off";
            r_MouseVisability.Clicked += MouseVisability_Clicked;
            r_MouseVisability.CurrentOption = Game.IsMouseVisible ? "Visible" : "Invisible";
            r_Done.Clicked += Done_Clicked;
        }

        private void AllowResizing_Clicked()
        {
            Game.Window.AllowUserResizing = !Game.Window.AllowUserResizing;
        }

        private void FullScreenMode_Clicked()
        {
            (Game as BaseGame).GraphicsDeviceManager.ToggleFullScreen();
        }

        private void MouseVisability_Clicked()
        {
            Game.IsMouseVisible = !Game.IsMouseVisible;
        }

        private void Done_Clicked()
        {
            ExitScreen();
        }
    }
}
