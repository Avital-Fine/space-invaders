namespace Infrastructure.Managers
{
    public class ScoreEntry
    {
        public string PlayerName { get; set; }
        public int Score { get; set; }
        public int Level { get; set; }
        public string Date { get; set; }
        public string GameName { get; set; }

        public ScoreEntry(string i_PlayerName, int i_Score, int i_Level, string i_Date, string i_GameName = "Space Invaders")
        {
            PlayerName = i_PlayerName;
            Score = i_Score;
            Level = i_Level;
            Date = i_Date;
            GameName = i_GameName;
        }
    }
}
