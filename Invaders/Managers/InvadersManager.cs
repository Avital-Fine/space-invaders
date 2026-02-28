using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Infrastructure;
using Infrastructure.Managers;
using Infrastructure.ObjectModel.Screens;
using Invaders.Screens;

namespace Invaders.Managers
{
    public class InvadersManager : BaseGame
    {
        private readonly ScoresDatabase r_ScoresDatabase;
        private const string k_BGTextureAssert = @"Sprites\BG_Space01_1024x768";
        private const string k_BGSoundAssert = @"Sounds\BGMusic";
        private const string k_MenuTransitionSoundAssert = @"Sounds\MenuMove";
        private readonly Background r_Background;
        private readonly WelcomeScreen r_WelcomeScreen;
        private readonly LevelTransitionScreen r_TransitionScreen;
        private readonly GameOverScreen r_GameOverScreen;
        private int m_CurrentLevel;
        private PlayScreen m_PlayScreen;
        private NameEntryScreen m_NameEntryScreen;
        private eNumberOfPlayers m_NumberOfPlayers = eNumberOfPlayers.OnePlayer;

        public InvadersManager()
        {
            m_CurrentLevel = 0;
            r_Background = new Background(this, k_BGTextureAssert);
            r_WelcomeScreen = new WelcomeScreen(this);
            r_TransitionScreen = new LevelTransitionScreen(this, m_CurrentLevel);
            r_GameOverScreen = new GameOverScreen(this);
            r_ScoresDatabase = new ScoresDatabase();
            this.Components.Add(r_Background);
        }

        protected override void Initialize()
        {
            base.Initialize();

            r_WelcomeScreen.DefineSettings += mainMenu_DefineSettings;
            r_GameOverScreen.DefineSettings += mainMenu_DefineSettings;
            r_GameOverScreen.StartNewGame += newGame;

            ScreensMananger.Push(r_GameOverScreen);
            ScreensMananger.SetCurrentScreen(r_WelcomeScreen);
        }

        protected override void LoadContent()
        {
            base.LoadContent();

            MenuScreen.TransitionSoundEffect = SoundsManager.LoadSoundEffect(k_MenuTransitionSoundAssert);
            SoundEffectInstance backgroundEffect = SoundsManager.LoadBackgroundEffect(k_BGSoundAssert);
        }

        private void newGame()
        {
            r_Background.TintColor = Color.White;

            if (PlayersManager.PlayerNames != null && PlayersManager.PlayerNames.Length == (int)m_NumberOfPlayers)
            {
                // Names already set — skip name entry
                startGame();
            }
            else
            {
                // First time — ask for names
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
            m_PlayScreen.EndLevel += playScreen_EndLevel;
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
                r_Background.TintColor = Color.IndianRed;
                r_GameOverScreen.SetScoreInfo(PlayersManager.Scores);
                // save scores to database
                for (int i = 0; i < PlayersManager.Scores.Length; i++)
                {
                    r_ScoresDatabase.SaveScore(PlayersManager.PlayerNames[i], PlayersManager.Scores[i], m_CurrentLevel);
                }
            }
        }
    }
}
