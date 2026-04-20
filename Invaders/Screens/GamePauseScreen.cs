using System;
using Microsoft.Xna.Framework;
using Infrastructure;
using Infrastructure.ObjectModel.Screens;

namespace Invaders.Screens
{
    public class GamePauseScreen : MenuScreen
    {
        public event Action BackToDashboard;

        private const string k_Title         = "Game Paused";
        private const float  k_BlackTintAlpha = 0.55f;

        private readonly MenuItem r_Resume;
        private readonly MenuItem r_Mute;
        private readonly MenuItem r_QuitToDashboard;

        public GamePauseScreen(Game i_Game) : base(i_Game, k_Title)
        {
            r_Resume          = new MenuItem(this, "Resume");
            r_Mute            = new MenuItem(this, "Mute / Unmute");
            r_QuitToDashboard = new MenuItem(this, "Quit to Dashboard");
        }

        public override void Initialize()
        {
            base.Initialize();

            this.IsModal      = true;
            this.IsOverlayed  = true;
            this.BlackTintAlpha = k_BlackTintAlpha;

            r_Resume.Clicked         += ExitScreen;
            r_Mute.Clicked           += mute_Clicked;
            r_QuitToDashboard.Clicked += quitToDashboard_Clicked;
        }

        private void mute_Clicked()
        {
            (Game as BaseGame).SoundsManager.MuteToggle();
        }

        private void quitToDashboard_Clicked()
        {
            ExitScreen();
            BackToDashboard?.Invoke();
        }
    }
}
