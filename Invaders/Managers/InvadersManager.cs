using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Infrastructure;
using Infrastructure.Managers;
using Infrastructure.ObjectModel.Screens;
using Invaders.Screens;
using Invaders.Tetris;
using Invaders.IcyTower;
using Invaders.Snake;
using Invaders.PacMan;

namespace Invaders.Managers
{
    public class InvadersManager : BaseGame
    {
        private readonly ScoresDatabase r_ScoresDatabase;
        private const string k_SpaceBGTexture           = @"Sprites\BG_Space01_1024x768";
        private const string k_DashboardBGTexture        = @"Screens\BG_Dashboard";
        private const string k_BGSoundAssert             = @"Sounds\BGMusic";
        private const string k_MenuTransitionSoundAssert = @"Sounds\MenuMove";
        private const string k_DashboardTitle            = "Game Dashboard";
        private const string k_SpaceInvadersTitle        = "Space Invaders";
        private const string k_TetrisTitle               = "Tetris";
        private const string k_IcyTowerTitle             = "Icy Tower";
        private const string k_SnakeTitle                = "Snake";
        private const string k_PacManTitle               = "Pac-Man";

        private readonly Background r_SpaceBackground;
        private readonly Background r_DashboardBackground;
        private readonly GamePickerScreen r_GamePickerScreen;
        private readonly WelcomeScreen r_WelcomeScreen;
        private readonly LevelTransitionScreen r_TransitionScreen;
        private GameOverScreen m_GameOverScreen;   // recreated on each return to dashboard
        private int m_CurrentLevel;
        private PlayScreen m_PlayScreen;
        private NameEntryScreen m_NameEntryScreen;
        private eNumberOfPlayers m_NumberOfPlayers = eNumberOfPlayers.OnePlayer;

        public InvadersManager()
        {
            m_CurrentLevel = 0;

            r_DashboardBackground = new Background(this, k_DashboardBGTexture);

            r_SpaceBackground = new Background(this, k_SpaceBGTexture);
            r_SpaceBackground.Visible = false;

            r_GamePickerScreen = new GamePickerScreen(this);
            r_WelcomeScreen    = new WelcomeScreen(this);
            r_TransitionScreen = new LevelTransitionScreen(this, m_CurrentLevel);
            r_ScoresDatabase   = new ScoresDatabase();

            this.Components.Add(r_DashboardBackground);
            this.Components.Add(r_SpaceBackground);
        }

        protected override void Initialize()
        {
            GraphicsDeviceManager.PreferredBackBufferWidth = BaseGame.k_DefaultWindowWidth;
            GraphicsDeviceManager.PreferredBackBufferHeight = BaseGame.k_DefaultWindowHeight;
            GraphicsDeviceManager.ApplyChanges();

            base.Initialize();

            createGameOverScreen();

            r_WelcomeScreen.DefineSettings   += mainMenu_DefineSettings;
            r_WelcomeScreen.BackToDashboard  += returnToDashboard;
            r_GamePickerScreen.SpaceInvadersSelected += launchSpaceInvaders;
            r_GamePickerScreen.TetrisSelected        += launchTetris;
            r_GamePickerScreen.IcyTowerSelected      += launchIcyTower;
            r_GamePickerScreen.SnakeSelected         += launchSnake;
            r_GamePickerScreen.PacManSelected        += launchPacMan;
            r_GamePickerScreen.LeaderboardSelected   += launchLeaderboard;

            Window.Title = k_DashboardTitle;
            ScreensMananger.SetCurrentScreen(r_GamePickerScreen);
        }

        private void createGameOverScreen()
        {
            m_GameOverScreen = new GameOverScreen(this);
            m_GameOverScreen.DefineSettings  += mainMenu_DefineSettings;
            m_GameOverScreen.StartNewGame    += newGame;
            m_GameOverScreen.BackToDashboard += returnToDashboard;
        }

        private void launchSpaceInvaders()
        {
            Window.Title = k_SpaceInvadersTitle;
            r_DashboardBackground.Visible = false;
            r_SpaceBackground.Visible     = true;
            r_SpaceBackground.TintColor   = Color.White;

            ScreensMananger.Push(m_GameOverScreen);
            ScreensMananger.SetCurrentScreen(r_WelcomeScreen);
        }

        private void launchTetris()
        {
            Window.Title = k_TetrisTitle;
            r_DashboardBackground.Visible = false;
            r_SpaceBackground.Visible     = false;

            TetrisPlayScreen tetrisScreen = new TetrisPlayScreen(this);
            tetrisScreen.BackToDashboard += returnToDashboard;
            ScreensMananger.SetCurrentScreen(tetrisScreen);
        }

        private void launchIcyTower()
        {
            Window.Title = k_IcyTowerTitle;
            r_DashboardBackground.Visible = false;
            r_SpaceBackground.Visible     = false;

            IcyTowerPlayScreen icyTowerScreen = new IcyTowerPlayScreen(this);
            icyTowerScreen.BackToDashboard += returnToDashboard;
            ScreensMananger.SetCurrentScreen(icyTowerScreen);
        }

