using System;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Infrastructure.ObjectModel;
using Infrastructure.ObjectModel.Screens;

namespace Invaders.Screens
{
    public class GameOverScreen : GameScreen
    {
        public event Action StartNewGame;
        public event Action<eNumberOfPlayers> DefineSettings;

        private const string k_FontType = "Consolas";
        private readonly Keys r_MainMenuTrigger = Keys.M;
        private readonly Keys r_NewGameTrigger = Keys.Home;
        private readonly Headline r_GameOverMessage;
        private readonly Text r_ScoresMessage;
        private readonly Text r_InfoMessage;
        private readonly MainMenuScreen r_MainMenu;

        public GameOverScreen(Game i_Game)
            : base(i_Game)
        {
            r_GameOverMessage = new Headline(this, k_FontType, "Game Over");
            r_ScoresMessage = new Text(this, k_FontType);
            r_InfoMessage = new Text(this, k_FontType, @"Press 'Home' to Start
Press 'M' for Main Menu
Press 'Esc' for Exit");
            r_MainMenu = new MainMenuScreen(i_Game);
        }

        public override void Initialize()
        {
            base.Initialize();

            if (r_MainMenu.DefineSettings == null) 
            { 
                r_MainMenu.DefineSettings += onDefineSettings; 
            }

            Game.Window.ClientSizeChanged += window_ClientSizeChanged;
            r_GameOverMessage.Scales = new Vector2(3, 3);
            setPosition();
        }

        private void setPosition()
        {
            if (r_ScoresMessage != null)
            {
                r_ScoresMessage.Position = r_GameOverMessage.Position + new Vector2(-r_ScoresMessage.Width / 2, (float)(r_GameOverMessage.Height * 2));
                r_InfoMessage.Position = r_GameOverMessage.Position + new Vector2(-r_InfoMessage.Width / 2, (float)(r_GameOverMessage.Height * 2) + r_ScoresMessage.Height * 1.5f);
            }
            else
            {
                r_InfoMessage.Position = r_GameOverMessage.Position + new Vector2(-r_InfoMessage.Width / 2, (float)(r_GameOverMessage.Height * 3));
            }
        }

        public void SetScoreInfo(int[] i_Scores)
        {
            StringBuilder scores = new StringBuilder();
            int maxScore = 0;
            int winnerIdx = 0;

            for (int i = 0; i < i_Scores.Length; i++)
            {
                scores.AppendFormat(@"P{0}: {1} Score{2}", (i + 1), i_Scores[i].ToString(), Environment.NewLine);

                if (i_Scores[i] > maxScore)
                {
                    maxScore = i_Scores[i];
                    winnerIdx = i;
                }
            }

            StringBuilder whoWins = new StringBuilder(string.Format(@"The Winner Is P{0}!", winnerIdx + 1));
            whoWins.Append(Environment.NewLine).Append(scores);
            r_ScoresMessage.Content = whoWins.ToString();
            r_ScoresMessage.Initialize();
            this.Initialize();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (InputManager.KeyPressed(Keys.Escape))
            {
                Game.Exit();
            }
            else if (InputManager.KeyPressed(r_MainMenuTrigger))
            {
                ScreensManager.SetCurrentScreen(r_MainMenu);
            }
            else if (InputManager.KeyPressed(r_NewGameTrigger)) 
            {
                onStartNewGame();
            }
        }

        private void window_ClientSizeChanged(object sender, EventArgs e)
        {
            setPosition();
        }

        private void onDefineSettings()
        {
            if (DefineSettings != null)
            {
                DefineSettings.Invoke(r_MainMenu.NumberOfPlayers);
            }
        }

        private void onStartNewGame()
        {
            if (StartNewGame != null)
            {
                StartNewGame.Invoke();
            }
        }
    }
}
