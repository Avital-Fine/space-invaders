using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Infrastructure.ObjectModel;
using Infrastructure.ObjectModel.Screens;
using Infrastructure.Managers;

namespace Invaders.Screens
{
    public class LeaderboardScreen : GameScreen
    {
        private const string k_FontType = "Consolas";
        private const int k_TopScoresCount = 5;

        private readonly ScoresDatabase r_ScoresDatabase;
        private readonly Headline r_TitleText;
        private readonly Text r_ScoresText;
        private readonly Text r_InfoText;

        public LeaderboardScreen(Game i_Game) : base(i_Game)
        {
            r_ScoresDatabase = new ScoresDatabase();
            r_TitleText = new Headline(this, k_FontType, "Top 5 Leaderboard");
            r_ScoresText = new Text(this, k_FontType);
            r_InfoText = new Text(this, k_FontType, "Press 'Esc' to go back");
        }

        public override void Initialize()
        {
            base.Initialize();

            this.BlendState = BlendState.NonPremultiplied;

            r_TitleText.Scales = new Vector2(2f, 2f);
            r_TitleText.TintColor = Color.Gold;

            loadAndDisplayScores();
            positionElements();
        }

        private void loadAndDisplayScores()
        {
            List<ScoreEntry> topScores = r_ScoresDatabase.GetTopScores(k_TopScoresCount);

            StringBuilder sb = new StringBuilder();
            if (topScores.Count == 0)
            {
                sb.AppendLine("No scores yet — play a game first!");
            }
            else
            {
                for (int i = 0; i < topScores.Count; i++)
                {
                    ScoreEntry entry = topScores[i];
                    sb.AppendFormat(
                        "{0}. {1,-12}  Score: {2,-6}  Level: {3}{4}",
                        i + 1,
                        entry.PlayerName,
                        entry.Score,
                        entry.Level,
                        Environment.NewLine);
                }
            }

            r_ScoresText.Content = sb.ToString();
            r_ScoresText.TintColor = Color.White;
        }

        private void positionElements()
        {
            // Title near the top-center
            r_TitleText.Position = new Vector2(
                CenterOfViewPort.X - r_TitleText.Width / 2f,
                CenterOfViewPort.Y - 200f);

            // Scores below the title
            r_ScoresText.Position = new Vector2(
                CenterOfViewPort.X - r_ScoresText.Width / 2f,
                r_TitleText.Position.Y + r_TitleText.Height + 20f);

            // Hint at the bottom
            r_InfoText.TintColor = Color.Gray;
            r_InfoText.Position = new Vector2(
                CenterOfViewPort.X - r_InfoText.Width / 2f,
                r_ScoresText.Position.Y + r_ScoresText.Height + 30f);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (InputManager.KeyPressed(Keys.Escape))
            {
                ExitScreen();
            }
        }
    }
}