        private void launchSnake()
        {
            Window.Title = k_SnakeTitle;
            r_DashboardBackground.Visible = false;
            r_SpaceBackground.Visible     = false;

            SnakePlayScreen snakeScreen = new SnakePlayScreen(this);
            snakeScreen.BackToDashboard += returnToDashboard;
            ScreensMananger.SetCurrentScreen(snakeScreen);
        }

        private void launchPacMan()
        {
            Window.Title = k_PacManTitle;
            r_DashboardBackground.Visible = false;
            r_SpaceBackground.Visible     = false;

            PacManPlayScreen pacManScreen = new PacManPlayScreen(this);
            pacManScreen.BackToDashboard += returnToDashboard;
            ScreensMananger.SetCurrentScreen(pacManScreen);
        }

        public void PromptPlayerName()
        {
            NameEntryScreen nameEntry = new NameEntryScreen(this, eNumberOfPlayers.OnePlayer);
            nameEntry.NamesEntered += (names) =>
            {
                PlayersManager.SetPlayerNames(names);
                returnToDashboard();
            };
            nameEntry.Cancelled += () =>
            {
                returnToDashboard();
            };
            ScreensMananger.SetCurrentScreen(nameEntry);
        }

        private void launchLeaderboard()
        {
            LeaderboardScreen leaderboard = new LeaderboardScreen(this);
            leaderboard.BackToDashboard += returnToDashboard;
            ScreensMananger.SetCurrentScreen(leaderboard);
        }

        private void returnToDashboard()
        {
            // GameOverScreen may already be closed if it fired BackToDashboard itself.
            // Only close it when it is still active in the stack.
            if (m_GameOverScreen.Enabled)
            {
                m_GameOverScreen.Close();
            }

            // Restore dashboard visuals and title
            r_SpaceBackground.Visible     = false;
            r_DashboardBackground.Visible = true;
            Window.Title = k_DashboardTitle;

            // Fresh GameOverScreen for the next Space Invaders session
            createGameOverScreen();

            ScreensMananger.SetCurrentScreen(r_GamePickerScreen);
        }

        protected override void LoadContent()
        {
            base.LoadContent();

            MenuScreen.TransitionSoundEffect = SoundsManager.LoadSoundEffect(k_MenuTransitionSoundAssert);
            SoundEffectInstance backgroundEffect = SoundsManager.LoadBackgroundEffect(k_BGSoundAssert);
        }

        private void newGame()
        {
            r_SpaceBackground.TintColor = Color.White;

            if (PlayersManager.PlayerNames != null && PlayersManager.PlayerNames.Length == (int)m_NumberOfPlayers)
            {
                startGame();
            }
            else
            {
                m_NameEntryScreen = new NameEntryScreen(this, m_NumberOfPlayers);
                m_NameEntryScreen.NamesEntered += enterNameScreen_NamesEntered;
                ScreensMananger.Push(m_NameEntryScreen);
                ScreensMananger.SetCurrentScreen(m_NameEntryScreen);
            }
        }

        private void enterNameScreen_NamesEntered(string[] i_PlayerNames)
        {
            PlayersManager.SetPlayerNames(i_PlayerNames);
            startGame();
        }

        private void startGame()
        {
            m_CurrentLevel = 1;
            setPlayScreen();
        }

        private void setPlayScreen()
        {
            m_PlayScreen = new PlayScreen(this, m_CurrentLevel, m_NumberOfPlayers);
            m_PlayScreen.EndLevel        += playScreen_EndLevel;
            m_PlayScreen.BackToDashboard += returnToDashboard;
            r_TransitionScreen.RestartLevel(m_CurrentLevel);
            ScreensMananger.Push(m_PlayScreen);
            ScreensMananger.SetCurrentScreen(r_TransitionScreen);
        }

        private void mainMenu_DefineSettings(eNumberOfPlayers i_NumberOfPlayers)
        {
            if (m_NumberOfPlayers != i_NumberOfPlayers)
            {
                PlayersManager.SetPlayerNames(null);
            }

            m_NumberOfPlayers = i_NumberOfPlayers;
            newGame();
        }

        private void playScreen_EndLevel(bool i_IsWin)
        {
            if (i_IsWin)
            {
                m_CurrentLevel++;
                setPlayScreen();
            }
            else
            {
                r_SpaceBackground.TintColor = Color.IndianRed;
                m_GameOverScreen.SetScoreInfo(PlayersManager.Scores);
                for (int i = 0; i < PlayersManager.Scores.Length; i++)
                {
                    r_ScoresDatabase.SaveScore(PlayersManager.PlayerNames[i], PlayersManager.Scores[i], m_CurrentLevel, "Space Invaders");
                }
            }
        }
    }
}
