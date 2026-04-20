using System;
using Microsoft.Xna.Framework;
using Infrastructure.ObjectModel.Screens;
using System.Collections.Generic;

namespace Invaders.Screens
{
    public class MainMenuScreen : MenuScreen
    {
        public Action DefineSettings;
        public event Action BackToDashboard;

        private const string k_Title = "Main Menu";
        private readonly MenuItem r_ScreenSetting;
        private readonly MultiOptionsItem r_Players;
        private readonly MenuItem r_SoundSetting;
        private readonly MenuItem r_Leaderboard;
        private readonly MenuItem r_Play;
        private readonly MenuItem r_Quit;
        private static eNumberOfPlayers s_NumberOfPlayers = eNumberOfPlayers.OnePlayer; // default

        public eNumberOfPlayers NumberOfPlayers { get { return s_NumberOfPlayers; } }

        public MainMenuScreen(Game i_Game) : base(i_Game, k_Title)
        {
            r_ScreenSetting = new MenuItem(this, "Screen Settings");
            r_Players = new MultiOptionsItem(this, "Players: ", "One", "Two");
            r_SoundSetting = new MenuItem(this, "Sound Setting");
            r_Leaderboard = new MenuItem(this, "Show Leaderboard");
            r_Play = new MenuItem(this, "Play");
            r_Quit = new MenuItem(this, "Quit");
        }

        public override void Initialize()
        {
            base.Initialize();

            r_ScreenSetting.Clicked += screenSetting_Clicked;
            r_Players.Clicked += players_Clicked;
            r_Players.CurrentOption = s_NumberOfPlayers == eNumberOfPlayers.OnePlayer ? "One" : "Two";
            r_SoundSetting.Clicked += soundSetting_Clicked;
            r_Leaderboard.Clicked += leaderboard_Clicked;
            r_Play.Clicked += play_Clicked;
            r_Quit.Clicked += quit_Clicked;
        }

        private void screenSetting_Clicked()
        {
            ScreensManager.SetCurrentScreen(new ScreenMenuScreen(Game));
        }

        private void players_Clicked()
        {
            s_NumberOfPlayers = s_NumberOfPlayers == eNumberOfPlayers.TwoPlayers ? eNumberOfPlayers.OnePlayer : eNumberOfPlayers.TwoPlayers;
        }

        private void soundSetting_Clicked()
        {
            ScreensManager.SetCurrentScreen(new SoundMenuScreen(Game));
        }

        private void leaderboard_Clicked()
        {
            ScreensManager.SetCurrentScreen(new LeaderboardScreen(Game));
        }

        private void play_Clicked()
        {
            this.ExitScreen();
            onDefineSettings();
        }

        private void quit_Clicked()
        {
            ExitScreen();
            BackToDashboard?.Invoke();
        }

        private void onDefineSettings()
        {
            if(DefineSettings != null)
            {
                DefineSettings.Invoke();
            }
        }
    }
}
