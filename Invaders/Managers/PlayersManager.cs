using System;
using Invaders.Screens;
using Invaders.Sprites;

namespace Invaders.Managers
{
    public class PlayersManager 
    {
        private const int k_StartLivesNumber = 3;
        private readonly PlayScreen r_PlayScreen;
        private readonly Player[] r_Players;
        private static int m_NumberOfPlayers;
        private static int m_ActivePlayers;
        private static int[] s_Scores;
        private static int[] s_LivesPerPlayer;

        public static int[] Scores { get { return s_Scores; } }
        public static int[] LivesPerPlayer { get { return s_LivesPerPlayer; } }
        public Player[] Players { get { return r_Players; } }

        public PlayersManager(PlayScreen i_PlayScreen)
        {
            r_PlayScreen = i_PlayScreen;
            r_Players = newPlayers();
        }

        public static void InitPlayersManager(int i_NumberOfPlayers)
        {
            m_NumberOfPlayers = i_NumberOfPlayers;
            m_ActivePlayers = i_NumberOfPlayers;
            s_Scores = new int[i_NumberOfPlayers];
            Array.Fill(s_Scores, 0);
            s_LivesPerPlayer = new int[i_NumberOfPlayers];
            Array.Fill(s_LivesPerPlayer, k_StartLivesNumber);
        }

        private Player[] newPlayers()
        {
            Player[] newPlayers = new Player[m_NumberOfPlayers];

            for (int i = 0; i < m_NumberOfPlayers; i++)
            {
                newPlayers[i] = new Player(r_PlayScreen, (eShipType)i, LivesPerPlayer[i], i + 1, Scores[i]);
                newPlayers[i].DonePlaying += player_DonePlaying;
            }

            return newPlayers;
        }

        private void player_DonePlaying(object sender, EventArgs e)
        {
            m_ActivePlayers--;

            if (m_ActivePlayers == 0)
            {
                r_PlayScreen.GameOver();
            }
        }

        public void UpdateInfo()
        {
            updateScores();
            updateLives();
        }

        private void updateScores()
        {
            for (int i = 0; i < Scores.Length; i++)
            {
                Scores[i] = Players[i].Score;
            }
        }

        private void updateLives()
        {
            for (int i = 0; i < LivesPerPlayer.Length; i++)
            {
                LivesPerPlayer[i] = Players[i].Lives;
            }
        }
    }
}
